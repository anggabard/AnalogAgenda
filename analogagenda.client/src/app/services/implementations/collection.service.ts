import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from '../base.service';
import { CollectionDto, CollectionOptionDto, PhotoDto, PagedResponseDto } from '../../DTOs';

@Injectable({
  providedIn: 'root',
})
export class CollectionService extends BaseService {
  constructor() {
    super('Collection');
  }

  /** Newest first (server). Same paging contract as films. */
  getMinePaged(page: number, pageSize: number): Observable<PagedResponseDto<CollectionDto>> {
    return this.get<PagedResponseDto<CollectionDto>>(`?page=${page}&pageSize=${pageSize}`);
  }

  getOpenOptions(): Observable<CollectionOptionDto[]> {
    return this.get<CollectionOptionDto[]>('open');
  }

  getById(id: string): Observable<CollectionDto> {
    return this.get<CollectionDto>(id);
  }

  /** Photos in the collection (owner), ordered for featured picker. */
  getPhotos(id: string): Observable<PhotoDto[]> {
    return this.get<PhotoDto[]>(`${id}/photos`);
  }

  create(dto: CollectionDto): Observable<CollectionDto> {
    return this.post<CollectionDto>('', dto);
  }

  update(id: string, dto: CollectionDto): Observable<CollectionDto> {
    return this.put<CollectionDto>(id, dto);
  }

  deleteById(id: string): Observable<void> {
    return this.delete<void>(id);
  }

  appendPhotos(collectionId: string, photoIds: string[]): Observable<CollectionDto> {
    return this.post<CollectionDto>(`${collectionId}/photos`, { ids: photoIds });
  }

  downloadArchive(id: string, small: boolean = false): Observable<Blob> {
    const q = small ? '?small=true' : '';
    return this.get<Blob>(`${id}/download${q}`, { responseType: 'blob' });
  }
}
