import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NoteDto } from '../../../DTOs';
import { NotesService } from '../../../services';

@Component({
  selector: 'app-note-table',
  templateUrl: './note-table.component.html',
  styleUrls: ['./note-table.component.css']
})
export class NoteTableComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  note: NoteDto = this.getEmptyNote();
  selectedFileName: string | null = null;

  isEditMode = false;
  isNewNote = false;
  isPreviewModalOpen: boolean = false;
  isDeleteModalOpen: boolean = false;

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
      this.isNewNote = true;
      this.isEditMode = true; // allow direct editing when creating
    }
  }

  /** Simulated backend load */
  loadNoteFromBackend(rowKey: string) {
    this.notesService.getNote(rowKey).subscribe({
      next: (note: NoteDto) => {
        this.note = note;
        this.originalNote = JSON.parse(JSON.stringify(this.note));
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  /** Switch between view and edit */
  toggleEditMode() {
    if (!this.isEditMode) {
      this.isEditMode = true;
    }
  }

  /** Discard changes and return to original */
  discardChanges() {
    if (this.isNewNote) {
      this.note = this.getEmptyNote();
      this.router.navigate(['/notes']);
    } else if (this.originalNote) {
      this.note = JSON.parse(JSON.stringify(this.originalNote));
    }
    this.isEditMode = false;
  }

  getEmptyNote() {
    return JSON.parse(JSON.stringify({
      rowKey: '',
      name: '',
      sideNote: '',
      imageBase64: '',
      imageUrl: '',
      entries: [{ rowKey: '', noteRowKey: '', time: 0, process: '', film: '', details: '' }]
    }));
  }

  /** Save changes to backend */
  saveNote() {
    if (!this.note.name)
      this.note.name = 'Untitled Note'

    if (this.isNewNote) {
      this.notesService.addNewNote(this.note).subscribe({
        next: (noteRowKey: string) => {
          this.router.navigate(['/notes/' + noteRowKey]);
        },
        error: (err) => {
          console.error(err);
        }
      });
    } else {
      this.notesService.updateNote(this.noteRowKey!, this.note).subscribe({
        next: () => {
          this.originalNote = JSON.parse(JSON.stringify(this.note));
          this.isEditMode = false;
        },
        error: (err) => {
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

    this.note.entries.splice(index + 1, 0, copyEntry);
  }

  /** Validate that time cannot be lower than the previous row and higher that the next*/
  onTimeChange(index: number, newTime: number) {
    const previousTime = index > 0 ? this.note.entries[index - 1].time : 0;
    const nextTime = index < this.note.entries.length - 1 ? this.note.entries[index + 1].time : null;

    if (newTime < previousTime) {
      alert('Time cannot be lower than the previous step!');
      this.note.entries[index].time = previousTime;
    } else if (nextTime && newTime > nextTime) {
      alert('Time cannot be higher than the next step!');
      this.note.entries[index].time = nextTime;
    }
    else {
      this.note.entries[index].time = newTime;
    }
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.selectedFileName = file.name;
      const reader = new FileReader();

      reader.readAsDataURL(file);
      reader.onload = () => (this.note.imageBase64 = reader.result as string);
    }
  }

  onDelete() {
    this.notesService.deleteNote(this.note.rowKey).subscribe({
      next: () => {
        this.router.navigate(['/notes']);
      },
      error: (err) => {
        console.error(err);
      }
    });
  }
}
