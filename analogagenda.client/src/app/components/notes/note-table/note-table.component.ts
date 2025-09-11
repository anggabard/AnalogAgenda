import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NoteDto } from '../../../DTOs';
import { NotesService } from '../../../services';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-note-table',
  templateUrl: './note-table.component.html',
  styleUrls: ['./note-table.component.css']
})
export class NoteTableComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService)

  note: NoteDto = {
    rowKey: '',
    name: '',
    entries: []
  };

  isEditMode = false;
  isNewNote = false;
  isLoading = true;

  noteRowKey: string | null = null;
  originalNote: NoteDto | null = null; // Used for discard

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.noteRowKey = this.route.snapshot.paramMap.get('id');

    if (this.noteRowKey) {
      // VIEW / EDIT MODE - Load from backend
      this.loadNoteFromBackend(this.noteRowKey);
    } else {
      // CREATE MODE
      this.note = {
        rowKey: '',
        name: '',
        entries: [
          { rowKey: '', noteRowKey: '', time: 0, process: '', film: '', details: '' }
        ]
      };
      this.originalNote = JSON.parse(JSON.stringify(this.note));
      this.isLoading = false;
      this.isNewNote = true;
      this.isEditMode = true; // allow direct editing when creating
    }
  }

  /** Simulated backend load */
  loadNoteFromBackend(rowKey: string) {
    setTimeout(() => {
      this.note = {
        rowKey,
        name: 'My Existing Note',
        entries: [
          { rowKey: '', noteRowKey: '1', time: 0, process: 'Prep Chemicals', film: 'Kodak', details: '' },
          { rowKey: '', noteRowKey: '2', time: 10, process: 'Develop Film', film: 'Kodak', details: 'Agitate every 30s' },
        ]
      };
      this.originalNote = JSON.parse(JSON.stringify(this.note));
      this.isLoading = false;
    }, 500);
  }

  /** Switch between view and edit */
  toggleEditMode() {
    if (!this.isEditMode) {
      this.isEditMode = true;
    }
  }

  /** Discard changes and return to original */
  discardChanges() {
    if (!this.note.rowKey) {
      // Creating a new note → reset to initial
      this.note = {
        rowKey: '',
        name: '',
        entries: [{ rowKey: '', noteRowKey: '', time: 0, process: '', film: '', details: '' }]
      };
    } else if (this.originalNote) {
      this.note = JSON.parse(JSON.stringify(this.originalNote));
    }
    this.isEditMode = false;
  }

  /** Save changes to backend */
  saveNote() {
    if (this.isNewNote) {
      this.notesService.addNewNote(this.note).subscribe({
        next: (noteRowKey: string) => {
          this.isLoading = false;
          this.router.navigate(['/notes/' + noteRowKey]);
        },
        error: (err) => {
          this.isLoading = false;
          console.error(err);
        }
      });
    } else {
      this.notesService.updateNote(this.noteRowKey!, this.note).subscribe({
        next: () => {
          this.isLoading = false;
          this.originalNote = JSON.parse(JSON.stringify(this.note));
          this.isEditMode = false;
        },
        error: (err) => {
          this.isLoading = false;
          console.error(err);
        }
      });
    }
  }

  /** Add a new row */
  addRow() {
    const lastEntry = this.note.entries[this.note.entries.length - 1];
    const newTime = lastEntry ? lastEntry.time : 0;

    this.note.entries.push({
      rowKey: '',
      noteRowKey: '',
      time: newTime,
      process: '',
      film: '',
      details: ''
    });
  }

  /** Remove an existing row */
  removeRow(index: number) {
    if (this.note.entries.length > 1) {
      this.note.entries.splice(index, 1);
    }
  }

  copyRow(index: number) {
    const originalEntry = this.note.entries[index];
    var copyEntry = JSON.parse(JSON.stringify(originalEntry));
    copyEntry.rowKey = '';

    this.note.entries.splice(index, 0, copyEntry);
  }

  /** Validate that time cannot be lower than the previous row */
  onTimeChange(index: number, newTime: number) {
    const previousTime = index > 0 ? this.note.entries[index - 1].time : 0;

    if (newTime < previousTime) {
      alert('Time cannot be lower than the previous step!');
      this.note.entries[index].time = previousTime;
    } else {
      this.note.entries[index].time = newTime;
    }
  }
}
