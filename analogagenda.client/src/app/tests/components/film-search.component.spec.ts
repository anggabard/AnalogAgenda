import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { FilmSearchComponent } from '../../components/films/film-search/film-search.component';
import { DevKitService, SessionService } from '../../services';
import { DevKitDto, SessionDto } from '../../DTOs';

describe('FilmSearchComponent', () => {
  let component: FilmSearchComponent;
  let fixture: ComponentFixture<FilmSearchComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockSessionService: jasmine.SpyObj<SessionService>;

  const mockDevKits: DevKitDto[] = [
    { rowKey: 'kit1', name: 'Belini', type: 'C41' as any, url: 'url1', purchasedBy: 'Angel' as any, purchasedOn: '2023-01-01', mixedOn: '2023-01-01', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, imageUrl: '', description: '', expired: false },
    { rowKey: 'kit2', name: 'Kodak', type: 'E6' as any, url: 'url2', purchasedBy: 'Cristiana' as any, purchasedOn: '2023-01-01', mixedOn: '2023-01-01', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, imageUrl: '', description: '', expired: false }
  ];

  const mockSessions: SessionDto[] = [
    { rowKey: 'session1', sessionDate: '2023-01-15', location: 'Studio A', participants: '[]', participantsList: [], description: '', usedSubstances: '[]', usedSubstancesList: [], developedFilms: '[]', developedFilmsList: [], imageUrl: '', imageBase64: '' },
    { rowKey: 'session2', sessionDate: '2023-02-20', location: 'Studio B', participants: '[]', participantsList: [], description: '', usedSubstances: '[]', usedSubstancesList: [], developedFilms: '[]', developedFilmsList: [], imageUrl: '', imageBase64: '' }
  ];

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getAll']);

    await TestBed.configureTestingModule({
      imports: [FilmSearchComponent, FormsModule],
      providers: [
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: SessionService, useValue: sessionServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmSearchComponent);
    component = fixture.componentInstance;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockSessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;

    mockDevKitService.getAll.and.returnValue(of(mockDevKits));
    mockSessionService.getAll.and.returnValue(of(mockSessions));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default name field visible', () => {
    fixture.detectChanges();
    
    const nameField = component.searchFields.find(f => f.key === 'name');
    expect(nameField?.visible).toBe(true);
    expect(nameField?.defaultVisible).toBe(true);
  });

  it('should load dev kits and sessions on init', () => {
    fixture.detectChanges();
    
    expect(mockDevKitService.getAll).toHaveBeenCalled();
    expect(mockSessionService.getAll).toHaveBeenCalled();
  });

  it('should format dev kit options correctly', () => {
    fixture.detectChanges();
    
    const kitField = component.searchFields.find(f => f.key === 'developedWithDevKitRowKey');
    expect(kitField?.options).toEqual([
      { value: 'kit1', label: 'Belini - C41' },
      { value: 'kit2', label: 'Kodak - E6' }
    ]);
  });

  it('should format session options correctly', () => {
    fixture.detectChanges();
    
    const sessionField = component.searchFields.find(f => f.key === 'developedInSessionRowKey');
    expect(sessionField?.options).toEqual([
      { value: 'session1', label: '2023-01-15 - Studio A' },
      { value: 'session2', label: '2023-02-20 - Studio B' }
    ]);
  });

  it('should toggle field visibility', () => {
    const field = component.searchFields.find(f => f.key === 'id');
    expect(field?.visible).toBe(false);
    
    component.toggleFieldVisibility(field!);
    expect(field?.visible).toBe(true);
    
    component.toggleFieldVisibility(field!);
    expect(field?.visible).toBe(false);
  });

  it('should clear field value when hiding field', () => {
    const field = component.searchFields.find(f => f.key === 'id');
    field!.value = 'test value';
    field!.visible = true;
    
    component.toggleFieldVisibility(field!);
    expect(field?.value).toBe('');
  });

  it('should emit search event with non-empty values', () => {
    spyOn(component.search, 'emit');
    
    const nameField = component.searchFields.find(f => f.key === 'name');
    const typeField = component.searchFields.find(f => f.key === 'type');
    
    nameField!.value = 'Test Film';
    typeField!.value = 'ColorNegative';
    typeField!.visible = true;
    
    component.onSearch();
    
    expect(component.search.emit).toHaveBeenCalledWith({
      name: 'Test Film',
      type: 'ColorNegative'
    });
  });

  it('should not emit empty values in search', () => {
    spyOn(component.search, 'emit');
    
    const nameField = component.searchFields.find(f => f.key === 'name');
    const typeField = component.searchFields.find(f => f.key === 'type');
    
    nameField!.value = 'Test Film';
    typeField!.value = '';
    typeField!.visible = true;
    
    component.onSearch();
    
    expect(component.search.emit).toHaveBeenCalledWith({
      name: 'Test Film'
    });
  });

  it('should emit clear filters event', () => {
    spyOn(component.clearFilters, 'emit');
    
    component.onClearFilters();
    
    expect(component.clearFilters.emit).toHaveBeenCalled();
  });

  it('should clear all field values on clear filters', () => {
    const nameField = component.searchFields.find(f => f.key === 'name');
    const typeField = component.searchFields.find(f => f.key === 'type');
    
    nameField!.value = 'Test Film';
    typeField!.value = 'ColorNegative';
    
    component.onClearFilters();
    
    expect(nameField?.value).toBe('');
    expect(typeField?.value).toBe('');
  });

  it('should get visible fields correctly', () => {
    const nameField = component.searchFields.find(f => f.key === 'name');
    const idField = component.searchFields.find(f => f.key === 'id');
    
    nameField!.visible = true;
    idField!.visible = false;
    
    const visibleFields = component.getVisibleFields();
    
    expect(visibleFields).toContain(nameField!);
    expect(visibleFields).not.toContain(idField!);
  });

  it('should get available fields correctly', () => {
    const availableFields = component.getAvailableFields();
    
    // Should return all fields (not just invisible ones)
    expect(availableFields.length).toBe(component.searchFields.length);
  });

  it('should filter available fields based on isMyFilmsTab', () => {
    component.isMyFilmsTab = true;
    const availableFields = component.getAvailableFields();
    
    // Owner field should NOT be available in My Films tab
    // Check that purchasedBy field is not in available fields
    const ownerField = availableFields.find(f => f.key === 'purchasedBy');
    expect(ownerField).toBeUndefined();
    
    // Check that all available fields have availableInMyFilms: true
    availableFields.forEach(field => {
      expect(field.availableInMyFilms).toBe(true);
    });
  });

  it('should toggle field selector visibility', () => {
    expect(component.showFieldSelector).toBe(false);
    
    component.toggleFieldSelector();
    expect(component.showFieldSelector).toBe(true);
    
    component.toggleFieldSelector();
    expect(component.showFieldSelector).toBe(false);
  });

  it('should close field selector when clicking outside', () => {
    component.showFieldSelector = true;
    
    const mockEvent = new MouseEvent('click');
    Object.defineProperty(mockEvent, 'target', {
      value: document.body,
      writable: false
    });
    
    component.onDocumentClick(mockEvent);
    
    expect(component.showFieldSelector).toBe(false);
  });

  it('should trigger search on enter key', () => {
    spyOn(component, 'onSearch');
    
    component.onEnterKey();
    
    expect(component.onSearch).toHaveBeenCalled();
  });

  it('should have correct film types', () => {
    expect(component.filmTypes).toEqual([
      { value: 'ColorNegative', label: 'Color Negative' },
      { value: 'ColorPositive', label: 'Color Positive' },
      { value: 'BlackAndWhite', label: 'Black And White' }
    ]);
  });

  it('should have correct username types', () => {
    expect(component.usernameTypes).toEqual([
      { value: 'Angel', label: 'Angel' },
      { value: 'Cristiana', label: 'Cristiana' },
      { value: 'Tudor', label: 'Tudor' }
    ]);
  });
});
