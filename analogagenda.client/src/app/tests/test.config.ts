import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';

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
}
