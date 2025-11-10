import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PhotoService } from '../../services/implementations/photo.service';
import { PhotoDto, PhotoCreateDto } from '../../DTOs';
import { TestConfig } from '../test.config';

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
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('createPhoto', () => {
    it('should create a single photo', () => {
      // Arrange
      const createDto: PhotoCreateDto = {
        filmId: 'test-film-id',
        imageBase64: 'data:image/jpeg;base64,validbase64data'
      };
      
      const mockResponse: PhotoDto = createMockPhoto('photo1', 'test-film-id', 1);

      // Act
      service.createPhoto(createDto).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createDto);
      req.flush(mockResponse);
    });

    it('should handle error when creating photo', () => {
      // Arrange
      const createDto: PhotoCreateDto = {
        filmId: 'invalid-film-id',
        imageBase64: 'data:image/jpeg;base64,validbase64data'
      };

      // Act
      service.createPhoto(createDto).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
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
    it('should return correct preview URL for a photo', () => {
      // Arrange
      const photoId = 'test-photo-id-123';
      const expectedUrl = `${baseUrl}/preview/${photoId}`;

      // Act
      const result = service.getPreviewUrl(photoId);

      // Assert
      expect(result).toBe(expectedUrl);
    });

    it('should handle photo IDs with special characters', () => {
      // Arrange
      const photoId = 'photo-id-with-dashes-123';
      const expectedUrl = `${baseUrl}/preview/${photoId}`;

      // Act
      const result = service.getPreviewUrl(photoId);

      // Assert
      expect(result).toBe(expectedUrl);
    });

    it('should not make HTTP call - just return URL string', () => {
      // Arrange
      const photoId = 'test-photo-id';

      // Act
      const result = service.getPreviewUrl(photoId);

      // Assert
      expect(typeof result).toBe('string');
      expect(result).toContain('/preview/');
      expect(result).toContain(photoId);
      
      // Verify no HTTP call was made
      httpMock.expectNone(`${baseUrl}/preview/${photoId}`);
    });
  });

  describe('uploadMultiplePhotos', () => {
    it('should upload multiple photos in parallel with numeric filenames', (done) => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '5.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '10.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [];
      
      let progressCallCount = 0;
      const onProgress = jasmine.createSpy('onProgress').and.callFake((current: number, total: number) => {
        progressCallCount++;
        expect(total).toBe(2);
        expect(current).toBeGreaterThan(0);
        expect(current).toBeLessThanOrEqual(2);
      });

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 5);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 10);

      // Act
      service.uploadMultiplePhotos(filmId, files, existingPhotos, onProgress).then(() => {
        // Assert after upload completes
        expect(progressCallCount).toBe(2);
        expect(onProgress).toHaveBeenCalledTimes(2);
        done();
      }).catch(err => done.fail(err));

      // Wait for requests to be made, then respond
      setTimeout(() => {
        const reqs = httpMock.match(req => req.url === baseUrl && req.method === 'POST');
        expect(reqs.length).toBe(2);
        
        // Check first request (index 5)
        expect(reqs[0].request.body.index).toBe(5);
        expect(reqs[0].request.body.filmId).toBe(filmId);
        reqs[0].flush(mockResponse1);
        
        // Check second request (index 10)
        expect(reqs[1].request.body.index).toBe(10);
        expect(reqs[1].request.body.filmId).toBe(filmId);
        reqs[1].flush(mockResponse2);
      }, 100);
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

      // Act
      service.uploadMultiplePhotos(filmId, files, existingPhotos).then(() => {
        done();
      }).catch(err => done.fail(err));

      // Wait for requests to be made, then respond
      setTimeout(() => {
        const reqs = httpMock.match(req => req.url === baseUrl && req.method === 'POST');
        expect(reqs.length).toBe(2);
        
        // Both should use auto-assigned indices starting from 9 (max existing + 1)
        expect(reqs[0].request.body.index).toBe(9);
        expect(reqs[1].request.body.index).toBe(10);
        
        reqs[0].flush(mockResponse1);
        reqs[1].flush(mockResponse2);
      }, 100);
    });

    it('should sort files by index before uploading', (done) => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '45.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '2.jpg', { type: 'image/jpeg' });
      const file3 = new File(['test3'], '10.jpg', { type: 'image/jpeg' });
      const files = [file1, file2, file3]; // Unsorted
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 2);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 10);
      const mockResponse3: PhotoDto = createMockPhoto('photo3', filmId, 45);

      // Act
      service.uploadMultiplePhotos(filmId, files, existingPhotos).then(() => {
        done();
      }).catch(err => done.fail(err));

      // Wait for requests to be made, then respond
      setTimeout(() => {
        // Assert - files should be uploaded in sorted order (2, 10, 45)
        const reqs = httpMock.match(req => req.url === baseUrl && req.method === 'POST');
        expect(reqs.length).toBe(3);
        
        expect(reqs[0].request.body.index).toBe(2);
        expect(reqs[1].request.body.index).toBe(10);
        expect(reqs[2].request.body.index).toBe(45);
        
        reqs[0].flush(mockResponse1);
        reqs[1].flush(mockResponse2);
        reqs[2].flush(mockResponse3);
      }, 100);
    });

    it('should handle files with leading zeros in filenames', (done) => {
      // Arrange
      const filmId = 'test-film-id';
      const file1 = new File(['test1'], '002.jpg', { type: 'image/jpeg' });
      const file2 = new File(['test2'], '045.jpg', { type: 'image/jpeg' });
      const files = [file1, file2];
      const existingPhotos: PhotoDto[] = [];

      const mockResponse1: PhotoDto = createMockPhoto('photo1', filmId, 2);
      const mockResponse2: PhotoDto = createMockPhoto('photo2', filmId, 45);

      // Act
      service.uploadMultiplePhotos(filmId, files, existingPhotos).then(() => {
        done();
      }).catch(err => done.fail(err));

      // Wait for requests to be made, then respond
      setTimeout(() => {
        const reqs = httpMock.match(req => req.url === baseUrl && req.method === 'POST');
        expect(reqs.length).toBe(2);
        
        expect(reqs[0].request.body.index).toBe(2); // 002 -> 2
        expect(reqs[1].request.body.index).toBe(45); // 045 -> 45
        
        reqs[0].flush(mockResponse1);
        reqs[1].flush(mockResponse2);
      }, 100);
    });
  });

  // Helper function to create mock photos
  function createMockPhoto(id: string, filmId: string, index: number): PhotoDto {
    return {
      id,
      filmId,
      index,
      imageUrl: `test-image-url-${index}`,
      imageBase64: ''
    };
  }
});
