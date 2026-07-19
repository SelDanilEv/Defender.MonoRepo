export class PortalBffClient {
  public constructor(private readonly portalBaseUrl: string) {}

  public async get<T>(path: string, accessToken: string): Promise<T> {
    return this.request<T>(path, { method: "GET" }, accessToken);
  }

  public async exchangeMcpToken(mcpToken: string): Promise<string> {
    const response = await fetch(new URL("/oauth/token-exchange", this.portalBaseUrl), {
      method: "POST",
      headers: { authorization: `Bearer ${mcpToken}`, accept: "application/json" },
    });
    if (!response.ok) throw new Error(`Portal token exchange returned ${response.status}.`);
    const payload = (await response.json()) as { accessToken: string };
    return payload.accessToken;
  }

  public async request<T>(path: string, init: RequestInit, accessToken: string): Promise<T> {
    const response = await fetch(new URL(path, this.portalBaseUrl), {
      ...init,
      headers: {
        ...init.headers,
        authorization: `Bearer ${accessToken}`,
        accept: "application/json",
      },
    });

    if (!response.ok) {
      throw new Error(`Portal BFF returned ${response.status}.`);
    }

    return (await response.json()) as T;
  }
}
