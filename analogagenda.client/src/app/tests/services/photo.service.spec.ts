import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PhotoService } from '../../services/implementations/photo.service';
import { PhotoDto, PhotoBulkUploadDto, PhotoCreateDto } from '../../DTOs';
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
        filmRowId: 'test-film-id',
        imageBase64: 'data:image/jpeg;base64,validbase64data'
      };
      
      const mockResponse: PhotoDto = createMockPhoto('photo1', 'test-film-id', 1);

      // Act
      service.createPhoto(createDto).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createDto);
      req.flush(mockResponse);
    });

    it('should handle error when creating photo', () => {
      // Arrange
      const createDto: PhotoCreateDto = {
        filmRowId: 'invalid-film-id',
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
      const req = httpMock.expectOne(`${baseUrl}/`);
      req.flush('Film not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('uploadPhotos', () => {
    it('should upload multiple photos', () => {
      // Arrange
      const uploadDto: PhotoBulkUploadDto = {
        filmRowId: 'test-film-id',
        photos: [
          { imageBase64: 'data:image/jpeg;base64,photo1data' },
          { imageBase64: 'data:image/jpeg;base64,photo2data' }
        ]
      };
      
      const mockResponse: PhotoDto[] = [
        createMockPhoto('photo1', 'test-film-id', 1),
        createMockPhoto('photo2', 'test-film-id', 2)
      ];

      // Act
      service.uploadPhotos(uploadDto).subscribe((response: any) => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.length).toBe(2);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/bulk`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(uploadDto);
      req.flush(mockResponse);
    });

    it('should handle error when uploading photos', () => {
      // Arrange
      const uploadDto: PhotoBulkUploadDto = {
        filmRowId: 'test-film-id',
        photos: []
      };

      // Act
      service.uploadPhotos(uploadDto).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(400);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/bulk`);
      req.flush('No photos provided', { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('getPhotosByFilmId', () => {
    it('should get photos by film ID', () => {
      // Arrange
      const filmRowId = 'test-film-id';
      const mockResponse: PhotoDto[] = [
        createMockPhoto('photo1', filmRowId, 1),
        createMockPhoto('photo2', filmRowId, 2),
        createMockPhoto('photo3', filmRowId, 3)
      ];

      // Act
      service.getPhotosByFilmId(filmRowId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.length).toBe(3);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/film/${filmRowId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should return empty array when no photos found', () => {
      // Arrange
      const filmRowId = 'test-film-id';
      const mockResponse: PhotoDto[] = [];

      // Act
      service.getPhotosByFilmId(filmRowId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.length).toBe(0);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/film/${filmRowId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('downloadPhoto', () => {
    it('should download a single photo', () => {
      // Arrange
      const rowKey = 'test-photo-key';
      const mockBlob = new Blob(['fake-image-data'], { type: 'image/jpeg' });

      // Act
      service.downloadPhoto(rowKey).subscribe(response => {
        // Assert
        expect(response).toEqual(mockBlob);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download/${rowKey}`);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(mockBlob);
    });

    it('should handle error when downloading photo', () => {
      // Arrange
      const rowKey = 'invalid-photo-key';

      // Act
      service.downloadPhoto(rowKey).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download/${rowKey}`);
      req.flush(null, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('downloadAllPhotos', () => {
    it('should download all photos as zip', () => {
      // Arrange
      const filmRowId = 'test-film-id';
      const mockZipBlob = new Blob(['fake-zip-data'], { type: 'application/zip' });

      // Act
      service.downloadAllPhotos(filmRowId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockZipBlob);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download-all/${filmRowId}`);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(mockZipBlob);
    });

    it('should handle error when no photos to download', () => {
      // Arrange
      const filmRowId = 'empty-film-id';

      // Act
      service.downloadAllPhotos(filmRowId).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/download-all/${filmRowId}`);
      req.flush(null, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('deletePhoto', () => {
    it('should delete a photo', () => {
      // Arrange
      const rowKey = 'test-photo-key';

      // Act
      service.deletePhoto(rowKey).subscribe(response => {
        // Assert
        expect(response).toBeDefined();
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/${rowKey}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('should handle error when deleting non-existent photo', () => {
      // Arrange
      const rowKey = 'invalid-photo-key';

      // Act
      service.deletePhoto(rowKey).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/${rowKey}`);
      req.flush('Photo not found', { status: 404, statusText: 'Not Found' });
    });
  });

  // Helper function to create mock photos
  function createMockPhoto(rowKey: string, filmRowId: string, index: number): PhotoDto {
    return {
      rowKey,
      filmRowId,
      index,
      imageUrl: `test-image-url-${index}`,
      imageBase64: ''
    };
  }
});
