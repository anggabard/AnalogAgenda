import { Component, ViewChild, TemplateRef } from "@angular/core";
import { Observable } from "rxjs";
import { BasePaginatedListComponent } from "../common/base-paginated-list/base-paginated-list.component";
import { NotesService } from "../../services";
import { NoteDto, PagedResponseDto } from "../../DTOs";

@Component({
    selector: 'app-notes',
    templateUrl: './notes.component.html',
    styleUrl: './notes.component.css',
    standalone: false
})
export class NotesComponent extends BasePaginatedListComponent<NoteDto> {

  // Merge modal state
  isMergeModalOpen = false;
  selectedNotesForMerge: Set<string> = new Set();

  constructor(private notesService: NotesService) {
    super();
  }

  @ViewChild('noteCardTemplate') declare cardTemplate: TemplateRef<any>;
  @ViewChild('noteRowTemplate') noteRowTemplate!: TemplateRef<any>;

  noteTableHeaders = ['Name', 'Preview'];

  protected getItemsObservable(page: number, pageSize: number): Observable<PagedResponseDto<NoteDto>> {
    return this.notesService.getNotesPaged(page, pageSize);
  }

  protected getBaseRoute(): string {
    return '/notes';
  }

  protected getId(note: NoteDto): string {
    return note.id;
  }

  // Alias methods for template compatibility
  get notes(): NoteDto[] { 
    return this.items; 
  }

  get hasMoreNotes(): boolean { 
    return this.hasMore; 
  }

  get loadingNotes(): boolean { 
    return this.loading; 
  }

  loadMoreNotes(): void {
    this.loadMoreItems();
  }

  onNoteSelected(id: string): void {
    this.router.navigate(['/notes', id]);
  }

  onNewNoteClick(): void {
    this.onNewItemClick();
  }

  // Merge functionality
  openMergeModal(): void {
    this.isMergeModalOpen = true;
    this.selectedNotesForMerge.clear();
  }

  closeMergeModal(): void {
    this.isMergeModalOpen = false;
    this.selectedNotesForMerge.clear();
  }

  toggleNoteSelection(noteId: string): void {
    if (this.selectedNotesForMerge.has(noteId)) {
      this.selectedNotesForMerge.delete(noteId);
    } else {
      this.selectedNotesForMerge.add(noteId);
    }
  }

  isNoteSelected(noteId: string): boolean {
    return this.selectedNotesForMerge.has(noteId);
  }

  canShowMergedNote(): boolean {
    return this.selectedNotesForMerge.size >= 2;
  }

  showMergedNote(): void {
    if (!this.canShowMergedNote()) return;

    const compositeId = this.generateCompositeId();
    this.router.navigate(['/notes/merge', compositeId]);
    this.closeMergeModal();
  }

  private generateCompositeId(): string {
    const selectedIds = Array.from(this.selectedNotesForMerge);
    const compositeId = [];
    
    // Interleave characters from each note's id
    // Note: NoteEntity has IdLength = 4, so each id is 4 characters
    for (let charIndex = 0; charIndex < 4; charIndex++) {
      for (const id of selectedIds) {
        if (charIndex < id.length) {
          compositeId.push(id[charIndex]);
        }
      }
    }
    
    return compositeId.join('');
  }
}
