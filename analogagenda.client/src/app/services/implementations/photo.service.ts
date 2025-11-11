import { Injectable, inject } from '@angular/core';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
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

  // Create a single photo via Azure Function
  createPhoto(photoDto: PhotoCreateDto): Observable<PhotoDto> {
    return this.http.post<PhotoDto>(`${environment.functionsUrl}/api/photo/upload`, photoDto);
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
   * Upload multiple photos in parallel with smart indexing and live callback
   * (Azure Functions can handle parallel uploads with auto-scaling)
   * @param filmId The ID of the film
   * @param files The files to upload
   * @param existingPhotos Existing photos for the film (to calculate next available index)
   * @param onPhotoUploaded Optional callback called after each photo uploads successfully
   * @returns Promise that resolves when all uploads complete
   */
  async uploadMultiplePhotos(
    filmId: string,
    files: FileList | File[],
    existingPhotos: PhotoDto[],
    onPhotoUploaded?: (uploadedPhoto: PhotoDto, current: number, total: number) => void
  ): Promise<void> {
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

    // Calculate next available index for files without explicit indices
    const nextAvailableIndex = existingPhotos.length === 0 
      ? 1 
      : Math.max(...existingPhotos.map(p => p.index)) + 1;
    let currentAutoIndex = nextAvailableIndex;

    // Prepare all upload promises
    const uploadPromises = filesWithIndices.map(async ({ file, index }, fileIndex) => {
      const base64 = await FileUploadHelper.fileToBase64(file);

      const photoDto: PhotoCreateDto = {
        filmId: filmId,
        imageBase64: base64,
        index: index !== null ? index : currentAutoIndex++
      };

      // Upload via Function (parallel uploads are now safe!)
      const uploadedPhoto = await lastValueFrom(this.createPhoto(photoDto));
      
      if (onPhotoUploaded) {
        onPhotoUploaded(uploadedPhoto, fileIndex + 1, fileArray.length);
      }
      
      return uploadedPhoto;
    });

    // Wait for all uploads to complete (Functions auto-scale to handle parallel requests)
    await Promise.all(uploadPromises);
  }
}
