/**
 * Full-size photo blob URLs use `.../photos/{guid}`; thumbnails use `.../photos/preview/{guid}`.
 * Idempotent when the URL already contains `photos/preview/`.
 */
export function toPhotosPreviewUrl(imageUrl: string | null | undefined): string {
  const u = imageUrl?.trim() ?? '';
  if (!u) return '';
  if (u.includes('photos/preview/')) return u;
  return u.replace('photos/', 'photos/preview/');
}

/** Query name must match server/cache-bust convention. */
const updatedDateParam = 'UpdatedDate';

function formatUpdatedDateForQuery(updatedDate: string | Date): string {
  if (typeof updatedDate === 'string') {
    const t = updatedDate.trim();
    return t.length > 0 ? t : '';
  }
  return Number.isNaN(updatedDate.getTime()) ? '' : updatedDate.toISOString();
}

/**
 * Appends `UpdatedDate=<encoded>` for browser cache busting. Uses `&` when the URL already has a query (e.g. SAS).
 */
export function appendUpdatedDateQuery(
  url: string | null | undefined,
  updatedDate: string | Date | null | undefined
): string {
  const u = url?.trim() ?? '';
  if (!u) return '';
  if (updatedDate == null || updatedDate === '') return u;
  const v = formatUpdatedDateForQuery(updatedDate);
  if (!v) return u;
  const sep = u.includes('?') ? '&' : '?';
  return `${u}${sep}${updatedDateParam}=${encodeURIComponent(v)}`;
}

/** Preview thumbnail URL with optional row timestamp for `<img src>`. */
export function toPhotosPreviewDisplayUrl(
  imageUrl: string | null | undefined,
  updatedDate: string | Date | null | undefined
): string {
  return appendUpdatedDateQuery(toPhotosPreviewUrl(imageUrl), updatedDate);
}
