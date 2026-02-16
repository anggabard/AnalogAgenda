/**
 * Helper for triggering file downloads from blobs and sanitizing filenames.
 */
export class DownloadHelper {
  /**
   * Triggers a browser download of a blob with the given filename.
   */
  static triggerBlobDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }

  /**
   * Sanitize for use in filenames: only alphanumeric, dot, hyphen, underscore.
   * Use for strict filename segments (e.g. base name before extension).
   */
  static sanitizeForFileName(s: string): string {
    const sanitized = (s || '').replace(/[^a-zA-Z0-9.\-_]/g, '');
    return (sanitized || 'file').substring(0, 50);
  }

  /**
   * Remove path-unsafe characters (<>:"/\|?*) and limit length.
   * Use for names/dates that appear inside filenames.
   */
  static sanitizePathUnsafeChars(s: string): string {
    return (s || '').replace(/[<>:"/\\|?*]/g, '').substring(0, 50);
  }
}
