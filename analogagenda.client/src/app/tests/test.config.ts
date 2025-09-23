import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { PagedResponseDto } from '../DTOs';

/**
 * Common test configuration utilities
 */
export class TestConfig {
  
  /**
   * Create a mock router spy for testing
   */
  static createRouterSpy(): jasmine.SpyObj<Router> {
    return jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);
  }

  /**
   * Common TestBed configuration for components
   */
  static configureTestBed(config: {
    declarations?: any[];
    imports?: any[];
    providers?: any[];
  }) {
    return TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, ...(config.imports || [])],
      declarations: config.declarations || [],
      providers: config.providers || []
    });
  }

  /**
   * Create a mock service spy with common CRUD methods
   */
  static createCrudServiceSpy<T>(serviceName: string, additionalMethods: string[] = []): jasmine.SpyObj<any> {
    const methods = [
      'getAll', 'getPaged', 'getById', 'add', 'update', 'delete',
      ...additionalMethods
    ];
    const spy = jasmine.createSpyObj(serviceName, methods);
    
    // Set up default return values
    spy.getAll.and.returnValue(of([]));
    spy.getPaged.and.returnValue(of(TestConfig.createEmptyPagedResponse()));
    spy.getById.and.returnValue(of({}));
    spy.add.and.returnValue(of({}));
    spy.update.and.returnValue(of({}));
    spy.delete.and.returnValue(of({}));
    
    return spy;
  }

  /**
   * Create an empty paged response for testing
   */
  static createEmptyPagedResponse<T>(): PagedResponseDto<T> {
    return {
      data: [],
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false
    };
  }

  /**
   * Create a paged response with test data
   */
  static createPagedResponse<T>(data: T[], currentPage: number = 1, pageSize: number = 5): PagedResponseDto<T> {
    const totalCount = data.length;
    const totalPages = Math.ceil(totalCount / pageSize);
    
    return {
      data,
      totalCount,
      pageSize,
      currentPage,
      totalPages,
      hasNextPage: currentPage < totalPages,
      hasPreviousPage: currentPage > 1
    };
  }

  /**
   * Setup common mocks for paginated services
   */
  static setupPaginatedServiceMocks<T>(
    serviceSpy: jasmine.SpyObj<any>, 
    testData: T[], 
    additionalMethods?: Record<string, any>
  ): void {
    serviceSpy.getAll.and.returnValue(of(testData));
    serviceSpy.getPaged.and.returnValue(of(TestConfig.createPagedResponse(testData)));
    
    if (additionalMethods) {
      Object.entries(additionalMethods).forEach(([method, returnValue]) => {
        if (serviceSpy[method]) {
          serviceSpy[method].and.returnValue(of(returnValue));
        }
      });
    }
  }
}
