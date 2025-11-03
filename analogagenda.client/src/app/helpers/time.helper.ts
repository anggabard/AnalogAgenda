export class TimeHelper {
  /**
   * Converts decimal minutes to min:sec format
   * @param decimalMinutes - Time in decimal minutes (e.g., 1.5 = 1 minute 30 seconds)
   * @returns Formatted time string (e.g., "1:30")
   */
  static decimalMinutesToMinSec(decimalMinutes: number): string {
    if (decimalMinutes === undefined || decimalMinutes === null || isNaN(decimalMinutes)) return '';
    const minutes = Math.floor(decimalMinutes);
    const seconds = Math.round((decimalMinutes - minutes) * 60);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  /**
   * Converts min:sec format to decimal minutes
   * @param minSec - Time string in min:sec format (e.g., "1:30")
   * @returns Decimal minutes (e.g., 1.5)
   */
  static minSecToDecimalMinutes(minSec: string): number {
    if (!minSec || minSec.trim() === '') return 0;
    const parts = minSec.split(':');
    if (parts.length !== 2) return 0;
    const minutes = parseInt(parts[0]) || 0;
    const seconds = parseInt(parts[1]) || 0;
    return minutes + (seconds / 60);
  }

  /**
   * Formats time for display in view mode
   * @param decimalMinutes - Time in decimal minutes
   * @returns Formatted time string for display (e.g., "1m 30s")
   */
  static formatTimeForDisplay(decimalMinutes: number): string {
    if (decimalMinutes === undefined || decimalMinutes === null || isNaN(decimalMinutes)) return '';
    const minutes = Math.floor(decimalMinutes);
    const seconds = Math.round((decimalMinutes - minutes) * 60);
    return `${minutes}m ${seconds.toString().padStart(2, '0')}s`;
  }

  /**
   * Validates min:sec format input
   * @param minSec - Time string to validate
   * @returns True if valid format
   */
  static isValidMinSecFormat(minSec: string): boolean {
    if (!minSec || minSec.trim() === '') return false;
    const parts = minSec.split(':');
    if (parts.length !== 2) return false;
    const minutes = parseInt(parts[0]);
    const seconds = parseInt(parts[1]);
    return !isNaN(minutes) && !isNaN(seconds) && minutes >= 0 && seconds >= 0 && seconds < 60;
  }
}
