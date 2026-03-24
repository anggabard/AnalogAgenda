import { UsedFilmThumbnailDto } from '../DTOs';

/** Lower tier = stronger brand match (starts with, then includes, then none). */
function brandMatchTier(filmName: string, brandLower: string): number {
  const n = filmName.trim().toLowerCase();
  if (n.startsWith(brandLower)) {
    return 0;
  }
  if (n.includes(brandLower)) {
    return 1;
  }
  return 2;
}

/** ISO values from form: single number or range like "200-400". */
function parseIsoTargets(iso: string | null | undefined): number[] {
  const s = (iso ?? '').trim();
  if (!s) {
    return [];
  }
  if (s.includes('-')) {
    return s
      .split('-')
      .map((x) => parseInt(x.trim(), 10))
      .filter((n) => !Number.isNaN(n));
  }
  const n = parseInt(s, 10);
  return Number.isNaN(n) ? [] : [n];
}

const largeIsoDistance = 1_000_000;

/** Minimum |thumbnailNumber - target| over all digit runs in name and all targets. */
function thumbnailIsoDistance(filmName: string, targets: number[]): number {
  if (targets.length === 0) {
    return 0;
  }
  const nums =
    filmName
      .match(/\d+/g)
      ?.map((x) => parseInt(x, 10))
      .filter((n) => !Number.isNaN(n)) ?? [];
  if (nums.length === 0) {
    return largeIsoDistance;
  }
  let best = Number.POSITIVE_INFINITY;
  for (const num of nums) {
    for (const t of targets) {
      best = Math.min(best, Math.abs(num - t));
    }
  }
  return best;
}

function compareBySimilarity(
  a: UsedFilmThumbnailDto,
  b: UsedFilmThumbnailDto,
  brandLower: string,
  isoTargets: number[]
): number {
  const ta = brandMatchTier(a.filmName, brandLower);
  const tb = brandMatchTier(b.filmName, brandLower);
  if (ta !== tb) {
    return ta - tb;
  }
  const da = thumbnailIsoDistance(a.filmName, isoTargets);
  const db = thumbnailIsoDistance(b.filmName, isoTargets);
  if (da !== db) {
    return da - db;
  }
  return a.filmName.localeCompare(b.filmName);
}

/**
 * Orders thumbnails most similar to the given brand/ISO first (prefix brand, then substring brand;
 * then smallest numeric distance to form ISO / range). Empty brand: alphabetical by filmName.
 */
export function sortUsedFilmThumbnailsByBrandIsoSimilarity(
  thumbnails: UsedFilmThumbnailDto[],
  brand: string | null | undefined,
  iso: string | null | undefined
): UsedFilmThumbnailDto[] {
  const copy = [...thumbnails];
  const b = (brand ?? '').trim();
  if (!b) {
    return copy.sort((x, y) => x.filmName.localeCompare(y.filmName));
  }
  const brandLower = b.toLowerCase();
  const isoTargets = parseIsoTargets(iso);
  return copy.sort((x, y) => compareBySimilarity(x, y, brandLower, isoTargets));
}
