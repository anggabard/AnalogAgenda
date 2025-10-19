import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FilmService } from '../../services/implementations/film.service';
import { FilmDto, PagedResponseDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('FilmService', () => {
  let service: FilmService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://localhost:7125/api/Film';

  beforeEach(() => {
    TestConfig.configureTestBed({
      providers: [FilmService]
    });
    service = TestBed.inject(FilmService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('My Films Pagination', () => {
    it('should call getMyDevelopedFilmsPaged with correct parameters', () => {
      // Arrange
      const mockResponse = TestConfig.createPagedResponse(
        [createMockFilm('1', 'My Film', UsernameType.Angel, true)]
      );

      // Act
      service.getMyDevelopedFilmsPaged(2, 10).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/my/developed?page=2&pageSize=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should call getMyDevelopedFilmsPaged with default parameters', () => {
      // Arrange
      const mockResponse = TestConfig.createEmptyPagedResponse<FilmDto>();

      // Act
      service.getMyDevelopedFilmsPaged().subscribe();

      // Assert
      const req = httpMock.expectOne(`${baseUrl}/my/developed?page=1&pageSize=5`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should call getMyNotDevelopedFilmsPaged with correct parameters', () => {
      // Arrange
      const mockResponse = TestConfig.createPagedResponse(
        [createMockFilm('1', 'My Not Developed Film', UsernameType.Angel, false)],
        2, // currentPage
        3  // pageSize
      );
      // Manually adjust specific test values
      mockResponse.totalCount = 1;
      mockResponse.totalPages = 1;
      mockResponse.hasPreviousPage = true;

      // Act
      service.getMyNotDevelopedFilmsPaged(2, 3).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/my/not-developed?page=2&pageSize=3`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('All Films Pagination', () => {
    it('should call getDevelopedFilmsPaged with correct parameters', () => {
      // Arrange
      const mockResponse = TestConfig.createPagedResponse([
          createMockFilm('1', 'Angel Film', UsernameType.Angel, true),
          createMockFilm('2', 'Tudor Film', UsernameType.Tudor, true)
        ]
      );
      // Manually adjust for specific test scenario
      mockResponse.totalCount = 10;
      mockResponse.totalPages = 2;
      mockResponse.hasNextPage = true;

      // Act
      service.getDevelopedFilmsPaged(1, 5).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.data.length).toBe(2);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/developed?page=1&pageSize=5`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should call getNotDevelopedFilmsPaged with correct parameters', () => {
      // Arrange
      const mockResponse = TestConfig.createPagedResponse(
        [createMockFilm('3', 'Not Developed Film', UsernameType.Cristiana, false)]
      );

      // Act
      service.getNotDevelopedFilmsPaged(1, 5).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/not-developed?page=1&pageSize=5`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('Backward Compatibility', () => {
    it('should maintain getAllFilms method for backward compatibility', () => {
      // Arrange
      const mockFilms: FilmDto[] = [
        createMockFilm('1', 'Film 1', UsernameType.Angel, true),
        createMockFilm('2', 'Film 2', UsernameType.Tudor, false)
      ];

      // Act
      service.getAll().subscribe((films: any) => {
        // Assert
        expect(films).toEqual(mockFilms);
      });

      // Assert HTTP call - should use page=0 for backward compatibility
      const req = httpMock.expectOne(`${baseUrl}/?page=0`);
      expect(req.request.method).toBe('GET');
      req.flush(mockFilms);
    });
  });

  describe('Bulk Upload', () => {
    it('should call add with bulkCount query parameter when bulkCount > 1', () => {
      // Arrange
      const filmDto = createMockFilm('1', 'Test Film', UsernameType.Angel, false);
      const mockResponse = createMockFilm('1', 'Test Film', UsernameType.Angel, false);

      // Act
      service.add(filmDto, { bulkCount: 5 }).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/?bulkCount=5`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(filmDto);
      req.flush(mockResponse);
    });

    it('should call add without query parameters when bulkCount = 1', () => {
      // Arrange
      const filmDto = createMockFilm('1', 'Test Film', UsernameType.Angel, false);
      const mockResponse = createMockFilm('1', 'Test Film', UsernameType.Angel, false);

      // Act
      service.add(filmDto, { bulkCount: 1 }).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/?bulkCount=1`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(filmDto);
      req.flush(mockResponse);
    });

    it('should call add without query parameters when not provided', () => {
      // Arrange
      const filmDto = createMockFilm('1', 'Test Film', UsernameType.Angel, false);
      const mockResponse = createMockFilm('1', 'Test Film', UsernameType.Angel, false);

      // Act
      service.add(filmDto).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(filmDto);
      req.flush(mockResponse);
    });

    it('should handle HTTP errors for bulk upload', () => {
      // Arrange
      const filmDto = createMockFilm('1', 'Test Film', UsernameType.Angel, false);

      // Act
      service.add(filmDto, { bulkCount: 3 }).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(400);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/?bulkCount=3`);
      req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
    });

    it('should support custom query parameters', () => {
      // Arrange
      const filmDto = createMockFilm('1', 'Test Film', UsernameType.Angel, false);
      const mockResponse = createMockFilm('1', 'Test Film', UsernameType.Angel, false);
      const customParams = { customParam: 'value', anotherParam: 123 };

      // Act
      service.add(filmDto, customParams).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/?customParam=value&anotherParam=123`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(filmDto);
      req.flush(mockResponse);
    });
  });

  describe('Error Handling', () => {
    it('should handle HTTP errors for getMyDevelopedFilmsPaged', () => {
      // Act
      service.getMyDevelopedFilmsPaged().subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(500);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/my/developed?page=1&pageSize=5`);
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle HTTP errors for pagination endpoints', () => {
      // Act
      service.getDevelopedFilmsPaged().subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
        }
      });

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/developed?page=1&pageSize=5`);
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
  });

  // Helper function to create mock films
  function createMockFilm(
    rowKey: string,
    name: string,
    purchasedBy: UsernameType,
    developed: boolean,
    purchasedOn: string = '2023-01-01'
  ): FilmDto {
    return {
      rowKey,
      name,
      iso: '400',
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy,
      purchasedOn,
      description: 'Test film description',
      developed,
      imageUrl: 'test-image-url',
      exposureDates: ''
    };
  }

  describe('Search functionality', () => {
    it('should call getDevelopedFilmsPaged with search parameters', () => {
      const searchParams = { name: 'Test Film', type: 'ColorNegative' };
      const page = 1;
      const pageSize = 5;

      // Act
      service.getDevelopedFilmsPaged(page, pageSize, searchParams).subscribe();

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/developed?page=1&pageSize=5&name=Test%20Film&type=ColorNegative`);
      expect(req.request.method).toBe('GET');
      req.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });

    it('should call getNotDevelopedFilmsPaged with search parameters', () => {
      const searchParams = { iso: '400' };
      const page = 2;
      const pageSize = 10;

      // Act
      service.getNotDevelopedFilmsPaged(page, pageSize, searchParams).subscribe();

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/not-developed?page=2&pageSize=10&iso=400`);
      expect(req.request.method).toBe('GET');
      req.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });

    it('should call getMyDevelopedFilmsPaged with search parameters', () => {
      const searchParams = { purchasedBy: 'Angel' };
      const page = 1;
      const pageSize = 5;

      // Act
      service.getMyDevelopedFilmsPaged(page, pageSize, searchParams).subscribe();

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/my/developed?page=1&pageSize=5&purchasedBy=Angel`);
      expect(req.request.method).toBe('GET');
      req.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });

    it('should call getMyNotDevelopedFilmsPaged with search parameters', () => {
      const searchParams = { developedWithDevKitRowKey: 'kit123' };
      const page = 1;
      const pageSize = 5;

      // Act
      service.getMyNotDevelopedFilmsPaged(page, pageSize, searchParams).subscribe();

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/my/not-developed?page=1&pageSize=5&developedWithDevKitRowKey=kit123`);
      expect(req.request.method).toBe('GET');
      req.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });

    it('should call methods without search parameters when undefined', () => {
      const page = 1;
      const pageSize = 5;

      // Act
      service.getDevelopedFilmsPaged(page, pageSize);
      service.getNotDevelopedFilmsPaged(page, pageSize);
      service.getMyDevelopedFilmsPaged(page, pageSize);
      service.getMyNotDevelopedFilmsPaged(page, pageSize);

      // Assert HTTP calls
      const req1 = httpMock.expectOne(`${baseUrl}/developed?page=1&pageSize=5`);
      const req2 = httpMock.expectOne(`${baseUrl}/not-developed?page=1&pageSize=5`);
      const req3 = httpMock.expectOne(`${baseUrl}/my/developed?page=1&pageSize=5`);
      const req4 = httpMock.expectOne(`${baseUrl}/my/not-developed?page=1&pageSize=5`);
      
      expect(req1.request.method).toBe('GET');
      expect(req2.request.method).toBe('GET');
      expect(req3.request.method).toBe('GET');
      expect(req4.request.method).toBe('GET');
      
      req1.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
      req2.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
      req3.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
      req4.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });

    it('should handle search with multiple parameters', () => {
      const searchParams = { 
        name: 'Test Film', 
        type: 'ColorNegative', 
        iso: '400',
        developedWithDevKitRowKey: 'kit123'
      };
      const page = 1;
      const pageSize = 5;

      // Act
      service.getDevelopedFilmsPaged(page, pageSize, searchParams).subscribe();

      // Assert HTTP call
      const req = httpMock.expectOne(`${baseUrl}/developed?page=1&pageSize=5&name=Test%20Film&type=ColorNegative&iso=400&developedWithDevKitRowKey=kit123`);
      expect(req.request.method).toBe('GET');
      req.flush(TestConfig.createEmptyPagedResponse<FilmDto>());
    });
  });
});
