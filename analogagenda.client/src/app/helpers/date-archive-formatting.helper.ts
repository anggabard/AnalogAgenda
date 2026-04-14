/**
 * Mirrors Database/Helpers/DateFormattingHelper.FormatExposureDateRange for archive filenames
 * (same rules as film formattedExposureDate / ZIP date segment).
 */

const MONTH_ABBREVS = [
  'JAN',
  'FEB',
  'MAR',
  'APR',
  'MAY',
  'JUN',
  'JUL',
  'AUG',
  'SEP',
  'OCT',
  'NOV',
  'DEC',
] as const;

export interface DateParts {
  y: number;
  m: number;
  d: number;
}

function parseIsoDateOnly(s: string | null | undefined): DateParts | null {
  if (!s?.trim()) return null;
  const t = String(s).trim().slice(0, 10);
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(t);
  if (!m) return null;
  const y = +m[1];
  const mo = +m[2];
  const d = +m[3];
  if (!y || mo < 1 || mo > 12 || d < 1 || d > 31) return null;
  return { y, m: mo, d };
}

function sameDay(a: DateParts, b: DateParts): boolean {
  return a.y === b.y && a.m === b.m && a.d === b.d;
}

function formatSingleDate(date: DateParts): string {
  return `${date.d} ${MONTH_ABBREVS[date.m - 1]} ${date.y}`;
}

function formatMonthYear(date: DateParts): string {
  return `${MONTH_ABBREVS[date.m - 1]} ${date.y}`;
}

function areMonthsConsecutive(months: number[]): boolean {
  if (months.length <= 1) return true;
  for (let i = 1; i < months.length; i++) {
    if (months[i] !== months[i - 1] + 1) return false;
  }
  return true;
}

function areYearsConsecutive(years: number[]): boolean {
  if (years.length <= 1) return true;
  for (let i = 1; i < years.length; i++) {
    if (years[i] !== years[i - 1] + 1) return false;
  }
  return true;
}

/**
 * Same rules as DateFormattingHelper.FormatExposureDateRange (C#).
 */
export function formatExposureDateRangeForArchive(
  dates: DateParts[],
  fallback: DateParts | null
): string {
  if (dates.length === 0) {
    return fallback ? formatSingleDate(fallback) : '';
  }

  const sorted = [...dates].sort((a, b) => {
    if (a.y !== b.y) return a.y - b.y;
    if (a.m !== b.m) return a.m - b.m;
    return a.d - b.d;
  });

  if (sorted.every((d) => sameDay(d, sorted[0]))) {
    return formatSingleDate(sorted[0]);
  }

  const firstDate = sorted[0];
  if (sorted.every((d) => d.y === firstDate.y && d.m === firstDate.m)) {
    return formatMonthYear(firstDate);
  }

  if (sorted.every((d) => d.y === firstDate.y)) {
    const months = [...new Set(sorted.map((d) => d.m))].sort((a, b) => a - b);
    if (areMonthsConsecutive(months)) {
      const firstMonth = months[0];
      const lastMonth = months[months.length - 1];
      return `${MONTH_ABBREVS[firstMonth - 1]}-${MONTH_ABBREVS[lastMonth - 1]} ${firstDate.y}`;
    }
    return String(firstDate.y);
  }

  const years = [...new Set(sorted.map((d) => d.y))].sort((a, b) => a - b);
  if (areYearsConsecutive(years)) {
    const firstYear = years[0];
    const lastYear = years[years.length - 1];
    return `${firstYear}-${lastYear}`;
  }
  return `${years[0]}-${years[years.length - 1]}`;
}

/**
 * Builds the date segment for collection ZIP names using From/To bounds,
 * using the same formatting rules as film exposure date strings.
 */
export function formatCollectionArchiveDateSegment(
  fromIso: string | null | undefined,
  toIso: string | null | undefined
): string {
  const fd = parseIsoDateOnly(fromIso ?? undefined);
  const td = parseIsoDateOnly(toIso ?? undefined);
  if (!fd && !td) return '';
  if (fd && !td) return formatExposureDateRangeForArchive([fd], null);
  if (!fd && td) return formatExposureDateRangeForArchive([td], null);
  const d1 = fd!;
  const d2 = td!;
  const before =
    d1.y < d2.y ||
    (d1.y === d2.y && d1.m < d2.m) ||
    (d1.y === d2.y && d1.m === d2.m && d1.d <= d2.d);
  const lo = before ? d1 : d2;
  const hi = before ? d2 : d1;
  if (sameDay(lo, hi)) {
    return formatExposureDateRangeForArchive([lo], null);
  }
  return formatExposureDateRangeForArchive([lo, hi], null);
}
