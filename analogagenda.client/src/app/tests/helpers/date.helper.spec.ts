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
});
