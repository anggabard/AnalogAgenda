import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Inject,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Optional,
  Output,
  PLATFORM_ID,
  SimpleChanges
} from '@angular/core';
import {
  buildTimelineEvents,
  computeTotalSeconds,
  getActiveRowIdsAtElapsed,
  NotesTimerSegment,
  NotesTimerSoundType,
  NotesTimerTimelineEvent,
  shouldBlinkActiveStep,
  timelineEventKey
} from './notes-timer-schedule';

@Component({
  selector: 'app-notes-timer',
  templateUrl: './notes-timer.component.html',
  styleUrl: './notes-timer.component.css',
  standalone: false
})
export class NotesTimerComponent implements OnChanges, OnDestroy, OnInit {
  @Input({ required: true }) segments: NotesTimerSegment[] = [];

  /** Emits true after Play until Stop (Pause still locked). */
  @Output() sessionLockedChange = new EventEmitter<boolean>();

  totalSec = 0;
  /** Monotonic elapsed in [0, totalSec] while running / paused. */
  elapsedSec = 0;
  isPlaying = false;
  sessionLocked = false;
  /** Compact bar: elapsed only bottom-right; frees mobile navbar toggle. */
  isMinimized = false;

  private timeline: NotesTimerTimelineEvent[] = [];
  private eventsBySecond = new Map<number, NotesTimerTimelineEvent[]>();
  private firedSoundKeys = new Set<string>();
  private lastHighlightKey = '';
  private lastActiveIds: string[] = [];
  private lastScrollPrimaryId = '';
  private rafId: number | null = null;
  private playStartPerf = 0;
  private elapsedAtPlayStart = 0;
  private audioCtx: AudioContext | null = null;

  constructor(
    @Inject(PLATFORM_ID) private platformId: object,
    @Optional() @Inject(DOCUMENT) private doc: Document | null
  ) {}

  ngOnInit(): void {
    this.syncDocumentBarHeight();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['segments']) {
      this.rebuildScheduleFromSegments();
      if (!this.sessionLocked) {
        this.elapsedSec = 0;
        this.clearDomHighlights();
        this.firedSoundKeys.clear();
        this.lastHighlightKey = '';
        this.lastActiveIds = [];
      }
      this.syncDocumentBarHeight();
    }
  }

  ngOnDestroy(): void {
    this.stopRaf();
    this.clearDomHighlights();
    void this.audioCtx?.close();
    this.audioCtx = null;
    if (isPlatformBrowser(this.platformId) && this.doc?.documentElement) {
      this.doc.documentElement.style.removeProperty('--notes-timer-bar-height');
    }
  }

  get displayElapsed(): { m: number; s: number } {
    const t = Math.min(this.totalSec, Math.max(0, this.elapsedSec));
    const sec = Math.floor(t);
    return { m: Math.floor(sec / 60), s: sec % 60 };
  }

  get displayTotal(): { m: number; s: number } {
    const sec = Math.max(0, Math.floor(this.totalSec));
    return { m: Math.floor(sec / 60), s: sec % 60 };
  }

  formatTwo(n: number): string {
    return n.toString().padStart(2, '0');
  }

  setMinimized(value: boolean): void {
    this.isMinimized = value;
    this.syncDocumentBarHeight();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (typeof window !== 'undefined' && window.innerWidth >= 768 && this.isMinimized) {
      this.setMinimized(false);
    }
  }

  private syncDocumentBarHeight(): void {
    if (!isPlatformBrowser(this.platformId) || !this.doc?.documentElement) {
      return;
    }
    if (this.totalSec <= 0) {
      this.doc.documentElement.style.setProperty('--notes-timer-bar-height', '0px');
      return;
    }
    const h = this.isMinimized ? '58px' : '132px';
    this.doc.documentElement.style.setProperty('--notes-timer-bar-height', h);
  }

  play(): void {
    if (this.totalSec <= 0) return;
    if (!this.sessionLocked) {
      this.sessionLocked = true;
      this.sessionLockedChange.emit(true);
    }
    if (this.isPlaying) return;
    const prev = this.elapsedSec;
    this.isPlaying = true;
    this.playStartPerf = performance.now();
    this.elapsedAtPlayStart = this.elapsedSec;
    this.fireSoundsForRange(prev - 0.0001, this.elapsedSec);
    this.applyDomHighlights();
    this.scheduleRaf();
  }

  pause(): void {
    if (!this.isPlaying) return;
    this.isPlaying = false;
    this.elapsedSec = Math.min(this.totalSec, Math.max(0, this.tickElapsedFromWall()));
    this.stopRaf();
    this.applyDomHighlights();
  }

  stop(): void {
    this.isPlaying = false;
    this.stopRaf();
    this.elapsedSec = 0;
    this.firedSoundKeys.clear();
    this.clearDomHighlights();
    this.lastHighlightKey = '';
    this.lastActiveIds = [];
    this.lastScrollPrimaryId = '';
    if (this.sessionLocked) {
      this.sessionLocked = false;
      this.sessionLockedChange.emit(false);
    }
  }

  nudgeSeconds(delta: number): void {
    const prev = this.elapsedSec;
    if (this.isPlaying) {
      this.elapsedSec = this.tickElapsedFromWall();
    }
    this.elapsedSec = Math.min(this.totalSec, Math.max(0, this.elapsedSec + delta));
    if (this.elapsedSec < prev) {
      this.pruneFiredAfter(this.elapsedSec);
    } else {
      this.fireSoundsForRange(prev, this.elapsedSec);
    }
    if (this.isPlaying) {
      this.playStartPerf = performance.now();
      this.elapsedAtPlayStart = this.elapsedSec;
    }
    this.applyDomHighlights();
  }

  private rebuildScheduleFromSegments(): void {
    this.timeline = buildTimelineEvents(this.segments);
    this.eventsBySecond.clear();
    for (const e of this.timeline) {
      const list = this.eventsBySecond.get(e.atSec) ?? [];
      list.push(e);
      this.eventsBySecond.set(e.atSec, list);
    }
    this.totalSec = computeTotalSeconds(this.segments);
  }

  private tickElapsedFromWall(): number {
    if (!this.isPlaying) return this.elapsedSec;
    const wall = (performance.now() - this.playStartPerf) / 1000;
    return Math.min(this.totalSec, this.elapsedAtPlayStart + wall);
  }

  private scheduleRaf(): void {
    this.stopRaf();
    const loop = (): void => {
      this.rafId = requestAnimationFrame(() => {
        if (!this.isPlaying) {
          this.rafId = null;
          return;
        }
        const prev = this.elapsedSec;
        this.elapsedSec = this.tickElapsedFromWall();
        this.fireSoundsForRange(prev, this.elapsedSec);
        this.applyDomHighlights();
        if (this.elapsedSec >= this.totalSec) {
          this.elapsedSec = this.totalSec;
          this.isPlaying = false;
          this.rafId = null;
          this.stop();
          return;
        }
        loop();
      });
    };
    loop();
  }

  private stopRaf(): void {
    if (this.rafId != null) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
  }

  private fireSoundsForRange(prev: number, curr: number): void {
    const a = Math.floor(prev);
    const b = Math.floor(curr);
    if (b > a) {
      for (let t = a + 1; t <= b; t++) {
        for (const e of this.eventsBySecond.get(t) ?? []) {
          const key = timelineEventKey(e);
          if (!this.firedSoundKeys.has(key)) {
            this.firedSoundKeys.add(key);
            this.playSound(e.type);
          }
        }
      }
    }
  }

  private pruneFiredAfter(currSec: number): void {
    const maxSec = Math.floor(currSec);
    for (const k of [...this.firedSoundKeys]) {
      const pipe = k.lastIndexOf('|');
      if (pipe < 0) continue;
      const atSec = Number(k.slice(pipe + 1));
      if (!Number.isNaN(atSec) && atSec > maxSec) {
        this.firedSoundKeys.delete(k);
      }
    }
  }

  private playSound(type: NotesTimerSoundType): void {
    try {
      const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      if (!this.audioCtx) {
        this.audioCtx = new Ctx();
      }
      const ctx = this.audioCtx;
      if (ctx.state === 'suspended') {
        void ctx.resume();
      }
      const t0 = ctx.currentTime;
      if (type === 'prep15') {
        this.schedulePrepBursts(ctx, t0, 1);
      } else if (type === 'prep10') {
        this.schedulePrepBursts(ctx, t0, 2);
      } else if (type === 'prep5') {
        this.schedulePrepBursts(ctx, t0, 3);
      } else if (type === 'step') {
        this.scheduleStepCue(ctx, t0);
      } else {
        this.scheduleDoneCue(ctx, t0);
      }
    } catch {
      /* ignore */
    }
  }

  /** Short prep rings; gap between rings is clearly audible. */
  private schedulePrepBursts(ctx: AudioContext, t0: number, count: number): void {
    const beepDur = 0.09;
    const gap = 0.34;
    const freq = 700;
    for (let i = 0; i < count; i++) {
      this.scheduleTone(ctx, t0 + i * (beepDur + gap), freq, beepDur, 0.1, 'sine');
    }
  }

  /** Distinct from prep: two-tone triangle, longer than a single prep ping. */
  private scheduleStepCue(ctx: AudioContext, t0: number): void {
    this.scheduleTone(ctx, t0, 420, 0.14, 0.11, 'triangle');
    this.scheduleTone(ctx, t0 + 0.2, 640, 0.22, 0.12, 'triangle');
  }

  /** Ascending three-note motif, fuller than prep rings. */
  private scheduleDoneCue(ctx: AudioContext, t0: number): void {
    const freqs = [523.25, 659.25, 783.99];
    let offset = 0;
    for (const f of freqs) {
      this.scheduleTone(ctx, t0 + offset, f, 0.34, 0.13, 'sine');
      offset += 0.36;
    }
  }

  private scheduleTone(
    ctx: AudioContext,
    start: number,
    freq: number,
    dur: number,
    peak: number,
    wave: OscillatorType
  ): void {
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = wave;
    osc.frequency.setValueAtTime(freq, start);
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(peak, start + 0.025);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + dur);
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start(start);
    osc.stop(start + dur + 0.02);
  }

  private applyDomHighlights(): void {
    if (!isPlatformBrowser(this.platformId) || !this.doc) {
      return;
    }
    const doc = this.doc;
    const t = Math.min(this.totalSec, Math.max(0, this.elapsedSec));
    const ids = getActiveRowIdsAtElapsed(this.segments, t);
    const blink = shouldBlinkActiveStep(this.segments, t);
    const key = `${ids.join(',')}|${blink}`;
    if (key === this.lastHighlightKey) {
      return;
    }
    this.lastHighlightKey = key;

    for (const id of this.lastActiveIds) {
      const el = doc.getElementById(id);
      el?.classList.remove('notes-timer-row-active', 'notes-timer-row-blink');
    }
    this.lastActiveIds = ids;

    for (const id of ids) {
      const el = doc.getElementById(id);
      if (el) {
        el.classList.add('notes-timer-row-active');
        if (blink) {
          el.classList.add('notes-timer-row-blink');
        }
      }
    }

    if (ids.length > 0) {
      const primary = ids[0];
      if (primary !== this.lastScrollPrimaryId) {
        this.lastScrollPrimaryId = primary;
        doc.getElementById(primary)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    } else {
      this.lastScrollPrimaryId = '';
    }
  }

  private clearDomHighlights(): void {
    if (!isPlatformBrowser(this.platformId) || !this.doc) {
      this.lastActiveIds = [];
      this.lastHighlightKey = '';
      return;
    }
    const doc = this.doc;
    for (const id of this.lastActiveIds) {
      doc.getElementById(id)?.classList.remove('notes-timer-row-active', 'notes-timer-row-blink');
    }
    for (const s of this.segments) {
      doc.getElementById(s.rowId)?.classList.remove('notes-timer-row-active', 'notes-timer-row-blink');
    }
    this.lastActiveIds = [];
    this.lastHighlightKey = '';
  }
}
