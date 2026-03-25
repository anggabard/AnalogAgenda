import { CommonModule, DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotesTimerComponent } from '../../components/notes/notes-timer/notes-timer.component';
import { NotesTimerSegment } from '../../components/notes/notes-timer/notes-timer-schedule';

describe('NotesTimerComponent', () => {
  const sampleSegments: NotesTimerSegment[] = [
    { rowId: 'timer-row-a', startSec: 0, durationSec: 120, kind: 'step' }
  ];

  function stubBrowserApis(): void {
    if (jasmine.isSpy(window.requestAnimationFrame)) {
      (window.requestAnimationFrame as jasmine.Spy).and.returnValue(42);
    } else {
      spyOn(window, 'requestAnimationFrame').and.returnValue(42);
    }
    if (!jasmine.isSpy(window.cancelAnimationFrame)) {
      spyOn(window, 'cancelAnimationFrame');
    }
    /* playSound catches failures; avoid real Web Audio in unit tests */
    const audioCtor = window.AudioContext as unknown as jasmine.Func;
    if (jasmine.isSpy(audioCtor)) {
      (audioCtor as jasmine.Spy).and.throwError(new Error('AudioContext stub'));
    } else {
      spyOn(window, 'AudioContext').and.throwError(new Error('AudioContext stub'));
    }
  }

  describe('browser', () => {
    let fixture: ComponentFixture<NotesTimerComponent>;
    let component: NotesTimerComponent;

    beforeEach(async () => {
      stubBrowserApis();
      await TestBed.configureTestingModule({
        imports: [CommonModule],
        declarations: [NotesTimerComponent],
        providers: [{ provide: PLATFORM_ID, useValue: 'browser' }]
      }).compileComponents();

      fixture = TestBed.createComponent(NotesTimerComponent);
      fixture.componentRef.setInput('segments', sampleSegments);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    afterEach(() => {
      TestBed.resetTestingModule();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
      expect(component.totalSec).toBeGreaterThan(0);
    });

    it('emits sessionLockedChange true on first play only, false on stop; pause does not unlock', () => {
      const spy = jasmine.createSpy('sessionLockedChange');
      component.sessionLockedChange.subscribe(spy);

      component.play();
      expect(spy).toHaveBeenCalledTimes(1);
      expect(spy).toHaveBeenCalledWith(true);
      expect(component.sessionLocked).toBeTrue();
      expect(component.isPlaying).toBeTrue();

      spy.calls.reset();
      component.play();
      expect(spy).not.toHaveBeenCalled();

      component.pause();
      expect(component.isPlaying).toBeFalse();
      expect(component.sessionLocked).toBeTrue();
      expect(spy).not.toHaveBeenCalled();

      component.stop();
      expect(spy).toHaveBeenCalledTimes(1);
      expect(spy).toHaveBeenCalledWith(false);
      expect(component.sessionLocked).toBeFalse();
      expect(component.elapsedSec).toBe(0);
      expect(component.isPlaying).toBeFalse();
    });

    it('stop without play does not emit sessionLockedChange', () => {
      const spy = jasmine.createSpy('sessionLockedChange');
      component.sessionLockedChange.subscribe(spy);

      component.stop();
      expect(spy).not.toHaveBeenCalled();
    });

    it('does not play when totalSec is 0', () => {
      fixture.componentRef.setInput('segments', []);
      fixture.detectChanges();

      const spy = jasmine.createSpy('sessionLockedChange');
      component.sessionLockedChange.subscribe(spy);

      component.play();
      expect(spy).not.toHaveBeenCalled();
      expect(component.isPlaying).toBeFalse();
    });

    it('schedules RAF on play and cancels on pause/stop', () => {
      component.play();
      expect(window.requestAnimationFrame).toHaveBeenCalled();

      (window.requestAnimationFrame as jasmine.Spy).calls.reset();
      component.pause();
      expect(window.cancelAnimationFrame).toHaveBeenCalled();

      component.play();
      (window.cancelAnimationFrame as jasmine.Spy).calls.reset();
      component.stop();
      expect(window.cancelAnimationFrame).toHaveBeenCalled();
    });
  });

  describe('non-browser platform (SSR)', () => {
    let fixture: ComponentFixture<NotesTimerComponent>;
    let component: NotesTimerComponent;

    beforeEach(async () => {
      stubBrowserApis();
      await TestBed.configureTestingModule({
        imports: [CommonModule],
        declarations: [NotesTimerComponent],
        providers: [{ provide: PLATFORM_ID, useValue: 'server' }]
      }).compileComponents();

      fixture = TestBed.createComponent(NotesTimerComponent);
      fixture.componentRef.setInput('segments', sampleSegments);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    afterEach(() => {
      TestBed.resetTestingModule();
    });

    it('does not query the document for row highlights (isPlatformBrowser guard)', () => {
      const doc = TestBed.inject(DOCUMENT);
      spyOn(doc, 'getElementById').and.callThrough();

      expect(() => {
        component.play();
        component.pause();
        component.stop();
      }).not.toThrow();

      expect(doc.getElementById).not.toHaveBeenCalled();
    });
  });
});
