import { Injectable } from '@angular/core';
import { NoteDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class NotesService extends BasePaginatedService<NoteDto> {
  constructor() { super('Notes'); }

  // Specific notes methods using base service patterns
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

  getNote(rowKey: string): Observable<NoteDto> { return this.getById(rowKey); }
  updateNote(rowKey: string, updateNote: NoteDto) { return this.update(rowKey, updateNote); }
  deleteNote(rowKey: string) { return this.deleteById(rowKey); }
}
