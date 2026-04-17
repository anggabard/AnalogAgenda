import { toPhotosPreviewUrl } from '../../helpers/photo-url.helper';

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
