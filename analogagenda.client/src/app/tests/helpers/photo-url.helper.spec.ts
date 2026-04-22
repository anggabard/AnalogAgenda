import {
  appendUpdatedDateQuery,
  toPhotosPreviewDisplayUrl,
  toPhotosPreviewUrl,
} from '../../helpers/photo-url.helper';

describe('toPhotosPreviewUrl', () => {
  it('returns empty for null/undefined/whitespace', () => {
    expect(toPhotosPreviewUrl(null)).toBe('');
    expect(toPhotosPreviewUrl(undefined)).toBe('');
    expect(toPhotosPreviewUrl('   ')).toBe('');
  });

  it('inserts preview segment for full-size photos container URL', () => {
    expect(toPhotosPreviewUrl('https://x.blob.core.windows.net/photos/abc-guid')).toBe(
      'https://x.blob.core.windows.net/photos/preview/abc-guid'
    );
  });

  it('is idempotent when URL already uses preview path', () => {
    const u = 'https://x.blob.core.windows.net/photos/preview/abc-guid';
    expect(toPhotosPreviewUrl(u)).toBe(u);
  });
});

describe('appendUpdatedDateQuery', () => {
  it('returns empty when url is empty', () => {
    expect(appendUpdatedDateQuery('', '2026-01-01T00:00:00.000Z')).toBe('');
    expect(appendUpdatedDateQuery(null, '2026-01-01T00:00:00.000Z')).toBe('');
  });

  it('returns url unchanged when updatedDate is missing', () => {
    const u = 'https://x.blob.core.windows.net/photos/g';
    expect(appendUpdatedDateQuery(u, null)).toBe(u);
    expect(appendUpdatedDateQuery(u, undefined)).toBe(u);
    expect(appendUpdatedDateQuery(u, '')).toBe(u);
    expect(appendUpdatedDateQuery(u, '   ')).toBe(u);
  });

  it('appends ?UpdatedDate= when url has no query', () => {
    const u = 'https://x.blob.core.windows.net/photos/preview/g';
    const iso = '2026-04-22T10:00:00.000Z';
    expect(appendUpdatedDateQuery(u, iso)).toBe(
      `${u}?UpdatedDate=${encodeURIComponent(iso)}`
    );
  });

  it('appends &UpdatedDate= when url already has query string', () => {
    const u = 'https://x.blob.core.windows.net/photos/preview/g?sv=2021';
    const iso = '2026-04-22T10:00:00.000Z';
    expect(appendUpdatedDateQuery(u, iso)).toBe(
      `${u}&UpdatedDate=${encodeURIComponent(iso)}`
    );
  });

  it('accepts Date objects', () => {
    const u = 'https://x.blob.core.windows.net/photos/g';
    const d = new Date(Date.UTC(2026, 3, 22, 10, 0, 0));
    expect(appendUpdatedDateQuery(u, d)).toBe(`${u}?UpdatedDate=${encodeURIComponent(d.toISOString())}`);
  });
});

describe('toPhotosPreviewDisplayUrl', () => {
  it('combines preview path and UpdatedDate', () => {
    const main = 'https://x.blob.core.windows.net/photos/g1';
    const iso = '2026-01-01T00:00:00.000Z';
    const preview = 'https://x.blob.core.windows.net/photos/preview/g1';
    expect(toPhotosPreviewDisplayUrl(main, iso)).toBe(
      `${preview}?UpdatedDate=${encodeURIComponent(iso)}`
    );
  });
});
