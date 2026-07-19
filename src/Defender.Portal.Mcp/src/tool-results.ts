export function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload) }],
    structuredContent: { data: payload },
  };
}

export function errorResult(message: string) {
  return {
    content: [{ type: "text" as const, text: message }],
    isError: true,
  };
}

export async function executePortalTool(action: () => Promise<unknown>) {
  try {
    return jsonResult(await action());
  } catch (error) {
    const message = error instanceof Error ? error.message : "Portal BFF request failed.";
    return errorResult(`${message} Check Portal access and request parameters.`);
  }
}
