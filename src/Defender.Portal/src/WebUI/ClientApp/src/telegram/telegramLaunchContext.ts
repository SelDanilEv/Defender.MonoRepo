let telegramLaunchData: string | null = null;

export const rememberTelegramLaunchData = (initData: string): void => {
  telegramLaunchData = initData;
};

export const getTelegramLaunchData = (): string | null => telegramLaunchData;

export const clearTelegramLaunchData = (): void => {
  telegramLaunchData = null;
};
