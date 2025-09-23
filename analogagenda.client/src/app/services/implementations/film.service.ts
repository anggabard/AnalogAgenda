import { Injectable } from '@angular/core';
import { FilmDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class FilmService extends BasePaginatedService<FilmDto> {
  constructor() { super('Film'); }

  // Specific film methods using base service patterns
  addNewFilm(newFilm: FilmDto) { return this.add(newFilm); }
  getAllFilms(): Observable<FilmDto[]> { return this.getAll(); }
  getFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> { 
    return this.getPaged(page, pageSize); 
  }
  getFilm(rowKey: string): Observable<FilmDto> { return this.getById(rowKey); }
  updateFilm(rowKey: string, updateFilm: FilmDto) { return this.update(rowKey, updateFilm); }
  deleteFilm(rowKey: string) { return this.deleteById(rowKey); }

  // Film-specific filtered pagination methods
  getDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('developed', page, pageSize);
  }

  getNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('not-developed', page, pageSize);
  }

  getMyDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/developed', page, pageSize);
  }

  getMyNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/not-developed', page, pageSize);
  }
}
