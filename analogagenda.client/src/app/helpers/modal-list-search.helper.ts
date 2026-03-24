/**
 * Case-insensitive substring match for modal list filtering.
 */
export function modalListMatches(query: string, ...fields: (string | null | undefined)[]): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  return fields.some((f) => (f ?? '').toString().toLowerCase().includes(q));
}
