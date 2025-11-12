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
   * Upload multiple photos sequentially to the backend API
   * @param filmId The ID of the film
   * @param files The files to upload
   * @param existingPhotos Existing photos for the film (to calculate next available index)
   * @param onPhotoUploaded Optional callback called after each photo uploads successfully
   * @returns Promise that resolves with array of upload results
   */
  async uploadMultiplePhotos(
    filmId: string,
    files: FileList | File[],
    existingPhotos: PhotoDto[],
    onPhotoUploaded?: (uploadedPhoto: PhotoDto) => void
  ): Promise<Array<{ success: boolean; photo?: PhotoDto; error?: string }>> {
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

    // Calculate next available index for files without indices
    const maxExistingIndex = existingPhotos.length > 0 
      ? Math.max(...existingPhotos.map(p => p.index))
      : 0;
    let nextAvailableIndex = maxExistingIndex + 1;

    const results: Array<{ success: boolean; photo?: PhotoDto; error?: string }> = [];

    // Process files sequentially (one at a time)
    for (const { file, index } of filesWithIndices) {
      try {
        // Convert file to base64
        const base64 = await FileUploadHelper.fileToBase64(file);
        
        // If index is null, assign next available index
        const assignedIndex = index !== null ? index : nextAvailableIndex++;
        
        const photoDto: PhotoCreateDto = {
          filmId: filmId,
          imageBase64: base64,
          index: assignedIndex
        };

        // Send request to backend API (sequential - wait for each to complete)
        const uploadedPhoto = await lastValueFrom(
          this.post<PhotoDto>('', photoDto)
        );
        
        // Call callback immediately after successful upload
        if (onPhotoUploaded) {
          onPhotoUploaded(uploadedPhoto);
        }
        
        results.push({
          success: true,
          photo: uploadedPhoto
        });
      } catch (error: any) {
        const errorMessage = error?.error?.error || error?.message || 'Unknown error';
        results.push({
          success: false,
          error: errorMessage
        });
      }
    }

    return results;
  }
}
