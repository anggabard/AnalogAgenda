import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UsedFilmThumbnailDto } from '../../DTOs';
import { BaseService } from '../base.service';

@Injectable({
  providedIn: 'root'
})
export class UsedFilmThumbnailService extends BaseService {
  constructor() { 
    super('UsedFilmThumbnail'); 
  }

  searchByFilmName(filmName: string): Observable<UsedFilmThumbnailDto[]> {
    return this.get<UsedFilmThumbnailDto[]>(`/search?filmName=${encodeURIComponent(filmName)}`);
  }

  uploadThumbnail(filmName: string, imageBase64: string): Observable<UsedFilmThumbnailDto> {
    const dto: UsedFilmThumbnailDto = {
      id: '',
      filmName: filmName,
      imageId: '',
      imageUrl: '',
      imageBase64: imageBase64
    };
    return this.post<UsedFilmThumbnailDto>('', dto);
  }
}

