import { Injectable } from '@angular/core';
import { NoteDto, PagedResponseDto, MergedNoteDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class NotesService extends BasePaginatedService<NoteDto> {
  constructor() { super('Notes'); }

  // Notes-specific methods that override or extend base functionality
  addNewNote(newNote: NoteDto): Observable<string> {
    return this.post('', newNote, { responseType: 'text' });
  }

  getAllNotes(withEntries: boolean = false): Observable<NoteDto[]> {
    const query = withEntries ? '?withEntries=true&page=0' : '?page=0';
    return this.get<NoteDto[]>(query);
  }

  getNotesPaged(page: number = 1, pageSize: number = 5, withEntries: boolean = false): Observable<PagedResponseDto<NoteDto>> {
    return this.getFilteredPaged('', page, pageSize, withEntries ? { withEntries: 'true' } : undefined);
  }

  getMergedNotes(compositeId: string): Observable<MergedNoteDto> {
    return this.get<MergedNoteDto>(`merge/${compositeId}`);
  }

  // Note: getById(), update(), deleteById() are inherited from BasePaginatedService
}
