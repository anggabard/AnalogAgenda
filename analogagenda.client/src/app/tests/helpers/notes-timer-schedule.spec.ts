import {
  buildMergedTimerSegments,
  buildTimelineEvents,
  computeTotalSeconds,
  decimalMinutesToScheduleSeconds,
  getActiveRowIdsAtElapsed,
  NotesTimerSegment,
  shouldBlinkActiveStep
} from '../../components/notes/notes-timer/notes-timer-schedule';
import { MergedNoteEntryDto } from '../../DTOs';

describe('notes-timer-schedule', () => {
  describe('decimalMinutesToScheduleSeconds', () => {
    it('converts decimal minutes to seconds', () => {
      expect(decimalMinutesToScheduleSeconds(1)).toBe(60);
      expect(decimalMinutesToScheduleSeconds(1.5)).toBe(90);
    });
  });

  describe('computeTotalSeconds', () => {
    it('uses max step end and outDone start', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'a', startSec: 0, durationSec: 120, kind: 'step' },
        { rowId: 'b', startSec: 120, durationSec: 0, kind: 'outDone' }
      ];
      expect(computeTotalSeconds(segments)).toBe(120);
    });
  });

  describe('buildTimelineEvents', () => {
    it('emits prep15/prep10/prep5 at end-15/10/5 when segment is long enough', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'r1', startSec: 0, durationSec: 120, kind: 'step' }
      ];
      const ev = buildTimelineEvents(segments);
      expect(ev.filter((e) => e.type === 'prep15').map((x) => x.atSec)).toContain(105);
      expect(ev.filter((e) => e.type === 'prep10').map((x) => x.atSec)).toContain(110);
      expect(ev.filter((e) => e.type === 'prep5').map((x) => x.atSec)).toContain(115);
      const steps = ev.filter((e) => e.type === 'step');
      expect(steps.length).toBe(1);
      expect(steps[0].atSec).toBe(0);
    });

    it('dedupes prep at same second from two segments', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'a', startSec: 0, durationSec: 120, kind: 'step' },
        { rowId: 'b', startSec: 0, durationSec: 120, kind: 'step' }
      ];
      const ev = buildTimelineEvents(segments);
      const prepAt105 = ev.filter((e) => e.type === 'prep15' && e.atSec === 105);
      expect(prepAt105.length).toBe(1);
    });

    it('dedupes coinciding step starts', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'a', startSec: 0, durationSec: 60, kind: 'step' },
        { rowId: 'b', startSec: 0, durationSec: 90, kind: 'step' }
      ];
      const ev = buildTimelineEvents(segments);
      const steps = ev.filter((e) => e.type === 'step' && e.atSec === 0);
      expect(steps.length).toBe(1);
    });

    it('emits done per outDone; dedupes same second', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'o1', startSec: 200, durationSec: 0, kind: 'outDone', processId: 'n1' },
        { rowId: 'o2', startSec: 300, durationSec: 0, kind: 'outDone', processId: 'n2' },
        { rowId: 'o3', startSec: 200, durationSec: 0, kind: 'outDone', processId: 'n3' }
      ];
      const ev = buildTimelineEvents(segments);
      const done200 = ev.filter((e) => e.type === 'done' && e.atSec === 200);
      expect(done200.length).toBe(1);
      const done300 = ev.filter((e) => e.type === 'done' && e.atSec === 300);
      expect(done300.length).toBe(1);
    });
  });

  describe('getActiveRowIdsAtElapsed', () => {
    it('returns multiple rows when two steps share the same start window', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'a', startSec: 0, durationSec: 60, kind: 'step' },
        { rowId: 'b', startSec: 0, durationSec: 90, kind: 'step' }
      ];
      const ids = getActiveRowIdsAtElapsed(segments, 0);
      expect(ids.sort()).toEqual(['a', 'b'].sort());
    });
  });

  describe('shouldBlinkActiveStep', () => {
    it('is true when within 15s of step end', () => {
      const segments: NotesTimerSegment[] = [
        { rowId: 'a', startSec: 0, durationSec: 100, kind: 'step' }
      ];
      expect(shouldBlinkActiveStep(segments, 86)).toBe(true);
      expect(shouldBlinkActiveStep(segments, 84)).toBe(false);
    });
  });

  describe('buildMergedTimerSegments', () => {
    it('maps merged entries to row ids and kinds', () => {
      const entries: MergedNoteEntryDto[] = [
        {
          rowKey: 'e1',
          noteRowKey: 'note1',
          time: 1,
          step: 'Dev',
          details: '',
          index: 0,
          temperatureMin: 20,
          substance: 'A',
          startTime: 0
        },
        {
          rowKey: 'out1',
          noteRowKey: 'note1',
          time: 0,
          step: 'OUT/DONE',
          details: '',
          index: -1,
          temperatureMin: 0,
          substance: 'A',
          startTime: 1
        }
      ];
      const segs = buildMergedTimerSegments(entries);
      expect(segs.length).toBe(2);
      expect(segs[0].kind).toBe('step');
      expect(segs[0].rowId).toBe('merged-timer-row-e1');
      expect(segs[1].kind).toBe('outDone');
      expect(segs[1].rowId).toBe('merged-timer-row-out1');
    });
  });
});
