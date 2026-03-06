import APICallWrapper from "src/api/APIWrapper/APICallWrapper";

describe("APICallWrapper", () => {
  test("WhenCalled_SetsSameOriginCredentialsAndDoesNotSetAuthorizationHeader", async () => {
    const fetchMock = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      text: async () => "",
    });
    (global as any).fetch = fetchMock;

    await APICallWrapper({
      url: "/api/home/health",
      options: {
        method: "GET",
      },
      doLock: false,
      showError: false,
      onSuccess: async () => {},
      onFailure: async () => {},
      onFinal: async () => {},
    });

    const options = fetchMock.mock.calls[0][1];
    expect(options.credentials).toBe("same-origin");
    expect(options.headers.Authorization).toBeUndefined();
  });
});
