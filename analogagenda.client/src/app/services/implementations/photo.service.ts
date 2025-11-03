import { Injectable } from '@angular/core';
import { PhotoDto, PhotoBulkUploadDto, PhotoCreateDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BaseService } from '../base.service';

@Injectable({
  providedIn: 'root'
})
export class PhotoService extends BaseService {
  constructor() { super('Photo'); }

  // Create a single photo
  createPhoto(photoDto: PhotoCreateDto): Observable<PhotoDto> {
    return this.post<PhotoDto>('', photoDto);
  }

  // Upload multiple photos for a film
  uploadPhotos(uploadDto: PhotoBulkUploadDto): Observable<void> {
    return this.post<void>('bulk', uploadDto);
  }

  // Get all photos for a specific film
  getPhotosByFilmId(filmId: string): Observable<PhotoDto[]> {
    return this.get<PhotoDto[]>(`film/${filmId}`);
  }

  // Download a single photo
  downloadPhoto(id: string): Observable<Blob> {
    return this.get<Blob>(`download/${id}`, { responseType: 'blob' });
  }

  // Download all photos for a film as zip
  downloadAllPhotos(filmId: string): Observable<Blob> {
    return this.get<Blob>(`download-all/${filmId}`, { responseType: 'blob' });
  }

  // Delete a photo
  deletePhoto(id: string): Observable<any> {
    return this.delete(id);
  }
}
