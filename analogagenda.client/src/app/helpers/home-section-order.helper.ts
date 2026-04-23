/** Stable ids aligned with server `HomeSectionOrderDefaults.ValidSectionIds`. */
export const DEFAULT_HOME_SECTION_ORDER = [
  'filmCheck',
  'currentFilm',
  'settings',
  'wackyIdeas',
  'photoOfTheDay',
] as const;

export type HomeSectionId = (typeof DEFAULT_HOME_SECTION_ORDER)[number];

const allowed = new Set<string>(DEFAULT_HOME_SECTION_ORDER);

/** Returns a full permutation of the five sections: preferred order first, then any missing defaults. */
export function normalizeHomeSectionOrder(raw: string[] | null | undefined): string[] {
  if (!raw?.length) {
    return [...DEFAULT_HOME_SECTION_ORDER];
  }

  const seen = new Set<string>();
  const ordered: string[] = [];
  for (const id of raw) {
    if (allowed.has(id) && !seen.has(id)) {
      seen.add(id);
      ordered.push(id);
    }
  }
  for (const id of DEFAULT_HOME_SECTION_ORDER) {
    if (!seen.has(id)) {
      ordered.push(id);
    }
  }
  return ordered;
}
