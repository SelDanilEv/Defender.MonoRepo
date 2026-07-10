let handlingSessionExpiry = false;

export const beginSessionExpiryHandling = () => {
  if (handlingSessionExpiry) {
    return false;
  }

  handlingSessionExpiry = true;
  return true;
};

export const resetSessionExpiryHandling = () => {
  handlingSessionExpiry = false;
};
