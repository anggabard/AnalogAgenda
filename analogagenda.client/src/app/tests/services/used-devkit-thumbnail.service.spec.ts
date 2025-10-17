import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UsedDevKitThumbnailService } from '../../services/implementations/used-devkit-thumbnail.service';
import { UsedDevKitThumbnailDto } from '../../DTOs';

describe('UsedDevKitThumbnailService', () => {
  let service: UsedDevKitThumbnailService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UsedDevKitThumbnailService]
    });
    service = TestBed.inject(UsedDevKitThumbnailService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should search thumbnails by devkit name', () => {
    const mockThumbnails: UsedDevKitThumbnailDto[] = [
      {
        rowKey: 'thumb1',
        devKitName: 'Bellini E6',
        imageId: 'img1',
        imageUrl: 'url1',
        imageBase64: ''
      },
      {
        rowKey: 'thumb2',
        devKitName: 'Bellini C41',
        imageId: 'img2',
        imageUrl: 'url2',
        imageBase64: ''
      }
    ];

    service.searchByDevKitName('Bellini').subscribe(thumbnails => {
      expect(thumbnails).toEqual(mockThumbnails);
    });

    const req = httpMock.expectOne('https://localhost:7125/api/UsedDevKitThumbnail/search?devKitName=Bellini');
    expect(req.request.method).toBe('GET');
    req.flush(mockThumbnails);
  });

  it('should search all thumbnails when no devkit name provided', () => {
    const mockThumbnails: UsedDevKitThumbnailDto[] = [
      {
        rowKey: 'thumb1',
        devKitName: 'Bellini E6',
        imageId: 'img1',
        imageUrl: 'url1',
        imageBase64: ''
      }
    ];

    service.searchByDevKitName('').subscribe(thumbnails => {
      expect(thumbnails).toEqual(mockThumbnails);
    });

    const req = httpMock.expectOne('https://localhost:7125/api/UsedDevKitThumbnail/search?devKitName=');
    expect(req.request.method).toBe('GET');
    req.flush(mockThumbnails);
  });

  it('should upload thumbnail', () => {
    const mockUploadedThumbnail: UsedDevKitThumbnailDto = {
      rowKey: 'thumb1',
      devKitName: 'Test DevKit E6',
      imageId: 'img1',
      imageUrl: 'url1',
      imageBase64: ''
    };

    const uploadDto: UsedDevKitThumbnailDto = {
      rowKey: '',
      devKitName: 'Test DevKit E6',
      imageId: '',
      imageUrl: '',
      imageBase64: 'data:image/jpeg;base64,testdata'
    };

    service.uploadThumbnail('Test DevKit E6', 'data:image/jpeg;base64,testdata').subscribe(thumbnail => {
      expect(thumbnail).toEqual(mockUploadedThumbnail);
    });

    const req = httpMock.expectOne('https://localhost:7125/api/UsedDevKitThumbnail');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(uploadDto);
    req.flush(mockUploadedThumbnail);
  });

  it('should handle search error', () => {
    service.searchByDevKitName('Bellini').subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('https://localhost:7125/api/UsedDevKitThumbnail/search?devKitName=Bellini');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
  });

  it('should handle upload error', () => {
    service.uploadThumbnail('Test DevKit', 'data:image/jpeg;base64,testdata').subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.status).toBe(400);
      }
    });

    const req = httpMock.expectOne('https://localhost:7125/api/UsedDevKitThumbnail');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
  });
});
