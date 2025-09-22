import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { FilmDto } from '../../DTOs';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FilmService extends BaseService {
  constructor() { super('Film'); }

  addNewFilm(newFilm: FilmDto) {
    return this.post('', newFilm);
  }

  getAllFilms(): Observable<FilmDto[]> {
    return this.get<FilmDto[]>('');
  }

  getFilm(rowKey: string): Observable<FilmDto> {
    return this.get<FilmDto>(rowKey)
  }

  updateFilm(rowKey: string , updateFilm: FilmDto) {
    return this.put(rowKey, updateFilm);
  }

  deleteFilm(rowKey: string){
    return this.delete(rowKey);
  }
}
