export interface MaintenanceLabelItem {
  id: string;
  name: string;
}

export const normalizeMaintenanceSelection = (ids: readonly string[] | null | undefined): string[] =>
  Array.from(new Set((ids ?? []).filter((id): id is string => Boolean(id))));

export const toggleMaintenanceSelection = (ids: readonly string[], id: string): string[] => {
  const normalized = normalizeMaintenanceSelection(ids);
  return normalized.includes(id)
    ? normalized.filter((itemId) => itemId !== id)
    : [...normalized, id];
};

export const getLinkedMaintenanceLabels = (
  ids: readonly string[] | null | undefined,
  items: readonly MaintenanceLabelItem[],
): string[] => {
  const selected = new Set(normalizeMaintenanceSelection(ids));
  return items.filter((item) => selected.has(item.id)).map((item) => item.name);
};
