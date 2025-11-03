import { TimeHelper } from './time.helper';

describe('TimeHelper', () => {
  describe('decimalMinutesToMinSec', () => {
    it('should convert 0 minutes to 0:00', () => {
      expect(TimeHelper.decimalMinutesToMinSec(0)).toBe('0:00');
    });

    it('should convert 1.5 minutes to 1:30', () => {
      expect(TimeHelper.decimalMinutesToMinSec(1.5)).toBe('1:30');
    });

    it('should convert 0.25 minutes to 0:15', () => {
      expect(TimeHelper.decimalMinutesToMinSec(0.25)).toBe('0:15');
    });

    it('should convert 3.75 minutes to 3:45', () => {
      expect(TimeHelper.decimalMinutesToMinSec(3.75)).toBe('3:45');
    });

    it('should convert 5.0 minutes to 5:00', () => {
      expect(TimeHelper.decimalMinutesToMinSec(5.0)).toBe('5:00');
    });

    it('should pad seconds with leading zero', () => {
      expect(TimeHelper.decimalMinutesToMinSec(2.083333)).toBe('2:05');
    });

    it('should handle undefined', () => {
      expect(TimeHelper.decimalMinutesToMinSec(undefined as any)).toBe('');
    });

    it('should handle null', () => {
      expect(TimeHelper.decimalMinutesToMinSec(null as any)).toBe('');
    });

    it('should handle NaN', () => {
      expect(TimeHelper.decimalMinutesToMinSec(NaN)).toBe('');
    });

    it('should round seconds correctly', () => {
      expect(TimeHelper.decimalMinutesToMinSec(1.4999)).toBe('1:30');
    });
  });

  describe('minSecToDecimalMinutes', () => {
    it('should convert 0:00 to 0 minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('0:00')).toBe(0);
    });

    it('should convert 1:30 to 1.5 minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('1:30')).toBe(1.5);
    });

    it('should convert 0:15 to 0.25 minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('0:15')).toBe(0.25);
    });

    it('should convert 3:45 to 3.75 minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('3:45')).toBe(3.75);
    });

    it('should convert 5:00 to 5.0 minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('5:00')).toBe(5.0);
    });

    it('should handle empty string', () => {
      expect(TimeHelper.minSecToDecimalMinutes('')).toBe(0);
    });

    it('should handle whitespace', () => {
      expect(TimeHelper.minSecToDecimalMinutes('   ')).toBe(0);
    });

    it('should handle invalid format', () => {
      expect(TimeHelper.minSecToDecimalMinutes('invalid')).toBe(0);
    });

    it('should handle missing colon', () => {
      expect(TimeHelper.minSecToDecimalMinutes('130')).toBe(0);
    });

    it('should handle too many parts', () => {
      expect(TimeHelper.minSecToDecimalMinutes('1:30:00')).toBe(0);
    });

    it('should handle non-numeric minutes', () => {
      expect(TimeHelper.minSecToDecimalMinutes('abc:30')).toBe(0.5);
    });

    it('should handle non-numeric seconds', () => {
      expect(TimeHelper.minSecToDecimalMinutes('1:abc')).toBe(1.0);
    });

    it('should handle large seconds correctly', () => {
      expect(TimeHelper.minSecToDecimalMinutes('2:90')).toBe(3.5); // 2 min + 90 sec = 3.5 min
    });
  });

  describe('formatTimeForDisplay', () => {
    it('should format 0 minutes as 0m 00s', () => {
      expect(TimeHelper.formatTimeForDisplay(0)).toBe('0m 00s');
    });

    it('should format 1.5 minutes as 1m 30s', () => {
      expect(TimeHelper.formatTimeForDisplay(1.5)).toBe('1m 30s');
    });

    it('should format 0.25 minutes as 0m 15s', () => {
      expect(TimeHelper.formatTimeForDisplay(0.25)).toBe('0m 15s');
    });

    it('should format 3.75 minutes as 3m 45s', () => {
      expect(TimeHelper.formatTimeForDisplay(3.75)).toBe('3m 45s');
    });

    it('should format 5.0 minutes as 5m 00s', () => {
      expect(TimeHelper.formatTimeForDisplay(5.0)).toBe('5m 00s');
    });

    it('should pad seconds with leading zero', () => {
      expect(TimeHelper.formatTimeForDisplay(2.083333)).toBe('2m 05s');
    });

    it('should handle undefined', () => {
      expect(TimeHelper.formatTimeForDisplay(undefined as any)).toBe('');
    });

    it('should handle null', () => {
      expect(TimeHelper.formatTimeForDisplay(null as any)).toBe('');
    });

    it('should handle NaN', () => {
      expect(TimeHelper.formatTimeForDisplay(NaN)).toBe('');
    });

    it('should round seconds correctly', () => {
      expect(TimeHelper.formatTimeForDisplay(1.4999)).toBe('1m 30s');
    });
  });

  describe('Round-trip conversion', () => {
    it('should maintain value through conversion cycle', () => {
      const original = 2.5;
      const minSec = TimeHelper.decimalMinutesToMinSec(original);
      const converted = TimeHelper.minSecToDecimalMinutes(minSec);
      expect(converted).toBe(original);
    });

    it('should maintain 0:15 through conversion', () => {
      const original = '0:15';
      const decimal = TimeHelper.minSecToDecimalMinutes(original);
      const converted = TimeHelper.decimalMinutesToMinSec(decimal);
      expect(converted).toBe(original);
    });

    it('should maintain 3:45 through conversion', () => {
      const original = '3:45';
      const decimal = TimeHelper.minSecToDecimalMinutes(original);
      const converted = TimeHelper.decimalMinutesToMinSec(decimal);
      expect(converted).toBe(original);
    });
  });
});

