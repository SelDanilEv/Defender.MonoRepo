import Role from "src/consts/Role";

export const canAccessMyGarage = (role?: string | null): boolean =>
  role === Role.User || role === Role.Admin || role === Role.SuperAdmin;
