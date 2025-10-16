import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UsedFilmThumbnailService } from '../../services/implementations/used-film-thumbnail.service';
import { UsedFilmThumbnailDto } from '../../DTOs';

describe('UsedFilmThumbnailService', () => {
  let service: UsedFilmThumbnailService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UsedFilmThumbnailService]
    });
    service = TestBed.inject(UsedFilmThumbnailService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should search thumbnails by film name', () => {
    const mockThumbnails: UsedFilmThumbnailDto[] = [
      {
        rowKey: 'thumb1',
        filmName: 'Kodak Portra 400',
        imageId: 'img1',
        imageUrl: 'url1',
        imageBase64: ''
      },
      {
        rowKey: 'thumb2',
        filmName: 'Fuji Superia 200',
        imageId: 'img2',
        imageUrl: 'url2',
        imageBase64: ''
      }
    ];

    service.searchByFilmName('Kodak').subscribe(thumbnails => {
      expect(thumbnails).toEqual(mockThumbnails);
    });

    const req = httpMock.expectOne('/api/UsedFilmThumbnail/search?filmName=Kodak');
    expect(req.request.method).toBe('GET');
    req.flush(mockThumbnails);
  });

  it('should search all thumbnails when no film name provided', () => {
    const mockThumbnails: UsedFilmThumbnailDto[] = [
      {
        rowKey: 'thumb1',
        filmName: 'Kodak Portra 400',
        imageId: 'img1',
        imageUrl: 'url1',
        imageBase64: ''
      }
    ];

    service.searchByFilmName('').subscribe(thumbnails => {
      expect(thumbnails).toEqual(mockThumbnails);
    });

    const req = httpMock.expectOne('/api/UsedFilmThumbnail/search?filmName=');
    expect(req.request.method).toBe('GET');
    req.flush(mockThumbnails);
  });

  it('should upload thumbnail', () => {
    const mockUploadedThumbnail: UsedFilmThumbnailDto = {
      rowKey: 'thumb1',
      filmName: 'Test Film 400',
      imageId: 'img1',
      imageUrl: 'url1',
      imageBase64: ''
    };

    const uploadDto: UsedFilmThumbnailDto = {
      rowKey: '',
      filmName: 'Test Film 400',
      imageId: '',
      imageUrl: '',
      imageBase64: 'data:image/jpeg;base64,testdata'
    };

    service.uploadThumbnail('Test Film 400', 'data:image/jpeg;base64,testdata').subscribe(thumbnail => {
      expect(thumbnail).toEqual(mockUploadedThumbnail);
    });

    const req = httpMock.expectOne('/api/UsedFilmThumbnail');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(uploadDto);
    req.flush(mockUploadedThumbnail);
  });

  it('should handle search error', () => {
    service.searchByFilmName('Kodak').subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('/api/UsedFilmThumbnail/search?filmName=Kodak');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
  });

  it('should handle upload error', () => {
    service.uploadThumbnail('Test Film', 'data:image/jpeg;base64,testdata').subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.status).toBe(400);
      }
    });

    const req = httpMock.expectOne('/api/UsedFilmThumbnail');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
  });
});
