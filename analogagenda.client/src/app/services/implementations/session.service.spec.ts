import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SessionService } from '../../services/implementations/session.service';
import { SessionDto } from '../../DTOs';

describe('SessionService', () => {
  let service: SessionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SessionService]
    });
    service = TestBed.inject(SessionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create a new session', () => {
    // Arrange
    const mockSession: SessionDto = {
      rowKey: '',
      sessionDate: '2025-10-02',
      location: 'Test Location',
      participants: '["Angel", "Tudor"]',
      description: 'Test session',
      usedSubstances: '["devkit1"]',
      developedFilms: '["film1"]',
      imageUrl: '',
      imageBase64: ''
    };

    const expectedResponse: SessionDto = {
      ...mockSession,
      rowKey: 'generated-row-key'
    };

    // Act
    service.add(mockSession).subscribe(response => {
      // Assert
      expect(response).toEqual(expectedResponse);
      expect(response.rowKey).toBe('generated-row-key');
    });

    // Assert HTTP request
    const req = httpMock.expectOne('api/Session');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(mockSession);
    req.flush(expectedResponse);
  });

  it('should get session by id', () => {
    // Arrange
    const sessionId = 'test-session-id';
    const mockSession: SessionDto = {
      rowKey: sessionId,
      sessionDate: '2025-10-02',
      location: 'Test Location',
      participants: '["Angel"]',
      description: 'Test session',
      usedSubstances: '["devkit1"]',
      developedFilms: '["film1"]',
      imageUrl: '',
      imageBase64: ''
    };

    // Act
    service.getById(sessionId).subscribe(response => {
      // Assert
      expect(response).toEqual(mockSession);
    });

    // Assert HTTP request
    const req = httpMock.expectOne(`api/Session/${sessionId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockSession);
  });

  it('should update session', () => {
    // Arrange
    const sessionId = 'test-session-id';
    const updatedSession: SessionDto = {
      rowKey: sessionId,
      sessionDate: '2025-10-02',
      location: 'Updated Location',
      participants: '["Angel", "Tudor"]',
      description: 'Updated description',
      usedSubstances: '["devkit1", "devkit2"]',
      developedFilms: '["film1", "film2"]',
      imageUrl: '',
      imageBase64: ''
    };

    // Act
    service.update(sessionId, updatedSession).subscribe(response => {
      // Assert
      expect(response).toBeTruthy();
    });

    // Assert HTTP request
    const req = httpMock.expectOne(`api/Session/${sessionId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updatedSession);
    req.flush({});
  });

  it('should delete session', () => {
    // Arrange
    const sessionId = 'test-session-id';

    // Act
    service.deleteById(sessionId).subscribe(response => {
      // Assert
      expect(response).toBeTruthy();
    });

    // Assert HTTP request
    const req = httpMock.expectOne(`api/Session/${sessionId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  it('should get paged sessions', () => {
    // Arrange
    const page = 1;
    const pageSize = 5;
    const mockResponse = {
      data: [
        {
          rowKey: 'session1',
          sessionDate: '2025-10-02',
          location: 'Location 1',
          participants: '["Angel"]',
          description: '',
          usedSubstances: '[]',
          developedFilms: '[]',
          imageUrl: '',
          imageBase64: ''
        }
      ],
      hasNextPage: false,
      totalCount: 1
    };

    // Act
    service.getPaged(page, pageSize).subscribe(response => {
      // Assert
      expect(response.data.length).toBe(1);
      expect(response.hasNextPage).toBe(false);
      expect(response.totalCount).toBe(1);
    });

    // Assert HTTP request
    const req = httpMock.expectOne(`api/Session?page=${page}&pageSize=${pageSize}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
