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
