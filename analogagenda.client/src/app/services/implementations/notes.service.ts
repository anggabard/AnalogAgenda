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
    return this.post('', newNote, {responseType: 'text'});
  }

  // getAllDevKits(): Observable<DevKitDto[]> {
  //   return this.get<DevKitDto[]>('');
  // }

  // getKit(rowKey: string): Observable<DevKitDto> {
  //   return this.get<DevKitDto>(rowKey)
  // }

  updateNote(rowKey: string , updateNote: NoteDto) {
    return this.put(rowKey, updateNote);
  }
}
