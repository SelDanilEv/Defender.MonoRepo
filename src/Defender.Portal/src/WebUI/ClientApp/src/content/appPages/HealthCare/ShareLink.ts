export const getAbsoluteShareUrl = (publicUrl: string, origin: string) => {
  if (!publicUrl) {
    return "";
  }

  return new URL(publicUrl, origin).toString();
};
