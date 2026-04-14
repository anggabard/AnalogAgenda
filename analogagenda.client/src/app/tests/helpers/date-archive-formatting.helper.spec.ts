import {
  formatCollectionArchiveDateSegment,
  formatExposureDateRangeForArchive,
  type DateParts,
} from '../../helpers/date-archive-formatting.helper';

describe('date-archive-formatting.helper', () => {
  describe('formatExposureDateRangeForArchive (mirrors DateFormattingHelper.FormatExposureDateRange)', () => {
    it('returns empty string when no dates and no fallback', () => {
      expect(formatExposureDateRangeForArchive([], null)).toBe('');
    });

    it('uses fallback when dates list is empty', () => {
      const fb: DateParts = { y: 2024, m: 3, d: 15 };
      expect(formatExposureDateRangeForArchive([], fb)).toBe('15 MAR 2024');
    });

    it('formats a single day when all dates are the same', () => {
      const d: DateParts = { y: 2025, m: 11, d: 13 };
      expect(formatExposureDateRangeForArchive([d, d, d], null)).toBe('13 NOV 2025');
    });

    it('formats same calendar month (different days) as month + year', () => {
      const a: DateParts = { y: 2025, m: 6, d: 1 };
      const b: DateParts = { y: 2025, m: 6, d: 15 };
      expect(formatExposureDateRangeForArchive([b, a], null)).toBe('JUN 2025');
    });

    it('formats same year with consecutive months as MONTH-MONTH year', () => {
      const oct: DateParts = { y: 2025, m: 10, d: 1 };
      const nov: DateParts = { y: 2025, m: 11, d: 15 };
      const dec: DateParts = { y: 2025, m: 12, d: 31 };
      expect(formatExposureDateRangeForArchive([oct, dec, nov], null)).toBe('OCT-DEC 2025');
    });

    it('formats same year with non-consecutive months as year only', () => {
      const jan: DateParts = { y: 2025, m: 1, d: 1 };
      const jun: DateParts = { y: 2025, m: 6, d: 1 };
      expect(formatExposureDateRangeForArchive([jan, jun], null)).toBe('2025');
    });

    it('formats consecutive years as first-last', () => {
      const a: DateParts = { y: 2020, m: 1, d: 1 };
      const b: DateParts = { y: 2023, m: 12, d: 31 };
      expect(formatExposureDateRangeForArchive([b, a], null)).toBe('2020-2023');
    });

    it('formats non-consecutive years as first-last span', () => {
      const a: DateParts = { y: 2018, m: 1, d: 1 };
      const b: DateParts = { y: 2025, m: 1, d: 1 };
      expect(formatExposureDateRangeForArchive([a, b], null)).toBe('2018-2025');
    });
  });

  describe('formatCollectionArchiveDateSegment (From/To ISO bounds)', () => {
    it('returns empty when both bounds missing or invalid', () => {
      expect(formatCollectionArchiveDateSegment(null, null)).toBe('');
      expect(formatCollectionArchiveDateSegment(undefined, undefined)).toBe('');
      expect(formatCollectionArchiveDateSegment('', '')).toBe('');
      expect(formatCollectionArchiveDateSegment('not-a-date', 'also-bad')).toBe('');
    });

    it('formats from-date only', () => {
      expect(formatCollectionArchiveDateSegment('2025-07-04', null)).toBe('4 JUL 2025');
    });

    it('formats to-date only', () => {
      expect(formatCollectionArchiveDateSegment(undefined, '2025-07-04')).toBe('4 JUL 2025');
    });

    it('treats equal from and to as a single day', () => {
      expect(formatCollectionArchiveDateSegment('2025-01-09', '2025-01-09')).toBe('9 JAN 2025');
    });

    it('normalizes swapped from/to (later date first in args)', () => {
      expect(formatCollectionArchiveDateSegment('2025-06-15', '2025-01-10')).toBe('2025');
    });

    it('uses inclusive range across two dates for archive segment', () => {
      expect(formatCollectionArchiveDateSegment('2025-03-01', '2025-03-20')).toBe('MAR 2025');
    });
  });
});
