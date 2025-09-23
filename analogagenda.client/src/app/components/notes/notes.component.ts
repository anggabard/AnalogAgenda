import { Component, inject, OnInit, ViewChild, TemplateRef } from "@angular/core";
import { Router } from "@angular/router";
import { NotesService } from "../../services";
import { NoteDto, PagedResponseDto } from "../../DTOs";

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrl: './notes.component.css'
})

export class NotesComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  @ViewChild('noteCardTemplate') noteCardTemplate!: TemplateRef<any>;

  notes: NoteDto[] = [];

  // Pagination state
  currentPage = 1;
  pageSize = 5;
  hasMoreNotes = false;
  loadingNotes = false;

  ngOnInit(): void {
    this.loadNotes();
  }

  loadNotes(): void {
    if (this.loadingNotes) return;
    
    this.loadingNotes = true;
    this.notesService.getNotesPaged(this.currentPage, this.pageSize).subscribe({
      next: (response: PagedResponseDto<NoteDto>) => {
        // Add new notes to existing array
        this.notes.push(...response.data);
        
        // Update pagination state
        this.hasMoreNotes = response.hasNextPage;
        this.currentPage++;
        this.loadingNotes = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingNotes = false;
      }
    });
  }

  loadMoreNotes(): void {
    this.loadNotes();
  }

  onNewNoteClick() {
    this.router.navigate(['/notes/new']);
  }

  onNoteSelected(rowKey: string) {
    this.router.navigate(['/notes/' + rowKey]);
  }
}
