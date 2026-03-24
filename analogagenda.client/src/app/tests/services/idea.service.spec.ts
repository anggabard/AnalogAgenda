import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { IdeaService } from '../../services/implementations/idea.service';
import { IdeaDto } from '../../DTOs';
import { TestConfig } from '../test.config';

describe('IdeaService', () => {
  let service: IdeaService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://localhost:7125/api/Idea';

  beforeEach(() => {
    TestConfig.configureTestBed({
      providers: [IdeaService]
    });
    service = TestBed.inject(IdeaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call getAll with correct URL', () => {
    const mockIdeas: IdeaDto[] = [
      { id: 'abc', title: 'Idea 1', description: 'Desc 1' }
    ];

    service.getAll().subscribe(response => {
      expect(response).toEqual(mockIdeas);
    });

    const req = httpMock.expectOne(`${baseUrl}/?page=0`);
    expect(req.request.method).toBe('GET');
    req.flush(mockIdeas);
  });

  it('should call getById with correct id', () => {
    const mockIdea: IdeaDto = { id: 'abc', title: 'Idea', description: 'Desc' };

    service.getById('abc').subscribe(response => {
      expect(response).toEqual(mockIdea);
    });

    const req = httpMock.expectOne(`${baseUrl}/abc`);
    expect(req.request.method).toBe('GET');
    req.flush(mockIdea);
  });

  it('should call add with correct body', () => {
    const newIdea: IdeaDto = { id: '', title: 'New Idea', description: 'New desc' };
    const createdIdea: IdeaDto = { id: 'xyz', title: 'New Idea', description: 'New desc' };

    service.add(newIdea).subscribe(response => {
      expect(response).toEqual(createdIdea);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newIdea);
    req.flush(createdIdea);
  });

  it('should call update with correct id and body', () => {
    const idea: IdeaDto = { id: 'abc', title: 'Updated', description: 'Updated desc' };

    service.update('abc', idea).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/abc`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(idea);
    req.flush(null);
  });

  it('should call deleteById with correct id', () => {
    service.deleteById('abc').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/abc`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should call getPhotosForIdea with correct URL', () => {
    const mockPhotos = [{ id: 'p1', filmId: 'f1', index: 1, imageUrl: '', imageBase64: '' }];
    service.getPhotosForIdea('idea1').subscribe((res) => expect(res).toEqual(mockPhotos));

    const req = httpMock.expectOne(`${baseUrl}/idea1/photos`);
    expect(req.request.method).toBe('GET');
    req.flush(mockPhotos);
  });

  it('should call addPhotosToIdea with photo ids in body', () => {
    const body = { ids: ['p1', 'p2'] };
    const mockPhotos = [{ id: 'p1', filmId: 'f1', index: 1, imageUrl: '', imageBase64: '' }];
    service.addPhotosToIdea('idea1', body.ids).subscribe((res) => expect(res).toEqual(mockPhotos));

    const req = httpMock.expectOne(`${baseUrl}/idea1/photos`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(mockPhotos);
  });

  it('should call removePhotoFromIdea with correct URL', () => {
    service.removePhotoFromIdea('idea1', 'p9').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/idea1/photos/p9`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
