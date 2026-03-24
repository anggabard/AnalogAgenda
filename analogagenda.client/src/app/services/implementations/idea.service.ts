import { Injectable } from '@angular/core';
import { IdeaDto, PhotoDto, IdListDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class IdeaService extends BasePaginatedService<IdeaDto> {
  constructor() { super('Idea'); }

  getPhotosForIdea(ideaId: string): Observable<PhotoDto[]> {
    return this.get<PhotoDto[]>(`${ideaId}/photos`);
  }

  addPhotosToIdea(ideaId: string, photoIds: string[]): Observable<PhotoDto[]> {
    const body: IdListDto = { ids: photoIds };
    return this.post<PhotoDto[]>(`${ideaId}/photos`, body);
  }

  removePhotoFromIdea(ideaId: string, photoId: string): Observable<void> {
    return this.delete<void>(`${ideaId}/photos/${photoId}`);
  }
}
