export class PortalBffClient {
  public constructor(private readonly portalBaseUrl: string) {}

  public get<T>(path: string, accessToken: string): Promise<T> {
    return this.request<T>(path, { method: "GET" }, accessToken);
  }

  public async exchangeMcpToken(mcpToken: string): Promise<string> {
    const response = await fetch(new URL("/oauth/token-exchange", this.portalBaseUrl), {
      method: "POST",
      headers: { authorization: `Bearer ${mcpToken}`, accept: "application/json" },
    });
    if (!response.ok) throw new Error(`Portal token exchange returned ${response.status}.`);

    const payload = (await response.json()) as { accessToken?: unknown };
    if (typeof payload.accessToken !== "string" || payload.accessToken.length === 0) {
      throw new Error("Portal token exchange returned no access token.");
    }
    return payload.accessToken;
  }

  public async request<T>(path: string, init: RequestInit, accessToken: string): Promise<T> {
    const response = await fetch(new URL(path, this.portalBaseUrl), {
      ...init,
      headers: { ...init.headers, authorization: `Bearer ${accessToken}`, accept: "application/json" },
    });
    if (!response.ok) throw new Error(`Portal BFF returned ${response.status}.`);
    return (await response.json()) as T;
  }
}
