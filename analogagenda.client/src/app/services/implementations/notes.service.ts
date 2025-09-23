import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { NoteDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NotesService extends BaseService {
  constructor() { super('Notes'); }

  addNewNote(newNote: NoteDto): Observable<string> {
    return this.post('', newNote, { responseType: 'text' });
  }

  getAllNotes(withEntries: boolean = false): Observable<NoteDto[]> {
    const query = withEntries ? '?withEntries=true&page=0' : '?page=0'; // page=0 for backward compatibility
    return this.get<NoteDto[]>(query);
  }

  getNotesPaged(page: number = 1, pageSize: number = 5, withEntries: boolean = false): Observable<PagedResponseDto<NoteDto>> {
    const query = withEntries ? 
      `?page=${page}&pageSize=${pageSize}&withEntries=true` : 
      `?page=${page}&pageSize=${pageSize}`;
    return this.get<PagedResponseDto<NoteDto>>(query);
  }

  getNote(rowKey: string): Observable<NoteDto> {
    return this.get<NoteDto>(rowKey)
  }

  updateNote(rowKey: string, updateNote: NoteDto) {
    return this.put(rowKey, updateNote);
  }

  deleteNote(rowKey: string) {
    return this.delete(rowKey);
  }
}
