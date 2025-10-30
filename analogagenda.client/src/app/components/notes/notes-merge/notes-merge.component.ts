import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NotesService } from '../../../services';
import { MergedNoteDto, MergedNoteEntryDto } from '../../../DTOs';

@Component({
    selector: 'app-notes-merge',
    templateUrl: './notes-merge.component.html',
    styleUrls: ['./notes-merge.component.css'],
    standalone: false
})
export class NotesMergeComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  mergedNote: MergedNoteDto | null = null;
  filmCount: number = 1;
  loading = false;
  error: string | null = null;

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
      next: (mergedNote: MergedNoteDto) => {
        this.mergedNote = mergedNote;
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load merged note';
        this.loading = false;
        console.error(err);
      }
    });
  }

  /** Calculate accumulated start time for an entry based on film count */
  getAccumulatedStartTime(entryIndex: number): number {
    if (!this.mergedNote) return 0;
    
    let accumulatedTime = 0;
    for (let i = 0; i < entryIndex; i++) {
      accumulatedTime += this.getEffectiveTime(this.mergedNote.entries[i]);
    }
    return accumulatedTime;
  }

  /** Get effective time for an entry based on film count, overrides, and rules */
  getEffectiveTime(entry: MergedNoteEntryDto): number {
    // For merged notes, we use the base time since overrides/rules are already applied
    // during the merge process on the backend
    return entry.time;
  }

  /** Get effective step name for an entry */
  getEffectiveStep(entry: MergedNoteEntryDto): string {
    return entry.step;
  }

  /** Get effective details for an entry */
  getEffectiveDetails(entry: MergedNoteEntryDto): string {
    return entry.details;
  }

  /** Get effective temperature for an entry */
  getEffectiveTemperature(entry: MergedNoteEntryDto): { min: number; max?: number } {
    return {
      min: entry.temperatureMin,
      max: entry.temperatureMax
    };
  }

  goBack(): void {
    this.router.navigate(['/notes']);
  }
}
