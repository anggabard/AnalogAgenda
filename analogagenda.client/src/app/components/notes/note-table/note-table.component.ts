import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NoteDto } from '../../../DTOs';

@Component({
  selector: 'app-note-table',
  templateUrl: './note-table.component.html',
  styleUrls: ['./note-table.component.css']
})
export class NoteTableComponent implements OnInit {
  note: NoteDto = {
    rowKey: '',
    name: '',
    entries: []
  };

  isEditMode = false;
  isLoading = true;
  originalNote: NoteDto | null = null; // Used for discard

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    const noteId = this.route.snapshot.paramMap.get('id');

    if (noteId) {
      // VIEW / EDIT MODE - Load from backend
      this.loadNoteFromBackend(noteId);
    } else {
      // CREATE MODE
      this.note = {
        rowKey: '',
        name: '',
        entries: [
          { noteRowKey: '', time: 0, process: '', film: '', details: '' }
        ]
      };
      this.originalNote = JSON.parse(JSON.stringify(this.note));
      this.isLoading = false;
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
          { noteRowKey: '1', time: 0, process: 'Prep Chemicals', film: 'Kodak', details: '' },
          { noteRowKey: '2', time: 10, process: 'Develop Film', film: 'Kodak', details: 'Agitate every 30s' },
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
        entries: [{ noteRowKey: '', time: 0, process: '', film: '', details: '' }]
      };
    } else if (this.originalNote) {
      this.note = JSON.parse(JSON.stringify(this.originalNote));
    }
    this.isEditMode = false;
  }

  /** Save changes to backend */
  saveNote() {
    console.log('Saving note:', this.note);
    this.originalNote = JSON.parse(JSON.stringify(this.note));
    this.isEditMode = false;
  }

  /** Add a new row */
  addRow() {
    const lastEntry = this.note.entries[this.note.entries.length - 1];
    const newTime = lastEntry ? lastEntry.time : 0;

    this.note.entries.push({
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
    const entry = this.note.entries[index];
    this.note.entries.splice(index, 0, entry);
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
