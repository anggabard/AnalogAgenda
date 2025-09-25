import { Component, ViewChild, TemplateRef } from "@angular/core";
import { Observable } from "rxjs";
import { BasePaginatedListComponent } from "../common/base-paginated-list/base-paginated-list.component";
import { NotesService } from "../../services";
import { NoteDto, PagedResponseDto } from "../../DTOs";

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrl: './notes.component.css'
})
export class NotesComponent extends BasePaginatedListComponent<NoteDto> {

  constructor(private notesService: NotesService) {
    super();
  }

  @ViewChild('noteCardTemplate') declare cardTemplate: TemplateRef<any>;

  protected getItemsObservable(page: number, pageSize: number): Observable<PagedResponseDto<NoteDto>> {
    return this.notesService.getNotesPaged(page, pageSize);
  }

  protected getBaseRoute(): string {
    return '/notes';
  }

  protected getRowKey(note: NoteDto): string {
    return note.rowKey;
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

  onNoteSelected(rowKey: string): void {
    this.router.navigate(['/notes', rowKey]);
  }

  onNewNoteClick(): void {
    this.onNewItemClick();
  }
}
