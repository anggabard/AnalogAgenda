import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DevKitService } from '../../services/implementations/dev-kit.service';
import { TestConfig } from '../test.config';

describe('DevKitService', () => {
  let service: DevKitService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://localhost:7125/api/DevKit';

  beforeEach(() => {
    TestConfig.configureTestBed({
      providers: [DevKitService],
    });
    service = TestBed.inject(DevKitService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getSessionAssignment should GET assignment/sessions with showAll query', () => {
    const rows = [{ id: 's1', sessionDate: '2024-01-01', location: 'L', participantsPreview: '', isSelected: true }];
    service.getSessionAssignment('kit1', true).subscribe((res) => expect(res).toEqual(rows));

    const req = httpMock.expectOne(`${baseUrl}/kit1/assignment/sessions?showAll=true`);
    expect(req.request.method).toBe('GET');
    req.flush(rows);
  });

  it('getSessionAssignment should use showAll=false when not requested', () => {
    service.getSessionAssignment('kit1', false).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/kit1/assignment/sessions?showAll=false`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('putSessionAssignment should PUT ids body', () => {
    const body = { ids: ['s1', 's2'] };
    service.putSessionAssignment('kit1', body.ids).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/kit1/assignment/sessions`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush([]);
  });

  it('getFilmAssignment should GET assignment/films with showAll query', () => {
    const rows = [
      {
        id: 'f1',
        name: '',
        brand: 'B',
        iso: '400',
        type: 'ColorNegative',
        formattedExposureDate: '',
        isSelected: false,
      },
    ];
    service.getFilmAssignment('kit1', true).subscribe((res) => expect(res).toEqual(rows));

    const req = httpMock.expectOne(`${baseUrl}/kit1/assignment/films?showAll=true`);
    expect(req.request.method).toBe('GET');
    req.flush(rows);
  });

  it('putFilmAssignment should PUT ids body', () => {
    const body = { ids: ['f1'] };
    service.putFilmAssignment('kit1', body.ids).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/kit1/assignment/films`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush([]);
  });
});
