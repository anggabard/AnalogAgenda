import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NoteDto, NoteEntryDto } from '../../../DTOs';

@Component({
  selector: 'app-note-table',
  templateUrl: './note-table.component.html',
  styleUrls: ['./note-table.component.css']
})
export class NoteTableComponent implements OnInit {
  note: NoteDto = { rowKey: '', name: 'Note Name', entries: []};

  isEditMode = false;
  isLoading = true;

  constructor(private route: ActivatedRoute) {}

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
      this.isLoading = false;
      this.isEditMode = true; // allow direct editing
    }
  }

  loadNoteFromBackend(rowKey: string) {
    // Replace with real backend call
    setTimeout(() => {
      this.note = {
        rowKey,
        name: 'My Existing Note',
        entries: [
          { noteRowKey: '1', time: 0, process: 'Prep Chemicals', film: 'Kodak', details: '' },
          { noteRowKey: '2', time: 10, process: 'Develop Film', film: 'Kodak', details: 'Agitate every 30s' },
        ]
      };
      this.isLoading = false;
    }, 500);
  }

  toggleEditMode() {
    if (this.isEditMode) {
      // If already editing, prompt to save or discard
      this.saveNote();
    } else {
      this.isEditMode = true;
    }
  }

  discardChanges() {
    if (this.note.rowKey) {
      // Creating a new note → just reset
      this.note = {
        rowKey: '',
        name: '',
        entries: [{ noteRowKey: '', time: 0, process: '', film: '', details: '' }]
      };
    } else {
      this.loadNoteFromBackend(this.note.rowKey); // reload original data
    }
    this.isEditMode = false;
  }

  saveNote() {
    // Simulate save to backend
    console.log('Saving note:', this.note);
    this.isEditMode = false;
  }

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

  removeRow(index: number) {
    if (this.note.entries.length > 1) {
      this.note.entries.splice(index, 1);
    }
  }

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
