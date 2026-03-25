import { MergedNoteEntryDto } from '../../../DTOs';

/** Convert note app decimal minutes to integer seconds (timeline). */
export function decimalMinutesToScheduleSeconds(decimalMinutes: number): number {
  return Math.round(decimalMinutes * 60);
}

export type NotesTimerSegmentKind = 'step' | 'outDone';

export interface NotesTimerSegment {
  rowId: string;
  startSec: number;
  durationSec: number;
  kind: NotesTimerSegmentKind;
  /** Note / process id (merged); used for per-process done + row coloring. */
  processId?: string;
}

export type NotesTimerSoundType = 'prep15' | 'prep10' | 'prep5' | 'step' | 'done';

/** Fired at most once per (atSec, type) after deduplication. */
export interface NotesTimerTimelineEvent {
  atSec: number;
  type: NotesTimerSoundType;
}

export function computeTotalSeconds(segments: NotesTimerSegment[]): number {
  let maxT = 0;
  for (const s of segments) {
    if (s.kind === 'step') {
      maxT = Math.max(maxT, s.startSec + s.durationSec);
    } else {
      maxT = Math.max(maxT, s.startSec);
    }
  }
  return maxT;
}

/**
 * Build merged note segments from sorted merged rows (same model as notes-merge).
 */
export function buildMergedTimerSegments(sortedEntries: MergedNoteEntryDto[]): NotesTimerSegment[] {
  return sortedEntries.map((e) => {
    const rowId = `merged-timer-row-${e.rowKey}`;
    const startSec = decimalMinutesToScheduleSeconds(e.startTime);
    if (e.step === 'OUT/DONE') {
      return {
        rowId,
        startSec,
        durationSec: 0,
        kind: 'outDone' as const,
        processId: e.noteRowKey
      };
    }
    return {
      rowId,
      startSec,
      durationSec: decimalMinutesToScheduleSeconds(e.time),
      kind: 'step' as const,
      processId: e.noteRowKey
    };
  });
}

/**
 * Prep at end-15/10/5 (only if segment longer than threshold); step at start; done at outDone instant.
 * Deduplicate same (atSec, type) into one audible event.
 */
export function buildTimelineEvents(segments: NotesTimerSegment[]): NotesTimerTimelineEvent[] {
  const bucket = new Map<string, NotesTimerTimelineEvent>();

  const add = (atSec: number, type: NotesTimerSoundType): void => {
    const key = `${atSec}|${type}`;
    if (!bucket.has(key)) {
      bucket.set(key, { atSec, type });
    }
  };

  for (const s of segments) {
    if (s.kind === 'step' && s.durationSec > 0) {
      const endSec = s.startSec + s.durationSec;
      if (s.durationSec > 15) {
        add(endSec - 15, 'prep15');
      }
      if (s.durationSec > 10) {
        add(endSec - 10, 'prep10');
      }
      if (s.durationSec > 5) {
        add(endSec - 5, 'prep5');
      }
      add(s.startSec, 'step');
    } else if (s.kind === 'outDone') {
      add(s.startSec, 'done');
    }
  }

  return [...bucket.values()].sort((a, b) => a.atSec - b.atSec);
}

/** Highlight OUT/DONE rows briefly after completion (zero-duration marker rows). */
const outDoneHighlightSeconds = 3;

/** Row ids active at elapsed second `t` (inclusive start, exclusive end for steps). */
export function getActiveRowIdsAtElapsed(segments: NotesTimerSegment[], t: number): string[] {
  const ids: string[] = [];
  for (const s of segments) {
    if (s.kind === 'step') {
      if (t >= s.startSec && t < s.startSec + s.durationSec) {
        ids.push(s.rowId);
      }
    } else if (s.kind === 'outDone' && t >= s.startSec && t < s.startSec + outDoneHighlightSeconds) {
      ids.push(s.rowId);
    }
  }
  return ids;
}

/** True if any active step segment has <= remaining seconds until end (blink cue). */
export function shouldBlinkActiveStep(segments: NotesTimerSegment[], t: number): boolean {
  for (const s of segments) {
    if (s.kind !== 'step' || s.durationSec <= 0) continue;
    if (t >= s.startSec && t < s.startSec + s.durationSec) {
      const remaining = s.startSec + s.durationSec - t;
      return remaining <= 15;
    }
  }
  return false;
}

export function timelineEventKey(e: NotesTimerTimelineEvent): string {
  return `${e.type}|${e.atSec}`;
}
