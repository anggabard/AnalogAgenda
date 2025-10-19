import { Injectable } from '@angular/core';
import { FilmDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';
import { SearchParams } from '../../components/films/film-search/film-search.component';

@Injectable({
  providedIn: 'root'
})
export class FilmService extends BasePaginatedService<FilmDto> {
  constructor() { super('Film'); }

  // Note: Basic CRUD methods are inherited from BasePaginatedService
  // Use add(), getAll(), getPaged(), getById(), update(), deleteById() directly

  // Film-specific filtered pagination methods
  getDevelopedFilmsPaged(page: number = 1, pageSize: number = 5, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('developed', page, pageSize, searchParams);
  }

  getNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('not-developed', page, pageSize, searchParams);
  }

  getMyDevelopedFilmsPaged(page: number = 1, pageSize: number = 5, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/developed', page, pageSize, searchParams);
  }

  getMyNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 5, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/not-developed', page, pageSize, searchParams);
  }

}
