import { Injectable } from '@angular/core';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
import { Observable, lastValueFrom, throwError, timer } from 'rxjs';
import { retryWhen, mergeMap } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
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
  downloadAllPhotos(filmId: string, small: boolean = false): Observable<Blob> {
    const params = small ? { params: { small: 'true' } } : {};
    return this.get<Blob>(`download-all/${filmId}`, { responseType: 'blob', ...params });
  }

  // Delete a photo
  deletePhoto(id: string): Observable<any> {
    return this.delete(id);
  }

  // Set restricted access for a photo (owner only)
  setRestricted(id: string, restricted: boolean): Observable<PhotoDto> {
    return this.patch<PhotoDto>(`${id}/restricted`, { restricted });
  }

  // Get preview URL for a photo (returns direct blob storage URL)
  getPreviewUrl(photo: PhotoDto): string {
    return photo.imageUrl.replace("photos/", "photos/preview/")
  }

  /**
   * Upload multiple photos in parallel to the backend API
   * @param filmId The ID of the film
   * @param files The files to upload
   * @param existingPhotos Existing photos for the film (to calculate next available index)
   * @param onPhotoUploaded Optional callback called after each photo uploads successfully
   * @param concurrency Number of parallel uploads (default: 5, matches scaling rule)
   * @returns Promise that resolves with array of upload results
   */
  async uploadMultiplePhotos(
    filmId: string,
    files: FileList | File[],
    existingPhotos: PhotoDto[],
    onPhotoUploaded?: (uploadedPhoto: PhotoDto) => void,
    concurrency: number = 5
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
    let nextAvailableIndexLock = 0; // Simple lock for index assignment

    const results: Array<{ success: boolean; photo?: PhotoDto; error?: string }> = [];
    
    // Helper function to upload a single photo
    const uploadPhoto = async (file: File, index: number | null): Promise<{ success: boolean; photo?: PhotoDto; error?: string }> => {
      try {
        // Convert file to base64
        const base64 = await FileUploadHelper.fileToBase64(file);
        
        // If index is null, assign next available index (thread-safe)
        let assignedIndex: number;
        if (index !== null) {
          assignedIndex = index;
        } else {
          // Simple lock mechanism for index assignment
          while (nextAvailableIndexLock !== 0) {
            await new Promise(resolve => setTimeout(resolve, 10));
          }
          nextAvailableIndexLock = 1;
          assignedIndex = nextAvailableIndex++;
          nextAvailableIndexLock = 0;
        }

        const photoDto: PhotoCreateDto = {
          filmId: filmId,
          imageBase64: base64,
          index: assignedIndex
        };

        // Send request to backend API with retry logic for 5xx server errors
        const uploadedPhoto = await lastValueFrom(
          this.post<PhotoDto>('', photoDto).pipe(
            retryWhen(errors =>
              errors.pipe(
                mergeMap((error: HttpErrorResponse, retryIndex: number) => {
                  // Retry on 5xx server errors (up to 3 attempts)
                  if (error.status >= 500 && error.status < 600 && retryIndex < 3) {
                    // Exponential backoff: 2s, 4s, 8s
                    const delayMs = Math.pow(2, retryIndex + 1) * 1000;
                    console.warn(`Upload failed with ${error.status}, retrying in ${delayMs}ms... (attempt ${retryIndex + 1}/3)`);
                    return timer(delayMs);
                  }
                  // Don't retry for other errors or after max retries
                  return throwError(() => error);
                })
              )
            )
          )
        );
        
        // Call callback immediately after successful upload
        if (onPhotoUploaded) {
          onPhotoUploaded(uploadedPhoto);
        }
        
        return {
          success: true,
          photo: uploadedPhoto
        };
      } catch (error: any) {
        const errorMessage = error?.error?.error || error?.message || 'Unknown error';
        const status = error?.status;
        
        // Provide user-friendly error messages
        if (status >= 500 && status < 600) {
          return {
            success: false,
            error: `Server error occurred. Please try again later.`
          };
        } else if (status === 401) {
          return {
            success: false,
            error: `Authentication failed. Please log in again.`
          };
        } else {
          return {
            success: false,
            error: errorMessage
          };
        }
      }
    };

    // Process files with concurrency control using a semaphore pattern
    const semaphore = { count: concurrency };
    const queue: Array<() => Promise<void>> = [];

    const runWithSemaphore = async <T>(fn: () => Promise<T>): Promise<T> => {
      return new Promise((resolve, reject) => {
        const run = async () => {
          semaphore.count--;
          try {
            const result = await fn();
            resolve(result);
          } catch (error) {
            reject(error);
          } finally {
            semaphore.count++;
            if (queue.length > 0) {
              const next = queue.shift()!;
              next();
            }
          }
        };

        if (semaphore.count > 0) {
          run();
        } else {
          queue.push(run);
        }
      });
    };

    // Process all files with concurrency control
    const uploadPromises = filesWithIndices.map(({ file, index }) =>
      runWithSemaphore(() => uploadPhoto(file, index))
    );

    // Wait for all uploads to complete
    const uploadResults = await Promise.all(uploadPromises);
    results.push(...uploadResults);

    // Sort results by index to maintain order
    results.sort((a, b) => {
      const indexA = a.photo?.index ?? 9999;
      const indexB = b.photo?.index ?? 9999;
      return indexA - indexB;
    });

    return results;
  }
}
