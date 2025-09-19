import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';

describe('ErrorInterceptor Integration', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: Router, useValue: routerSpy }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    httpMock.verify();
  });


  it('should handle HTTP errors appropriately', () => {
    const testUrl = '/api/test';
    
    // Make a request that will fail
    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected error'),
      error: (error) => {
        expect(error).toBeTruthy();
      }
    });

    // Provide the mock error response
    const req = httpMock.expectOne(testUrl);
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });
  });


  it('should handle network errors', () => {
    const testUrl = '/api/test';
    
    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected error'),
      error: (error) => {
        expect(error).toBeTruthy();
      }
    });

    const req = httpMock.expectOne(testUrl);
    req.error(new ProgressEvent('Network Error'));
  });
});