# Free API Options for PersonalFoodAdviser

## Current Architecture

The service uses **two HuggingFace Inference API models** through a single `IMenuIntelligenceClient` interface:

| Task | Model | Method | What it does |
|------|-------|--------|-------------|
| Menu image → dish names | `Salesforce/blip2-opt-2.7b` | `ExtractDishNamesFromImagesAsync` | Vision-language model receives a base64 image + prompt, returns dish names |
| Dish ranking | `google/flan-t5-base` | `GetRankedRecommendationsAsync` | Text generation model receives menu + user preferences, returns ranked list |

### Current Free Tier Limitations

- **$0.10/month** in free credits (consumed per inference second)
- ~few hundred requests/hour rate limit
- `BLIP-2 2.7B` is a relatively small vision model — struggles with decorative fonts, multi-column menus, non-English text
- `flan-t5-base` is 250M parameters — limited reasoning for nuanced preference matching

### Current API Call Flow

```
Image upload → ImageBlob (MongoDB)
    → Kafka: MenuParsingRequestedEvent
    → MenuParsingProcessor
        → Load image bytes from MongoDB
        → POST to HuggingFace /models/Salesforce/blip2-opt-2.7b
           payload: { inputs: "<prompt>", image: "<base64>", parameters: { max_new_tokens: 400 } }
        → Parse generated_text → ParseDishNamesFromText → session.ParsedItems
    → User confirms dishes
    → Kafka: RecommendationsRequestedEvent
    → RecommendationProcessor
        → POST to HuggingFace /models/google/flan-t5-base
           payload: { inputs: "<recommendation prompt>" }
        → Parse response → NormalizeRankedDishes → session.RankedItems
```

---

## Option 1: Google Gemini API

**Recommended as primary provider — replaces both vision and text models.**

### Overview

| Property | Value |
|----------|-------|
| Provider | Google AI Studio |
| Base URL | `https://generativelanguage.googleapis.com/v1beta` |
| Auth | API key via `x-goog-api-key` header or `?key=` query param |
| Multimodal | Yes — accepts images + text in a single request |
| Signup | [aistudio.google.com](https://aistudio.google.com) — free, no credit card required |
| Docs | [ai.google.dev/gemini-api/docs](https://ai.google.dev/gemini-api/docs) |

### Free Tier Limits (as of early 2026)

| Model | RPM | Requests/Day | Tokens/Min |
|-------|-----|-------------|------------|
| `gemini-2.5-flash-lite` | 15 | 1,000 | 250,000 |
| `gemini-2.5-flash` | 10 | 250 | 250,000 |
| `gemini-2.5-pro` | 5 | 100 | 250,000 |

Daily quota resets at midnight Pacific Time. Limits are per-project (not per-key).

### Why Gemini for This Repo

- **One API replaces both models.** Gemini is multimodal — send a menu photo with the existing `VisionPrompt` for dish extraction, and send the existing `BuildRecommendationPrompt` text for rankings. No architectural changes needed beyond swapping the HTTP client.
- **1,000 req/day (Flash-Lite)** is ~100x more generous than HuggingFace free tier.
- **Far more capable** than BLIP-2 + flan-t5-base. Gemini Flash-Lite handles complex menus, multi-language text, decorative fonts, and multi-column layouts.
- **Structured output** support — can request JSON responses with `responseMimeType: "application/json"` to get cleaner dish lists.

### Request Format — Vision (Dish Extraction)

```
POST https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={API_KEY}
Content-Type: application/json

{
  "contents": [
    {
      "parts": [
        {
          "inlineData": {
            "mimeType": "image/jpeg",
            "data": "<base64-encoded-image>"
          }
        },
        {
          "text": "List all dish and menu item names visible in this restaurant menu. Return only the names, one per line, no prices or numbers."
        }
      ]
    }
  ],
  "generationConfig": {
    "maxOutputTokens": 1024,
    "temperature": 0.2
  }
}
```

### Request Format — Text (Recommendations)

```
POST https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={API_KEY}
Content-Type: application/json

{
  "contents": [
    {
      "parts": [
        {
          "text": "Menu dishes: Margherita Pizza, Caesar Salad, Tiramisu\nLikes: Italian, cheese\nDislikes: spicy\nTry something new: no\nReturn exactly 3 dish names from the menu, one per line, ranked by how much the user will enjoy them. Only dish names, no numbers or extra text."
        }
      ]
    }
  ],
  "generationConfig": {
    "maxOutputTokens": 256,
    "temperature": 0.3
  }
}
```

### Response Format

```json
{
  "candidates": [
    {
      "content": {
        "parts": [
          {
            "text": "Margherita Pizza\nCaesar Salad\nTiramisu"
          }
        ],
        "role": "model"
      },
      "finishReason": "STOP"
    }
  ],
  "usageMetadata": {
    "promptTokenCount": 120,
    "candidatesTokenCount": 15,
    "totalTokenCount": 135
  }
}
```

Extract text: `response.candidates[0].content.parts[0].text`

### Integration Sketch (C#)

```csharp
// Options class
public class GeminiOptions
{
    public const string SectionName = "GeminiOptions";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string Model { get; set; } = "gemini-2.5-flash-lite";
    public int MaxOutputTokens { get; set; } = 1024;
}

// Vision call
var url = $"{opts.BaseUrl}/models/{opts.Model}:generateContent?key={opts.ApiKey}";
var payload = new
{
    contents = new[]
    {
        new
        {
            parts = new object[]
            {
                new { inlineData = new { mimeType = "image/jpeg", data = Convert.ToBase64String(imageBytes) } },
                new { text = visionPrompt }
            }
        }
    },
    generationConfig = new { maxOutputTokens = opts.MaxOutputTokens, temperature = 0.2 }
};
// POST, then parse candidates[0].content.parts[0].text

// Text call — same endpoint, just no inlineData part
var payload = new
{
    contents = new[]
    {
        new { parts = new object[] { new { text = recommendationPrompt } } }
    },
    generationConfig = new { maxOutputTokens = 256, temperature = 0.3 }
};
```

### Pros and Cons

| Pros | Cons |
|------|------|
| One API for both tasks | Google account required |
| Very generous free tier (1,000 req/day) | Daily quota resets at midnight PT only |
| Multimodal — no separate OCR step needed | Free tier was reduced in Dec 2025 (could change again) |
| Much higher quality than BLIP-2 + flan-t5 | Non-OpenAI request format (custom JSON) |
| Structured JSON output mode available | Rate limit is per-project, not per-key |

---

## Option 2: OCR.space

**Recommended as a dedicated OCR provider for the dish extraction step.**

### Overview

| Property | Value |
|----------|-------|
| Provider | a9t9 software |
| Endpoint | `https://api.ocr.space/parse/image` (POST) |
| Auth | API key via `apikey` form field (free signup via email) |
| Multimodal | No — pure OCR only (image → raw text) |
| Signup | [ocr.space/ocrapi/freekey](https://ocr.space/ocrapi/freekey) |
| Docs | [ocr.space/OCRAPI](https://ocr.space/OCRAPI) |
| Status | [status.ocr.space](https://status.ocr.space/) |

### Free Tier Limits

| Limit | Value |
|-------|-------|
| Requests/month | 25,000 |
| Requests/day (rate limit) | ~500 per IP |
| File size | 1 MB |
| PDF pages | 3 |
| Searchable PDF | Yes (with watermark) |
| OCR Engines | Engine 1 (fast, 20+ languages), Engine 2 (better for decorative/rotated text), Engine 3 (200+ languages, handwriting) |

### Why OCR.space for This Repo

- **25,000 req/month** — the most generous free OCR quota available. Enough for ~830 menu parses per day.
- **Pure OCR complements existing code.** Returns raw text that feeds directly into the existing `ParseDishNamesFromText` method — the regex-based parser already handles OCR-style output (strips prices, bullets, numbers).
- **`isTable=true`** parameter optimizes for menu-like tabular layouts, returning text line by line.
- **Engine 2** handles decorative restaurant fonts and rotated text better.
- **Simple integration** — multipart POST with image bytes, JSON response with `ParsedText`.

### Request Format

```
POST https://api.ocr.space/parse/image
Content-Type: multipart/form-data

Fields:
  apikey:              "YOUR_FREE_API_KEY"
  file:                <image bytes as multipart file>
  language:            "eng"           (or "auto" for Engine 2/3)
  OCREngine:           "2"            (better for menu fonts)
  isTable:             "true"         (line-by-line output for menus)
  scale:               "true"         (upscale low-res images)
  detectOrientation:   "true"         (auto-rotate)
```

Alternative — base64 input:
```
Fields:
  apikey:              "YOUR_FREE_API_KEY"
  base64Image:         "data:image/jpeg;base64,/9j/4AAQ..."
  language:            "eng"
  OCREngine:           "2"
  isTable:             "true"
```

### Response Format

```json
{
  "ParsedResults": [
    {
      "TextOverlay": null,
      "FileParseExitCode": 1,
      "ParsedText": "Margherita Pizza  12.99\nCaesar Salad  8.50\nTiramisu  7.00\nGrilled Salmon  18.99\n",
      "ErrorMessage": null,
      "ErrorDetails": null
    }
  ],
  "OCRExitCode": 1,
  "IsErroredOnProcessing": false,
  "ErrorMessage": null,
  "ErrorDetails": null,
  "ProcessingTimeInMilliseconds": "1200"
}
```

Extract text: `response.ParsedResults[0].ParsedText`

Exit codes: `1` = success, `2` = partial, `3` = all pages failed, `4` = fatal error.

### Integration Sketch (C#)

```csharp
// Options class
public class OcrSpaceOptions
{
    public const string SectionName = "OcrSpaceOptions";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.ocr.space/parse/image";
    public string Language { get; set; } = "eng";
    public int OcrEngine { get; set; } = 2;
    public bool IsTable { get; set; } = true;
    public bool Scale { get; set; } = true;
}

// API call
var content = new MultipartFormDataContent();
content.Add(new StringContent(opts.ApiKey), "apikey");
content.Add(new StringContent(opts.Language), "language");
content.Add(new StringContent(opts.OcrEngine.ToString()), "OCREngine");
content.Add(new StringContent(opts.IsTable.ToString().ToLower()), "isTable");
content.Add(new StringContent(opts.Scale.ToString().ToLower()), "scale");
content.Add(new StringContent("true"), "detectOrientation");

var imageContent = new ByteArrayContent(imageBytes);
imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
content.Add(imageContent, "file", "menu.jpg");

var response = await httpClient.PostAsync(opts.BaseUrl, content, cancellationToken);
response.EnsureSuccessStatusCode();
var json = await response.Content.ReadAsStringAsync(cancellationToken);

// Parse JSON → ParsedResults[0].ParsedText
// Feed into existing ParseDishNamesFromText()
```

### Pros and Cons

| Pros | Cons |
|------|------|
| 25,000 req/month — most generous free OCR tier | OCR only — still need a separate LLM for recommendations |
| `isTable=true` designed for menu layouts | 1 MB file size limit (may need to resize large photos) |
| Existing `ParseDishNamesFromText` works directly on output | Pure OCR doesn't understand context (extracts everything, not just dish names) |
| Multiple OCR engines to choose from | No prompt-based filtering (extracts prices, headers, etc.) |
| Simple REST API, no SDK needed | API key sent via form field (not header) |

### Best Used As

A **preprocessing step** in a two-stage pipeline:

1. **OCR.space** extracts raw text from the menu image
2. **Gemini or HuggingFace** (text-only call) filters dish names from the OCR output

This avoids spending vision model tokens on OCR and lets you use a cheaper text-only LLM call to parse the dish list.

---

## Option 3: Together AI — Llama-Vision-Free

**Recommended as a free vision-language model alternative/fallback.**

### Overview

| Property | Value |
|----------|-------|
| Provider | Together AI |
| Model | `meta-llama/Llama-Vision-Free` (Llama 3.2 11B Vision) |
| Base URL | `https://api.together.xyz/v1` |
| Auth | Bearer token via `Authorization` header |
| Multimodal | Yes — OpenAI-compatible chat completions with image support |
| Signup | [api.together.ai](https://api.together.ai) — free, no credit card |
| Docs | [docs.together.ai/docs/vision-overview](https://docs.together.ai/docs/vision-overview) |
| Playground | [api.together.ai/playground/chat/meta-llama/Llama-Vision-Free](https://api.together.ai/playground/chat/meta-llama/Llama-Vision-Free) |

### Free Tier

| Property | Value |
|----------|-------|
| Model | `meta-llama/Llama-Vision-Free` |
| Parameters | 11B |
| Context | 128K tokens |
| Cost | $0.00 (free model — rate-limited) |
| Rate limits | Check [console.together.ai](https://console.together.ai) for current limits |

Together AI also provides **$1.00 in free credits** on signup for paid models.

### Why Together AI for This Repo

- **Free multimodal model** at no cost — `Llama-Vision-Free` is a zero-cost inference endpoint.
- **11B parameters** — significantly more capable than BLIP-2 2.7B for instruction-following and dish name extraction.
- **OpenAI-compatible API format** — standard `/v1/chat/completions` endpoint, easy to integrate with existing HTTP client patterns.
- **Good fallback/alternative** — if Gemini daily quota is exhausted, Together AI provides a second free option.
- **Base64 image support** via `data:image/jpeg;base64,...` URIs in the `image_url` field.

### Request Format — Vision (Dish Extraction)

```
POST https://api.together.xyz/v1/chat/completions
Authorization: Bearer {API_KEY}
Content-Type: application/json

{
  "model": "meta-llama/Llama-Vision-Free",
  "messages": [
    {
      "role": "user",
      "content": [
        {
          "type": "image_url",
          "image_url": {
            "url": "data:image/jpeg;base64,<base64-encoded-image>"
          }
        },
        {
          "type": "text",
          "text": "List all dish and menu item names visible in this restaurant menu. Return only the names, one per line, no prices or numbers."
        }
      ]
    }
  ],
  "max_tokens": 1024,
  "temperature": 0.2
}
```

### Request Format — Text (Recommendations)

```
POST https://api.together.xyz/v1/chat/completions
Authorization: Bearer {API_KEY}
Content-Type: application/json

{
  "model": "meta-llama/Llama-Vision-Free",
  "messages": [
    {
      "role": "user",
      "content": "Menu dishes: Margherita Pizza, Caesar Salad, Tiramisu\nLikes: Italian, cheese\nDislikes: spicy\nTry something new: no\nReturn exactly 3 dish names from the menu, one per line, ranked by how much the user will enjoy them. Only dish names, no numbers or extra text."
    }
  ],
  "max_tokens": 256,
  "temperature": 0.3
}
```

### Response Format

```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "model": "meta-llama/Llama-Vision-Free",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Margherita Pizza\nCaesar Salad\nTiramisu"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 120,
    "completion_tokens": 15,
    "total_tokens": 135
  }
}
```

Extract text: `response.choices[0].message.content`

### Integration Sketch (C#)

```csharp
// Options class
public class TogetherAiOptions
{
    public const string SectionName = "TogetherAiOptions";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.together.xyz/v1";
    public string VisionModel { get; set; } = "meta-llama/Llama-Vision-Free";
    public int MaxTokens { get; set; } = 1024;
}

// Vision call
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);

var payload = new
{
    model = opts.VisionModel,
    messages = new[]
    {
        new
        {
            role = "user",
            content = new object[]
            {
                new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}" } },
                new { type = "text", text = visionPrompt }
            }
        }
    },
    max_tokens = opts.MaxTokens,
    temperature = 0.2
};
// POST to {BaseUrl}/chat/completions
// Parse choices[0].message.content
```

### Pros and Cons

| Pros | Cons |
|------|------|
| Completely free model ($0.00) | Rate-limited (exact limits vary) |
| 11B params — much better than BLIP-2 2.7B | Smaller than Gemini Flash models |
| OpenAI-compatible API format | Less capable at complex multi-column menus than Gemini |
| Handles both vision and text tasks | Free model may have lower priority/slower cold starts |
| Base64 image support built-in | Together AI may change free model availability |
| 128K context window | No structured JSON output mode |

---

## Comparison Matrix

| | Gemini API | OCR.space | Together AI |
|---|---|---|---|
| **Type** | Multimodal LLM | Pure OCR | Multimodal LLM |
| **Free volume** | 250–1,000 req/day | 25,000 req/month | Rate-limited (free model) |
| **Vision (menu → dishes)** | Excellent | Good (raw OCR) | Very good |
| **Recommendations (text → ranked list)** | Excellent | N/A | Good |
| **Replaces both HuggingFace models?** | Yes | No (OCR only) | Yes |
| **Model quality vs current** | Much better | Different approach | Better |
| **Menu-specific features** | Prompt-based filtering | `isTable=true`, multiple engines | Prompt-based filtering |
| **Multi-language support** | Excellent | 200+ languages (Engine 3) | Good |
| **API format** | Google custom JSON | Multipart form POST | OpenAI-compatible |
| **Auth method** | API key (query param or header) | API key (form field) | Bearer token |
| **Signup** | Google account, no credit card | Email only | Account, no credit card |
| **Integration effort** | Medium | Low | Medium |
| **File size limit** | 20 MB (inline) | 1 MB | Context-dependent |

---

## Recommended Strategy

### Primary: Gemini Flash-Lite

Use Google Gemini as the main provider for both dish extraction and recommendation ranking. It offers the best quality-to-cost ratio with 1,000 free requests/day.

### Fallback: OCR.space + Text LLM

When Gemini quota is exhausted or unavailable:

1. **OCR.space** extracts raw text from the menu image (25,000 req/month buffer)
2. **HuggingFace flan-t5-base** (existing) or **Together AI** parses dish names from the OCR text

### Architecture

```
MenuParsingProcessor
  ├── Try: GeminiClient.ExtractDishNamesFromImagesAsync()
  │     → POST image + prompt to Gemini → parse dish names
  │
  └── Fallback: OcrSpaceClient.ExtractTextAsync()
        → POST image to OCR.space → raw text
        → ParseDishNamesFromText(rawText) → dish names

RecommendationProcessor
  ├── Try: GeminiClient.GetRankedRecommendationsAsync()
  │     → POST text prompt to Gemini → ranked dishes
  │
  └── Fallback: TogetherAiClient.GetRankedRecommendationsAsync()
        → POST text prompt to Together AI → ranked dishes
```

### Configuration

```json
{
  "GeminiOptions": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "Model": "gemini-2.5-flash-lite",
    "MaxOutputTokens": 1024
  },
  "OcrSpaceOptions": {
    "BaseUrl": "https://api.ocr.space/parse/image",
    "Language": "eng",
    "OcrEngine": 2,
    "IsTable": true,
    "Scale": true
  },
  "TogetherAiOptions": {
    "BaseUrl": "https://api.together.xyz/v1",
    "VisionModel": "meta-llama/Llama-Vision-Free",
    "MaxTokens": 1024
  }
}
```

API keys should be stored as secrets (not in appsettings):

| Secret | Provider |
|--------|----------|
| `GeminiApiKey` | Google AI Studio |
| `OcrSpaceApiKey` | ocr.space |
| `TogetherAiApiKey` | Together AI |

---

## Quick Links

| Resource | URL |
|----------|-----|
| Gemini API Console | https://aistudio.google.com |
| Gemini API Docs | https://ai.google.dev/gemini-api/docs |
| Gemini Rate Limits | https://ai.google.dev/gemini-api/docs/rate-limits |
| OCR.space Free Key | https://ocr.space/ocrapi/freekey |
| OCR.space API Docs | https://ocr.space/OCRAPI |
| OCR.space Status | https://status.ocr.space |
| Together AI Console | https://api.together.ai |
| Together AI Vision Docs | https://docs.together.ai/docs/vision-overview |
| Together AI Playground | https://api.together.ai/playground/chat/meta-llama/Llama-Vision-Free |
