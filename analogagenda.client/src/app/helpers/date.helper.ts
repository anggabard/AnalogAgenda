/**
 * Helper functions for date operations
 */
export class DateHelper {
  
  /**
   * Creates today's date formatted for HTML date input (YYYY-MM-DD format)
   */
  static getTodayForInput(): string {
    return new Date().toISOString().split('T')[0];
  }

  /**
   * Display-only: DD/MM/YYYY. Parses YYYY-MM-DD from the start of the string first
   * to avoid UTC off-by-one when using Date-only strings from the API.
   */
  static formatDdMmYyyy(value: string | Date | null | undefined): string {
    if (value == null || value === '') return '';
    if (value instanceof Date) {
      if (isNaN(value.getTime())) return '';
      const d = value.getDate();
      const m = value.getMonth() + 1;
      const y = value.getFullYear();
      return `${pad2(d)}/${pad2(m)}/${y}`;
    }
    const s = String(value).trim();
    const ymd = /^(\d{4})-(\d{2})-(\d{2})/.exec(s);
    if (ymd) {
      return `${ymd[3]}/${ymd[2]}/${ymd[1]}`;
    }
    const d = new Date(s);
    if (isNaN(d.getTime())) return s;
    return `${pad2(d.getDate())}/${pad2(d.getMonth() + 1)}/${d.getFullYear()}`;
  }
}

function pad2(n: number): string {
  return n < 10 ? `0${n}` : `${n}`;
}
