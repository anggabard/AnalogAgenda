import { Injectable } from '@angular/core';
import { FilmDto, PagedResponseDto, ExposureDateDto } from '../../DTOs';
import { Observable, EMPTY } from 'rxjs';
import { expand, reduce, map } from 'rxjs/operators';
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
  getDevelopedFilmsPaged(page: number = 1, pageSize: number = 20, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('developed', page, pageSize, searchParams);
  }

  getNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 20, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('not-developed', page, pageSize, searchParams);
  }

  getMyDevelopedFilmsPaged(page: number = 1, pageSize: number = 20, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/developed', page, pageSize, searchParams);
  }

  getMyNotDevelopedFilmsPaged(page: number = 1, pageSize: number = 20, searchParams?: SearchParams): Observable<PagedResponseDto<FilmDto>> {
    return this.getFilteredPaged('my/not-developed', page, pageSize, searchParams);
  }

  getExposureDates(filmId: string): Observable<ExposureDateDto[]> {
    return this.get<ExposureDateDto[]>(`${filmId}/exposure-dates`);
  }

  updateExposureDates(filmId: string, exposureDates: ExposureDateDto[]): Observable<void> {
    return this.put(`${filmId}/exposure-dates`, exposureDates);
  }

  /** Get all not-developed films (all users). Fetches all pages to avoid pagination limiting results. */
  getNotDevelopedFilms(): Observable<FilmDto[]> {
    const pageSize = 500;
    return this.getFilteredPaged('not-developed', 1, pageSize).pipe(
      expand((res) =>
        res.hasNextPage
          ? this.getFilteredPaged('not-developed', res.currentPage + 1, pageSize)
          : EMPTY
      ),
      reduce((acc: FilmDto[], res) => acc.concat(res.data), [] as FilmDto[])
    );
  }

  /** Get all developed films (all users). Backend returns full list when page=0. */
  getDevelopedFilmsAll(): Observable<FilmDto[]> {
    return this.get<FilmDto[]>('developed?page=0');
  }

  /** Get all developed films for the current user. Backend returns full list when page=0. */
  getMyDevelopedFilmsAll(): Observable<FilmDto[]> {
    return this.get<FilmDto[]>('my/developed?page=0');
  }

}
