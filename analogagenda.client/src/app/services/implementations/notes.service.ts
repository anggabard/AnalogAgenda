import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { NoteDto } from '../../DTOs';
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
    return this.get<NoteDto[]>(withEntries ? 'withEntries=true' : '');
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
