import { Component, inject, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { NotesService } from "../../services";
import { NoteDto } from "../../DTOs";

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrl: './notes.component.css'
})

export class NotesComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  notes: NoteDto[] = [];

  ngOnInit(): void {
    this.notesService.getAllNotes().subscribe({
      next: (notes: NoteDto[]) => {
        this.notes = notes;
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  onNewNoteClick() {
    this.router.navigate(['/notes/new']);
  }

  onNoteSelected(rowKey: string) {
    this.router.navigate(['/notes/' + rowKey]);
  }
}
