import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import { foodAdvisorApi } from "src/api/foodAdvisor";

vi.mock("src/api/APIWrapper/APICallWrapper", () => ({ default: vi.fn() }));

test("uploadSessionImages_WhenCalled_UsesCentralApiWrapperWithMultipartBody", async () => {
  vi.mocked(APICallWrapper).mockImplementation(async ({ options, onSuccess }) => {
    expect(options.body).toBeInstanceOf(FormData);
    await onSuccess?.(new Response(JSON.stringify(["image-1"]), { status: 200 }));
  });
  const file = new File(["image"], "menu.png", { type: "image/png" });

  await expect(foodAdvisorApi.uploadSessionImages("session-1", [file])).resolves.toEqual(["image-1"]);
  expect(APICallWrapper).toHaveBeenCalledWith(expect.objectContaining({
    url: expect.stringContaining("session-1/upload"),
    options: expect.objectContaining({ method: "POST" }),
  }));
});
