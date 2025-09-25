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
}
