import ruWelcome from "src/localization/ru/welcome.json";

describe("auth story Russian copy", () => {
  test("LoginStory_WhenRussian_UsesCompactEquivalentHeadline", () => {
    expect(ruWelcome.login_story_title).toBe(
      "Ваша цифровая жизнь. Одно безопасное место."
    );
    expect(ruWelcome.login_story_description).toBe(
      "Финансы, здоровье, поездки и повседневные инструменты — всё рядом."
    );
  });
});
