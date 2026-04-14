import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CollectionService } from '../../services/implementations/collection.service';
import { CollectionDto, CollectionOptionDto, PagedResponseDto } from '../../DTOs';

describe('CollectionService', () => {
  let service: CollectionService;
  let httpMock: HttpTestingController;

  const baseUrl = 'https://localhost:7125/api/Collection';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CollectionService],
    });
    service = TestBed.inject(CollectionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMinePaged', () => {
    it('should GET with page and pageSize query (paged mine list contract)', () => {
      const page = 2;
      const pageSize = 15;
      const mock: PagedResponseDto<CollectionDto> = {
        data: [],
        totalCount: 0,
        pageSize,
        currentPage: page,
        hasNextPage: false,
        hasPreviousPage: true,
        totalPages: 0,
      };

      service.getMinePaged(page, pageSize).subscribe((res) => {
        expect(res).toEqual(mock);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === `${baseUrl}/?page=${page}&pageSize=${pageSize}` &&
          r.withCredentials === true
      );
      req.flush(mock);
    });
  });

  describe('getOpenOptions', () => {
    it('should GET open collection options for assignment UI', () => {
      const mock: CollectionOptionDto[] = [
        { id: 'c1', name: 'Summer', imageUrl: '' },
      ];

      service.getOpenOptions().subscribe((res) => {
        expect(res).toEqual(mock);
      });

      const req = httpMock.expectOne(
        (r) => r.method === 'GET' && r.url === `${baseUrl}/open` && r.withCredentials === true
      );
      req.flush(mock);
    });
  });

  describe('appendPhotos', () => {
    it('should POST ids to collection photos append endpoint', () => {
      const collectionId = 'coll-abc';
      const photoIds = ['p1', 'p2'];
      const mockCollection: CollectionDto = {
        id: collectionId,
        name: 'Test',
        fromDate: null,
        toDate: null,
        location: '',
        imageId: '',
        isOpen: true,
        owner: 'user',
        photoIds,
        photoCount: 2,
        imageUrl: '',
      };

      service.appendPhotos(collectionId, photoIds).subscribe((res) => {
        expect(res).toEqual(mockCollection);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'POST' &&
          r.url === `${baseUrl}/${collectionId}/photos` &&
          r.withCredentials === true
      );
      expect(req.request.body).toEqual({ ids: photoIds });
      req.flush(mockCollection);
    });
  });

  describe('downloadArchive', () => {
    it('should GET blob without small query by default', () => {
      const id = 'coll-zip';
      const blob = new Blob(['PK'], { type: 'application/zip' });

      service.downloadArchive(id).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === `${baseUrl}/${id}/download` &&
          r.responseType === 'blob' &&
          r.withCredentials === true
      );
      req.flush(blob);
    });

    it('should GET blob with small=true when requested', () => {
      const id = 'coll-zip';
      const blob = new Blob(['PK'], { type: 'application/zip' });

      service.downloadArchive(id, true).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === `${baseUrl}/${id}/download?small=true` &&
          r.responseType === 'blob'
      );
      req.flush(blob);
    });
  });
});
