import { Injectable, inject } from '@angular/core';
import { PhotoDto, PhotoCreateDto, UploadKeyDto } from '../../DTOs';
import { Observable, lastValueFrom } from 'rxjs';
import { BaseService } from '../base.service';
import { FileUploadHelper } from '../../helpers/file-upload.helper';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PhotoService extends BaseService {
  private http = inject(HttpClient);

  constructor() { super('Photo'); }

  // Get upload key for a film
  getUploadKey(filmId: string): Observable<UploadKeyDto> {
    return this.get<UploadKeyDto>(`UploadKey?filmId=${filmId}`);
  }

  // Create a single photo via Azure Function
  createPhoto(photoDto: PhotoCreateDto, key: string, keyId: string): Observable<PhotoDto> {
    const url = `${environment.functionsUrl}/api/photo/upload?Key=${encodeURIComponent(key)}&KeyId=${encodeURIComponent(keyId)}`;
    // Don't use withCredentials for Azure Function (different domain, CORS handled by Function)
    return this.http.post<PhotoDto>(url, photoDto, {
      withCredentials: false
    });
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

  // Get preview URL for a photo (returns direct blob storage URL)
  getPreviewUrl(photo: PhotoDto): string {
    // Extract account name and imageId from ImageUrl
    // ImageUrl format: https://{accountName}.blob.core.windows.net/photos/{imageId}
    const url = new URL(photo.imageUrl);
    const pathParts = url.pathname.split('/').filter(p => p);
    const imageId = pathParts[1];
    const accountName = url.hostname.split('.')[0];
    // Construct preview URL: https://{accountName}.blob.core.windows.net/photos/preview/{imageId}
    return `https://${accountName}.blob.core.windows.net/photos/preview/${imageId}`;
  }

  /**
   * Upload multiple photos in a single batch request using Durable Functions
   * @param filmId The ID of the film
   * @param files The files to upload
   * @param existingPhotos Existing photos for the film (to calculate next available index)
   * @param onPhotoUploaded Optional callback called after each photo uploads successfully (may not be called immediately due to async processing)
   * @returns Promise that resolves with orchestration instance ID when batch upload is started
   */
  async uploadMultiplePhotos(
    filmId: string,
    files: FileList | File[],
    existingPhotos: PhotoDto[],
    onPhotoUploaded?: (uploadedPhoto: PhotoDto, current: number, total: number) => void
  ): Promise<{ instanceId: string; statusQueryGetUri: string }> {
    // Get upload key and keyId first
    const { key, keyId } = await lastValueFrom(this.getUploadKey(filmId));

    const fileArray = Array.from(files);

    // Extract indices from filenames
    const filesWithIndices = fileArray.map(file => ({
      file,
      index: FileUploadHelper.extractIndexFromFilename(file.name)
    }));

    // Sort by index (nulls go to end)
    filesWithIndices.sort((a, b) => {
      if (a.index === null && b.index === null) return 0;
      if (a.index === null) return 1;
      if (b.index === null) return -1;
      return a.index - b.index;
    });

    // Convert all files to base64 and create PhotoCreateDto array
    const photoDtos: PhotoCreateDto[] = await Promise.all(
      filesWithIndices.map(async ({ file, index }) => {
        const base64 = await FileUploadHelper.fileToBase64(file);
        return {
          filmId: filmId,
          imageBase64: base64,
          index: index !== null ? index : undefined
        };
      })
    );

    // Create batch upload DTO
    const batchDto = {
      key: key,
      keyId: keyId,
      filmId: filmId,
      photos: photoDtos
    };

    // Send batch request to Azure Function
    const url = `${environment.functionsUrl}/api/photo/upload`;
    const response = await lastValueFrom(
      this.http.post<{ instanceId: string; statusQueryGetUri: string }>(url, batchDto, {
        withCredentials: false
      })
    );

    // Note: onPhotoUploaded callback may not work as expected since processing is async
    // The caller can poll the statusQueryGetUri to check progress if needed
    // For now, we return the instance ID and status URL

    return response;
  }
}
