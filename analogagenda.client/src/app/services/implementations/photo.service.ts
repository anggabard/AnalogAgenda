import { Injectable } from '@angular/core';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
import { Observable, lastValueFrom } from 'rxjs';
import { BaseService } from '../base.service';
import { FileUploadHelper } from '../../helpers/file-upload.helper';

@Injectable({
  providedIn: 'root'
})
export class PhotoService extends BaseService {
  constructor() { super('Photo'); }

  // Create a single photo
  createPhoto(photoDto: PhotoCreateDto): Observable<PhotoDto> {
    return this.post<PhotoDto>('', photoDto);
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

  // Get preview URL for a photo (returns minified version)
  getPreviewUrl(photoId: string): string {
    return `${this.baseUrl}/preview/${photoId}`;
  }

  /**
   * Upload multiple photos in parallel with smart indexing
   * @param filmId The ID of the film
   * @param files The files to upload
   * @param existingPhotos Existing photos for the film (to calculate next available index)
   * @param onProgress Optional callback for progress updates
   * @returns Promise that resolves when all uploads complete
   */
  async uploadMultiplePhotos(
    filmId: string,
    files: FileList | File[],
    existingPhotos: PhotoDto[],
    onProgress?: (current: number, total: number) => void
  ): Promise<void> {
    const fileArray = Array.from(files);
    let uploadedCount = 0;

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

    // Prepare all photo uploads
    const uploadPromises = filesWithIndices.map(async ({ file, index }) => {
      const base64 = await FileUploadHelper.fileToBase64(file);

      const photoDto: PhotoCreateDto = {
        filmId: filmId,
        imageBase64: base64,
        index: index !== null ? index : currentAutoIndex++
      };

      await lastValueFrom(this.createPhoto(photoDto));
      
      uploadedCount++;
      if (onProgress) {
        onProgress(uploadedCount, fileArray.length);
      }
    });

    // Send all uploads in parallel
    await Promise.all(uploadPromises);
  }
}
