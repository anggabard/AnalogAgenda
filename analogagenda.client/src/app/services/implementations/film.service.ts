import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { FilmDto, PagedResponseDto } from '../../DTOs';
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
    return this.get<FilmDto[]>('?page=0'); // page=0 for backward compatibility to get all films
  }

  getFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.get<PagedResponseDto<FilmDto>>(`?page=${page}&pageSize=${pageSize}`);
  }

  getDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.get<PagedResponseDto<FilmDto>>(`developed?page=${page}&pageSize=${pageSize}`);
  }

  getNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.get<PagedResponseDto<FilmDto>>(`not-developed?page=${page}&pageSize=${pageSize}`);
  }

  getMyDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.get<PagedResponseDto<FilmDto>>(`my/developed?page=${page}&pageSize=${pageSize}`);
  }

  getMyNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.get<PagedResponseDto<FilmDto>>(`my/not-developed?page=${page}&pageSize=${pageSize}`);
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
