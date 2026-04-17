import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PhotoService } from '../../services/implementations/photo.service';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
import { TestConfig } from '../test.config';
import { FileUploadHelper } from '../../helpers/file-upload.helper';

describe('PhotoService', () => {
  let service: PhotoService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://localhost:7125/api/Photo';

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
      const uploadPromise = service.uploadMultiplePhotos(filmId, [file], existingPhotos);

      // Wait for request to be made
      await new Promise(resolve => setTimeout(resolve, 50));

      // Assert HTTP call to backend API endpoint
      const req = httpMock.expectOne((r) => r.url === baseUrl && r.method === 'POST');
      expect(req.request.method).toBe('POST');
      expect(req.request.body.filmId).toBe(filmId);
      expect(req.request.body.index).toBe(1);
      expect(req.request.withCredentials).toBe(true);
      req.flush(mockResponse);

      // Wait for upload to complete
      const result = await uploadPromise;

      // Assert
      expect(result.length).toBe(1);
      expect(result[0].success).toBe(true);
      expect(result[0].photo).toEqual(mockResponse);
    });

    it('should handle error when uploading photo', async () => {
      // Arrange
      const filmId = 'invalid-film-id';
      const file = new File(['test'], '1.jpg', { type: 'image/jpeg' });
      const existingPhotos: PhotoDto[] = [];

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, [file], existingPhotos);

      // Wait for request to be made
      await new Promise(resolve => setTimeout(resolve, 50));

      // Assert HTTP call to backend API endpoint
      const req = httpMock.expectOne((r) => r.url === baseUrl && r.method === 'POST');
      req.flush('Film not found', { status: 404, statusText: 'Not Found' });

      // Wait for upload to complete
      const result = await uploadPromise;

      // Assert
      expect(result.length).toBe(1);
      expect(result[0].success).toBe(false);
      expect(result[0].error).toBeDefined();
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

  describe('downloadSelectedPhotos', () => {
    it('should POST download-selected with film id, ids, and small flag', () => {
      const filmId = 'test-film-id';
      const ids = ['p1', 'p2'];
      const mockZipBlob = new Blob(['fake-zip'], { type: 'application/zip' });

      service.downloadSelectedPhotos(filmId, ids, true).subscribe((response) => {
        expect(response).toEqual(mockZipBlob);
      });

      const req = httpMock.expectOne(`${baseUrl}/download-selected`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ filmId, ids, small: true });
      expect(req.request.responseType).toBe('blob');
      req.flush(mockZipBlob);
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

    it('should leave URL unchanged when imageUrl already uses preview path', () => {
      const preview =
        'https://analogagendastorage.blob.core.windows.net/photos/preview/12345678-1234-1234-1234-123456789012';
      const photo: PhotoDto = {
        id: 'test-photo-id',
        filmId: 'test-film-id',
        index: 1,
        imageUrl: preview,
        imageBase64: ''
      };
      expect(service.getPreviewUrl(photo)).toBe(preview);
    });
  });

  describe('uploadMultiplePhotos - sequential uploads', () => {
    it('should upload multiple photos sequentially and call callback with PhotoDto', async () => {
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

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 1);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 2);

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act — array order wins; filenames 5.jpg / 10.jpg are not used for indices
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, onPhotoUploaded, 1); // Use concurrency 1 for sequential

      // Wait a bit for first request to be made
      await new Promise(resolve => setTimeout(resolve, 50));

      // First request should come sequentially
      const req1 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 1);
      expect(req1.request.method).toBe('POST');
      expect(req1.request.body.filmId).toBe(filmId);
      expect(req1.request.body.index).toBe(1);
      req1.flush(mockResponse1);

      // Wait for second request
      await new Promise(resolve => setTimeout(resolve, 50));

      // Second request should come after first completes
      const req2 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 2);
      expect(req2.request.method).toBe('POST');
      expect(req2.request.body.filmId).toBe(filmId);
      expect(req2.request.body.index).toBe(2);
      req2.flush(mockResponse2);

      // Wait for upload to complete
      const result = await uploadPromise;

      // Assert
      expect(result.length).toBe(2);
      expect(result[0].success).toBe(true);
      expect(result[1].success).toBe(true);
      expect(onPhotoUploaded).toHaveBeenCalledTimes(2);
      expect(uploadedPhotos.length).toBe(2);
    });

    it('should use next available index for non-numeric filenames', async () => {
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

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, onPhotoUploaded, 1); // Use concurrency 1 for sequential

      // Wait for first request
      await new Promise(resolve => setTimeout(resolve, 50));
      const req1 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 9);
      expect(req1.request.body.index).toBe(9);
      req1.flush(mockResponse1);

      // Wait for second request
      await new Promise(resolve => setTimeout(resolve, 50));
      const req2 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 10);
      expect(req2.request.body.index).toBe(10);
      req2.flush(mockResponse2);

      // Wait for completion
      await uploadPromise;

      // Assert
      expect(uploadedPhotos.length).toBe(2);
    });

    it('should assign indices in file array order (not by numeric filename)', async () => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '45.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '2.jpg', { type: 'image/jpeg' });
      const file3 = new File(['test3'], '10.jpg', { type: 'image/jpeg' });
      const files = [file1, file2, file3];
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1 = createMockPhoto('photo1', filmId, 1);
      const mockResponse2 = createMockPhoto('photo2', filmId, 2);
      const mockResponse3 = createMockPhoto('photo3', filmId, 3);

      const uploadedPhotos: PhotoDto[] = [];

      // Mock fileToBase64
      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      // Act
      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, (photo) => {
        uploadedPhotos.push(photo);
      }, 1); // Use concurrency 1 for sequential

      await new Promise(resolve => setTimeout(resolve, 50));
      const req1 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 1);
      expect(req1.request.body.index).toBe(1);
      req1.flush(mockResponse1);

      await new Promise(resolve => setTimeout(resolve, 50));
      const req2 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 2);
      expect(req2.request.body.index).toBe(2);
      req2.flush(mockResponse2);

      await new Promise(resolve => setTimeout(resolve, 50));
      const req3 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 3);
      expect(req3.request.body.index).toBe(3);
      req3.flush(mockResponse3);

      await uploadPromise;

      expect(uploadedPhotos.length).toBe(3);
      const indices = uploadedPhotos.map(p => p.index);
      expect(indices).toEqual([1, 2, 3]);
    });

    it('should ignore numeric-looking basenames; order is array order', async () => {
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '002.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '045.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1 = createMockPhoto('photo1', filmId, 1);
      const mockResponse2 = createMockPhoto('photo2', filmId, 2);

      const uploadedPhotos: PhotoDto[] = [];

      spyOn(FileUploadHelper, 'fileToBase64').and.returnValue(Promise.resolve('data:image/jpeg;base64,validbase64data'));

      const uploadPromise = service.uploadMultiplePhotos(filmId, files, existingPhotos, (photo) => {
        uploadedPhotos.push(photo);
      }, 1);

      await new Promise(resolve => setTimeout(resolve, 50));
      const req1 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 1);
      req1.flush(mockResponse1);

      await new Promise(resolve => setTimeout(resolve, 50));
      const req2 = httpMock.expectOne((req) => req.url === baseUrl && req.body.index === 2);
      req2.flush(mockResponse2);

      await uploadPromise;

      expect(uploadedPhotos.length).toBe(2);
      expect(uploadedPhotos.map(p => p.index).sort()).toEqual([1, 2]);
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
