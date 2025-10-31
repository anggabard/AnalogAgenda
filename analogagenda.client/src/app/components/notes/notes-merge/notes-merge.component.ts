import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NotesService } from '../../../services';
import { MergedNoteEntryDto, NoteDto, NoteEntryDto, NoteEntryOverrideDto } from '../../../DTOs';
import { TimeHelper } from '../../../helpers/time.helper';

@Component({
    selector: 'app-notes-merge',
    templateUrl: './notes-merge.component.html',
    styleUrls: ['./notes-merge.component.css'],
    standalone: false
})
export class NotesMergeComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  notes: NoteDto[] = [];
  filmCount: number = 1;
  loading = false;
  error: string | null = null;
  sortedEntries: MergedNoteEntryDto[] = []; // Re-calculated and sorted entries
  mergedName: string = '';
  mergedSideNote: string = '';
  mergedImageUrl: string = '';

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    const compositeId = this.route.snapshot.paramMap.get('compositeId');
    if (compositeId) {
      this.loadMergedNote(compositeId);
    } else {
      this.error = 'Invalid composite ID';
    }
  }

  loadMergedNote(compositeId: string): void {
    this.loading = true;
    this.error = null;

    this.notesService.getMergedNotes(compositeId).subscribe({
      next: (notes: NoteDto[]) => {
        this.notes = notes;
        this.mergedName = notes.map(n => n.name).join(' + ');
        this.mergedSideNote = notes.map(n => n.sideNote).filter(s => s).join('\n\n');
        this.mergedImageUrl = notes[0]?.imageUrl || '';
        this.recalculateAndSortEntries();
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load merged notes';
        this.loading = false;
        console.error(err);
      }
    });
  }

  /** Recalculate entries as if all processes start at the same time, then sort by start time */
  recalculateAndSortEntries(): void {
    if (this.notes.length === 0) return;

    // Recreate merged entries with effective times based on film count
    const allEntries: MergedNoteEntryDto[] = [];

    // Process each note independently (as if all start at time 0)
    // Each process accumulates time independently, starting from 0
    this.notes.forEach(note => {
      let accumulatedTime = 0;
      
      note.entries.forEach(entry => {
        const effectiveTime = this.getEffectiveTime(entry);
        const startTimeWithinProcess = accumulatedTime;
        
        allEntries.push({
          rowKey: entry.rowKey,
          noteRowKey: entry.noteRowKey,
          time: effectiveTime,
          step: this.getEffectiveStep(entry),
          details: this.getEffectiveDetails(entry),
          index: entry.index,
          temperatureMin: this.getEffectiveTemperature(entry).min,
          temperatureMax: this.getEffectiveTemperature(entry).max,
          substance: note.name,
          startTime: startTimeWithinProcess // Start time within this note's process
        });
        
        accumulatedTime += effectiveTime; // Accumulate time within this process
      });
      
      // Add OUT/DONE row for this process at the total time
      allEntries.push({
        rowKey: `out-done-${note.rowKey}`,
        noteRowKey: note.rowKey,
        time: 0, // OUT/DONE has no duration
        step: 'OUT/DONE',
        details: '',
        index: -1,
        temperatureMin: 0,
        temperatureMax: undefined,
        substance: note.name,
        startTime: accumulatedTime // Total time for this process
      });
    });

    // Sort by start time within each process (as if all processes started simultaneously)
    // This shows which step happens first across all processes, including OUT/DONE rows
    allEntries.sort((a, b) => a.startTime - b.startTime);

    this.sortedEntries = allEntries;
  }

  /** Get applicable override for current film count */
  private getApplicableOverride(entry: NoteEntryDto): NoteEntryOverrideDto | null {
    if (entry.overrides.length === 0) {
      return null;
    }

    // First, try to find an override that matches the current film count
    const matchingOverride = entry.overrides.find(override => 
      this.filmCount >= override.filmCountMin && this.filmCount <= override.filmCountMax
    );
    
    if (matchingOverride) {
      return matchingOverride;
    }
    
    // If no match found, find the last override that ended before the current film count
    const applicableOverrides = entry.overrides.filter(override => 
      (override.filmCountMax ?? 0) <= this.filmCount
    );
    
    if (applicableOverrides.length > 0) {
      applicableOverrides.sort((a, b) => (b.filmCountMax ?? 0) - (a.filmCountMax ?? 0));
      return applicableOverrides[0];
    }
    
    return null;
  }

  /** Get effective time for an entry based on film count, overrides, and rules */
  private getEffectiveTime(entry: NoteEntryDto): number {
    // Check for overrides first
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.time !== undefined) {
      return applicableOverride.time;
    }
    
    // Apply rules if no override
    // "Every X films" means: first X films (1-X) get base time only
    // Then every subsequent group of X films gets an additional increment
    if (entry.rules.length > 0) {
      const rule = entry.rules[0]; // Only one rule per entry
      const incrementCount = Math.floor((this.filmCount - 1) / rule.filmInterval);
      const additionalTime = incrementCount * rule.timeIncrement;
      return entry.time + additionalTime;
    }
    
    return entry.time;
  }

  /** Get effective step name for an entry based on film count and overrides */
  private getEffectiveStep(entry: NoteEntryDto): string {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.step) {
      return applicableOverride.step;
    }
    
    return entry.step;
  }

  /** Get effective details for an entry based on film count and overrides */
  private getEffectiveDetails(entry: NoteEntryDto): string {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.details) {
      return applicableOverride.details;
    }
    
    return entry.details;
  }

  /** Get effective temperature for an entry based on film count and overrides */
  private getEffectiveTemperature(entry: NoteEntryDto): { min: number; max?: number } {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride) {
      return {
        min: applicableOverride.temperatureMin ?? entry.temperatureMin,
        max: applicableOverride.temperatureMax ?? entry.temperatureMax
      };
    }
    
    return {
      min: entry.temperatureMin,
      max: entry.temperatureMax
    };
  }

  /** Film counter controls */
  incrementFilmCount() {
    if (this.filmCount < 100) {
      this.filmCount++;
      this.recalculateAndSortEntries();
    }
  }

  decrementFilmCount() {
    if (this.filmCount > 1) {
      this.filmCount--;
      this.recalculateAndSortEntries();
    }
  }

  /** Check if an entry is an OUT/DONE row */
  isOutDoneRow(entry: MergedNoteEntryDto): boolean {
    return entry.step === 'OUT/DONE';
  }

  /** Format time for display using TimeHelper */
  formatTimeForDisplay(decimalMinutes: number): string {
    return TimeHelper.formatTimeForDisplay(decimalMinutes);
  }
}
