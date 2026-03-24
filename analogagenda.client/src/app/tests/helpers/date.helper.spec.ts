import { DateHelper } from '../../helpers/date.helper';

describe('DateHelper', () => {
  describe('getTodayForInput', () => {
    it('should return today\'s date in YYYY-MM-DD format', () => {
      // Arrange
      const today = new Date();
      const expectedDate = today.toISOString().split('T')[0];

      // Act
      const result = DateHelper.getTodayForInput();

      // Assert
      expect(result).toBe(expectedDate);
    });

    it('should return date in correct format for different dates', () => {
      // Arrange - Mock Date to return a specific date
      const mockDate = new Date('2023-05-15T10:30:00Z');
      spyOn(window, 'Date').and.returnValue(mockDate as any);

      // Act
      const result = DateHelper.getTodayForInput();

      // Assert
      expect(result).toBe('2023-05-15');
    });

    it('should handle single digit months and days correctly', () => {
      // Arrange - Mock Date to return early date in year
      const mockDate = new Date('2023-03-05T10:30:00Z');
      spyOn(window, 'Date').and.returnValue(mockDate as any);

      // Act
      const result = DateHelper.getTodayForInput();

      // Assert
      expect(result).toBe('2023-03-05');
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/); // Should always be in YYYY-MM-DD format
    });
  });

  describe('formatDdMmYyyy', () => {
    it('returns empty string for null, undefined, and empty string', () => {
      expect(DateHelper.formatDdMmYyyy(null)).toBe('');
      expect(DateHelper.formatDdMmYyyy(undefined)).toBe('');
      expect(DateHelper.formatDdMmYyyy('')).toBe('');
    });

    it('uses YYYY-MM-DD fast path at start of string (no timezone shift)', () => {
      expect(DateHelper.formatDdMmYyyy('2023-10-05')).toBe('05/10/2023');
      expect(DateHelper.formatDdMmYyyy('2023-10-05T12:00:00.000Z')).toBe('05/10/2023');
      expect(DateHelper.formatDdMmYyyy('  2024-01-09  ')).toBe('09/01/2024');
    });

    it('formats Date instances in local calendar components', () => {
      expect(DateHelper.formatDdMmYyyy(new Date(2023, 9, 5))).toBe('05/10/2023');
      expect(DateHelper.formatDdMmYyyy(new Date(2023, 0, 7))).toBe('07/01/2023');
    });

    it('returns empty string for invalid Date', () => {
      expect(DateHelper.formatDdMmYyyy(new Date(NaN))).toBe('');
    });

    it('falls back to Date parse for non-YYYY-MM-DD-leading strings', () => {
      expect(DateHelper.formatDdMmYyyy('October 5, 2023')).toBe('05/10/2023');
    });

    it('returns original string when Date parsing fails', () => {
      expect(DateHelper.formatDdMmYyyy('not-a-date')).toBe('not-a-date');
    });
  });
});
