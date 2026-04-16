import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicCollectionService } from '../../services/implementations/public-collection.service';
import { environment } from '../../../environments/environment';

describe('PublicCollectionService', () => {
  let service: PublicCollectionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PublicCollectionService],
    });
    service = TestBed.inject(PublicCollectionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getPage GETs collection page with credentials', (done) => {
    const id = 'abc12345';
    const url = `${environment.apiUrl}/api/public/collections/${id}`;

    service.getPage(id).subscribe((dto) => {
      expect(dto.requiresPassword).toBeFalse();
      expect(dto.name).toBe('Test');
      done();
    });

    const req = httpMock.expectOne(url);
    expect(req.request.withCredentials).toBeTrue();
    req.flush({
      requiresPassword: false,
      name: 'Test',
      photos: [],
      comments: [],
    });
  });

  it('postComment POSTs with credentials', (done) => {
    const id = 'abc12345';
    const url = `${environment.apiUrl}/api/public/collections/${id}/comments`;

    service.postComment(id, { authorName: 'A', body: 'B' }).subscribe((c) => {
      expect(c.body).toBe('B');
      done();
    });

    const req = httpMock.expectOne(url);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.body).toEqual({ authorName: 'A', body: 'B' });
    req.flush({ id: 'x', authorName: 'A', body: 'B', createdAt: new Date().toISOString() });
  });

  it('verify POSTs password with credentials', (done) => {
    const id = 'abc12345';
    const url = `${environment.apiUrl}/api/public/collections/${id}/verify`;

    service.verify(id, 'secret').subscribe(() => done());

    const req = httpMock.expectOne(url);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.body).toEqual({ password: 'secret' });
    req.flush({ ok: true });
  });

  describe('downloadAll', () => {
    it('should GET blob without small query by default', () => {
      const id = 'col1';
      const blob = new Blob(['PK'], { type: 'application/zip' });
      const url = `${environment.apiUrl}/api/public/collections/${id}/download`;

      service.downloadAll(id, false).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === url &&
          r.responseType === 'blob' &&
          r.withCredentials === true
      );
      req.flush(blob);
    });

    it('should GET blob with small=true when requested', () => {
      const id = 'col1';
      const blob = new Blob(['PK'], { type: 'application/zip' });
      const url = `${environment.apiUrl}/api/public/collections/${id}/download?small=true`;

      service.downloadAll(id, true).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === url &&
          r.responseType === 'blob' &&
          r.withCredentials === true
      );
      req.flush(blob);
    });
  });

  describe('downloadSelected', () => {
    it('should POST ids and small with blob response and credentials', () => {
      const id = 'col1';
      const ids = ['p1', 'p2'];
      const blob = new Blob(['PK'], { type: 'application/zip' });
      const url = `${environment.apiUrl}/api/public/collections/${id}/download/selected`;

      service.downloadSelected(id, ids, false).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'POST' &&
          r.url === url &&
          r.responseType === 'blob' &&
          r.withCredentials === true
      );
      expect(req.request.body).toEqual({ ids, small: false });
      req.flush(blob);
    });
  });

  describe('downloadPhoto', () => {
    it('should GET blob for single photo download', () => {
      const id = 'col1';
      const photoId = 'ph1';
      const blob = new Blob([new Uint8Array([1, 2])], { type: 'image/jpeg' });
      const url = `${environment.apiUrl}/api/public/collections/${id}/photos/${photoId}/download`;

      service.downloadPhoto(id, photoId).subscribe((res) => {
        expect(res).toEqual(blob);
      });

      const req = httpMock.expectOne(
        (r) =>
          r.method === 'GET' &&
          r.url === url &&
          r.responseType === 'blob' &&
          r.withCredentials === true
      );
      req.flush(blob);
    });
  });
});

