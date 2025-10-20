import { TestBed } from '@angular/core/testing';
import { LocalStorageService } from '../../services/local-storage.service';

describe('LocalStorageService', () => {
  let service: LocalStorageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LocalStorageService);
    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    // Clean up localStorage after each test
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should save and retrieve state', () => {
    const testData = { key: 'value', number: 123, array: [1, 2, 3] };
    const key = 'test-key';

    service.saveState(key, testData);
    const retrievedData = service.getState(key);

    expect(retrievedData).toEqual(testData);
  });

  it('should return null when no state is saved', () => {
    const retrievedState = service.getState('non-existent-key');
    expect(retrievedState).toBeNull();
  });

  it('should clear state', () => {
    const testData = { key: 'value' };
    const key = 'test-key';

    service.saveState(key, testData);
    service.clearState(key);
    const retrievedData = service.getState(key);

    expect(retrievedData).toBeNull();
  });

  it('should check if state exists', () => {
    const testData = { key: 'value' };
    const key = 'test-key';

    expect(service.hasState(key)).toBeFalse();

    service.saveState(key, testData);
    expect(service.hasState(key)).toBeTrue();

    service.clearState(key);
    expect(service.hasState(key)).toBeFalse();
  });

  it('should handle complex objects', () => {
    const complexData = {
      user: { name: 'John', age: 30 },
      settings: { theme: 'dark', notifications: true },
      items: [{ id: 1, name: 'item1' }, { id: 2, name: 'item2' }]
    };
    const key = 'complex-data';

    service.saveState(key, complexData);
    const retrievedData = service.getState(key);

    expect(retrievedData).toEqual(complexData);
  });

  it('should handle errors gracefully when localStorage is not available', () => {
    // Mock localStorage to throw an error
    spyOn(localStorage, 'setItem').and.throwError('Storage not available');
    spyOn(localStorage, 'getItem').and.throwError('Storage not available');
    spyOn(localStorage, 'removeItem').and.throwError('Storage not available');

    const testData = { key: 'value' };
    const key = 'test-key';

    // Should not throw an error
    expect(() => {
      service.saveState(key, testData);
      service.getState(key);
      service.clearState(key);
      service.hasState(key);
    }).not.toThrow();
  });

  it('should handle JSON parsing errors gracefully', () => {
    // Mock localStorage to return invalid JSON
    spyOn(localStorage, 'getItem').and.returnValue('invalid-json');

    const result = service.getState('test-key');
    expect(result).toBeNull();
  });
});
