export class PortalBffClient {
    portalBaseUrl;
    constructor(portalBaseUrl) {
        this.portalBaseUrl = portalBaseUrl;
    }
    async get(path, accessToken) {
        return this.request(path, { method: "GET" }, accessToken);
    }
    async exchangeMcpToken(mcpToken) {
        const response = await fetch(new URL("/oauth/token-exchange", this.portalBaseUrl), {
            method: "POST",
            headers: { authorization: `Bearer ${mcpToken}`, accept: "application/json" },
        });
        if (!response.ok)
            throw new Error(`Portal token exchange returned ${response.status}.`);
        const payload = (await response.json());
        return payload.accessToken;
    }
    async request(path, init, accessToken) {
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
        return (await response.json());
    }
}
