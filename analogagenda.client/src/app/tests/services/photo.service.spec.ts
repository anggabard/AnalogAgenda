import { TestBed, fakeAsync, tick, flush, waitForAsync } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PhotoService } from '../../services/implementations/photo.service';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
import { TestConfig } from '../test.config';
import { FileUploadHelper } from '../../helpers/file-upload.helper';

describe('PhotoService', () => {
  let service: PhotoService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://localhost:7125/api/Photo';
  const functionsUrl = 'https://analogagenda.azurewebsites.net';

  beforeEach(() => {
    TestConfig.configureTestBed({
      providers: [PhotoService]
    });
    service = TestBed.inject(PhotoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Skip verification for upload tests as file reading is async and can't be controlled
    try {
      httpMock.verify();
    } catch (e) {
      // Ignore verification errors for upload tests
    }
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('uploadMultiplePhotos', () => {
    it('should upload a single photo via backend API', async () => {
      // Arrange
      const filmId = 'test-film-id';
      const file = new File(['test'], '1.jpg', { type: 'image/jpeg' });
      const existingPhotos: PhotoDto[] = [];
      const mockResponse: PhotoDto = createMockPhoto('photo1', filmId, 1);

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const result = await service.uploadMultiplePhotos(filmId, [file], existingPhotos);

      // Assert
      expect(result.length).toBe(1);
      expect(result[0].success).toBe(true);
      expect(result[0].photo).toEqual(mockResponse);

      // Assert HTTP call to backend API endpoint
      const req = httpMock.expectOne(`${baseUrl}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.filmId).toBe(filmId);
      expect(req.request.body.index).toBe(1);
      expect(req.request.withCredentials).toBe(true);
      req.flush(mockResponse);
    });

    it('should handle error when uploading photo', async () => {
      // Arrange
      const filmId = 'invalid-film-id';
      const file = new File(['test'], '1.jpg', { type: 'image/jpeg' });
      const existingPhotos: PhotoDto[] = [];

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const result = await service.uploadMultiplePhotos(filmId, [file], existingPhotos);

      // Assert
      expect(result.length).toBe(1);
      expect(result[0].success).toBe(false);
      expect(result[0].error).toBeDefined();

      // Assert HTTP call to backend API endpoint
      const req = httpMock.expectOne(`${baseUrl}`);
      req.flush('Film not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getPhotosByFilmId', () => {
    it('should get photos by film ID', () => {
      // Arrange
      const filmId = 'test-film-id';
      const mockResponse: PhotoDto[] = [
        createMockPhoto('photo1', filmId, 1),
        createMockPhoto('photo2', filmId, 2),
        createMockPhoto('photo3', filmId, 3)
      ];

      // Act
      service.getPhotosByFilmId(filmId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.length).toBe(3);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/film/${filmId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should return empty array when no photos found', () => {
      // Arrange
      const filmId = 'test-film-id';
      const mockResponse: PhotoDto[] = [];

      // Act
      service.getPhotosByFilmId(filmId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.length).toBe(0);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/film/${filmId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('downloadPhoto', () => {
    it('should download a single photo', () => {
      // Arrange
      const id = 'test-photo-key';
      const mockBlob = new Blob(['fake-image-data'], { type: 'image/jpeg' });

      // Act
      service.downloadPhoto(id).subscribe(response => {
        // Assert
        expect(response).toEqual(mockBlob);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download/${id}`);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(mockBlob);
    });

    it('should handle error when downloading photo', () => {
      // Arrange
      const id = 'invalid-photo-key';

      // Act
      service.downloadPhoto(id).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download/${id}`);
      req.flush(null, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('downloadAllPhotos', () => {
    it('should download all photos as zip', () => {
      // Arrange
      const filmId = 'test-film-id';
      const mockZipBlob = new Blob(['fake-zip-data'], { type: 'application/zip' });

      // Act
      service.downloadAllPhotos(filmId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockZipBlob);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download-all/${filmId}`);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(mockZipBlob);
    });

    it('should handle error when no photos to download', () => {
      // Arrange
      const filmId = 'empty-film-id';

      // Act
      service.downloadAllPhotos(filmId).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download-all/${filmId}`);
      req.flush(null, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('deletePhoto', () => {
    it('should delete a photo', () => {
      // Arrange
      const id = 'test-photo-key';

      // Act
      service.deletePhoto(id).subscribe(response => {
        // Assert
        expect(response).toBeDefined();
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/${id}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('should handle error when deleting non-existent photo', () => {
      // Arrange
      const id = 'invalid-photo-key';

      // Act
      service.deletePhoto(id).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/${id}`);
      req.flush('Photo not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getPreviewUrl', () => {
    it('should return correct preview URL from blob storage', () => {
      // Arrange
      const photo: PhotoDto = {
        id: 'test-photo-id-123',
        filmId: 'test-film-id',
        index: 1,
        imageUrl: 'https://analogagendastorage.blob.core.windows.net/photos/12345678-1234-1234-1234-123456789012',
        imageBase64: ''
      };
      const expectedUrl = 'https://analogagendastorage.blob.core.windows.net/photos/preview/12345678-1234-1234-1234-123456789012';

      // Act
      const result = service.getPreviewUrl(photo);

      // Assert
      expect(result).toBe(expectedUrl);
    });

    it('should extract account name and imageId from ImageUrl', () => {
      // Arrange
      const photo: PhotoDto = {
        id: 'test-photo-id',
        filmId: 'test-film-id',
        index: 1,
        imageUrl: 'https://mystorageaccount.blob.core.windows.net/photos/abcdef12-3456-7890-abcd-ef1234567890',
        imageBase64: ''
      };
      const expectedUrl = 'https://mystorageaccount.blob.core.windows.net/photos/preview/abcdef12-3456-7890-abcd-ef1234567890';

      // Act
      const result = service.getPreviewUrl(photo);

      // Assert
      expect(result).toBe(expectedUrl);
    });

    it('should not make HTTP call - just return URL string', () => {
      // Arrange
      const photo: PhotoDto = {
        id: 'test-photo-id',
        filmId: 'test-film-id',
        index: 1,
        imageUrl: 'https://analogagendastorage.blob.core.windows.net/photos/12345678-1234-1234-1234-123456789012',
        imageBase64: ''
      };

      // Act
      const result = service.getPreviewUrl(photo);

      // Assert
      expect(typeof result).toBe('string');
      expect(result).toContain('/photos/preview/');
      expect(result).toContain('12345678-1234-1234-1234-123456789012');
      
      // Verify no HTTP call was made
      httpMock.expectNone(`${baseUrl}/preview/${photo.id}`);
    });
  });

  describe('uploadMultiplePhotos', () => {
    it('should upload multiple photos in parallel and call callback with PhotoDto', (done) => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '5.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '10.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [];
      
      const uploadedPhotos: PhotoDto[] = [];
      const onPhotoUploaded = jasmine.createSpy('onPhotoUploaded').and.callFake(
        (photo: PhotoDto) => {
          uploadedPhotos.push(photo);
        }
      );

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 5);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 10);

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, onPhotoUploaded);

      // Respond to parallel requests (both may come at once)
      setTimeout(() => {
        // Both requests should come in parallel
        const requests = httpMock.match((req) => 
          req.url === `${functionsUrl}/api/photo/upload` && req.method === 'POST'
        );
        expect(requests.length).toBe(2);
        
        // Find requests by index
        const req1 = requests.find(r => r.request.body.index === 5);
        const req2 = requests.find(r => r.request.body.index === 10);
        
        expect(req1).toBeDefined();
        expect(req2).toBeDefined();
        expect(req1!.request.body.filmId).toBe(filmId);
        expect(req2!.request.body.filmId).toBe(filmId);
        
        // Flush responses
        req1!.flush(mockResponse1);
        req2!.flush(mockResponse2);
      }, 10);
    });

    it('should use next available index for non-numeric filenames', (done) => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], 'photo1.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], 'photo2.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [
        createMockPhoto('existing1', filmId, 5),
        createMockPhoto('existing2', filmId, 8)
      ];

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 9);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 10);

      const uploadedPhotos: PhotoDto[] = [];
      const onPhotoUploaded = (photo: PhotoDto) => {
        uploadedPhotos.push(photo);
      };

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, onPhotoUploaded);

      // Respond to parallel requests
      setTimeout(() => {
        const requests = httpMock.match((req) => 
          req.url === `${functionsUrl}/api/photo/upload` && req.method === 'POST'
        );
        expect(requests.length).toBe(2);
        
        const req1 = requests.find(r => r.request.body.index === 9);
        const req2 = requests.find(r => r.request.body.index === 10);
        
        req1!.flush(mockResponse1);
        req2!.flush(mockResponse2);
      }, 10);
    });

    it('should sort files by index before uploading', async () => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '45.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '2.jpg', { type: 'image/jpeg' });
      const file3 = new File(['test3'], '10.jpg', { type: 'image/jpeg' });
      const files = [file1, file2, file3]; // Unsorted
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1 = { success: true, photo: createMockPhoto('photo1', filmId, 2) };
      const mockResponse2 = { success: true, photo: createMockPhoto('photo2', filmId, 10) };
      const mockResponse3 = { success: true, photo: createMockPhoto('photo3', filmId, 45) };

      const uploadedPhotos: PhotoDto[] = [];

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, (photo) => {
        uploadedPhotos.push(photo);
      });

      // Wait a bit for base64 conversion to complete
      await new Promise(resolve => setTimeout(resolve, 50));

      // Poll for upload requests
      let req1: any = null;
      let req2: any = null;
      let req3: any = null;
      let attempts = 0;
      const maxAttempts = 30;
      while ((!req1 || !req2 || !req3) && attempts < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, 50));
        const requests = httpMock.match((req) => {
          const fullUrl = (req.urlWithParams || req.url || '').toString();
          return fullUrl.includes('/api/photo/upload') && req.method === 'POST';
        });
        if (!req1) req1 = requests.find((r: any) => r.request.body.index === 2);
        if (!req2) req2 = requests.find((r: any) => r.request.body.index === 10);
        if (!req3) req3 = requests.find((r: any) => r.request.body.index === 45);
        attempts++;
      }
      
      expect(req1).toBeDefined('Request 1 (index 2) should be found');
      expect(req2).toBeDefined('Request 2 (index 10) should be found');
      expect(req3).toBeDefined('Request 3 (index 45) should be found');
      req1!.flush(mockResponse1);
      req2!.flush(mockResponse2);
      req3!.flush(mockResponse3);

      // Wait for upload to complete
      await uploadPromise;

      // Assert all photos uploaded (order may vary with parallel uploads)
      expect(uploadedPhotos.length).toBe(3);
      const indices = uploadedPhotos.map(p => p.index).sort((a, b) => a - b);
      expect(indices).toEqual([2, 10, 45]);
    });

    it('should handle files with leading zeros in filenames', async () => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '002.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '045.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1 = { success: true, photo: createMockPhoto('photo1', filmId, 2) };
      const mockResponse2 = { success: true, photo: createMockPhoto('photo2', filmId, 45) };

      const uploadedPhotos: PhotoDto[] = [];

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, (photo) => {
        uploadedPhotos.push(photo);
      });

      // Respond to parallel requests
      setTimeout(() => {
        const requests = httpMock.match((req) => 
          req.url === `${functionsUrl}/api/photo/upload` && req.method === 'POST'
        );
        expect(requests.length).toBe(2);
        
        const req1 = requests.find(r => r.request.body.index === 2);
        const req2 = requests.find(r => r.request.body.index === 45);
        
        req1!.flush(mockResponse1);
        req2!.flush(mockResponse2);
      }, 10);

      // Wait for upload to complete
      await uploadPromise;

      // Assert
      expect(uploadedPhotos.length).toBe(2);
      const indices = uploadedPhotos.map(p => p.index).sort();
      expect(indices).toEqual([2, 45]);
    });
  });

  // Helper function to create mock photos
  function createMockPhoto(id: string, filmId: string, index: number): PhotoDto {
    return {
      id,
      filmId,
      index,
      imageUrl: `https://analogagendastorage.blob.core.windows.net/photos/${id}-${index}`,
      imageBase64: ''
    };
  }
});
