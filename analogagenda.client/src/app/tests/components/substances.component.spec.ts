import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { SubstancesComponent } from '../../components/substances/substances.component';
import { DevKitService } from '../../services';
import { DevKitDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';

describe('SubstancesComponent', () => {
  let component: SubstancesComponent;
  let fixture: ComponentFixture<SubstancesComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAllDevKits']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    // Set up default return values to avoid subscription errors
    devKitServiceSpy.getAllDevKits.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [SubstancesComponent],
      providers: [
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SubstancesComponent);
    component = fixture.componentInstance;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should load dev kits on initialization', () => {
    // Arrange
    const mockDevKits: DevKitDto[] = [
      {
        rowKey: '1',
        name: 'Test Kit',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test description',
        expired: false,
        imageUrl: 'test-url',
        imageBase64: ''
      }
    ];

    // Set up mock BEFORE component initialization
    mockDevKitService.getAllDevKits.and.returnValue(of(mockDevKits));
    
    // Create new component instance with proper mocks
    fixture = TestBed.createComponent(SubstancesComponent);
    component = fixture.componentInstance;

    // Act
    fixture.detectChanges(); // This triggers constructor

    // Assert
    expect(mockDevKitService.getAllDevKits).toHaveBeenCalled();
    expect(component.availableDevKits.length).toBe(1);
    expect(component.expiredDevKits.length).toBe(0);
  });

  it('should navigate to new kit page when onNewKitClick is called', () => {
    // Act
    component.onNewKitClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances/new']);
  });

  it('should navigate to kit details when onKitSelected is called', () => {
    // Arrange
    const rowKey = 'test-row-key';

    // Act
    component.onKitSelected(rowKey);

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances/' + rowKey]);
  });

  it('should split dev kits into available and expired correctly', () => {
    // Arrange
    const mockDevKits: DevKitDto[] = [
      {
        rowKey: '1',
        name: 'Available Kit 1',
        url: 'http://example1.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Available kit',
        expired: false,
        imageUrl: 'test-url-1',
        imageBase64: ''
      },
      {
        rowKey: '2',
        name: 'Expired Kit',
        url: 'http://example2.com',
        type: DevKitType.E6,
        purchasedBy: UsernameType.Tudor,
        purchasedOn: '2022-01-01',
        mixedOn: '2022-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 8,
        description: 'Expired kit',
        expired: true,
        imageUrl: 'test-url-2',
        imageBase64: ''
      },
      {
        rowKey: '3',
        name: 'Available Kit 2',
        url: 'http://example3.com',
        type: DevKitType.BW,
        purchasedBy: UsernameType.Cristiana,
        purchasedOn: '2023-06-01',
        mixedOn: '2023-06-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 2,
        description: 'Another available kit',
        expired: false,
        imageUrl: 'test-url-3',
        imageBase64: ''
      }
    ];

    // Set up mock BEFORE component initialization
    mockDevKitService.getAllDevKits.and.returnValue(of(mockDevKits));
    
    // Create new component instance with proper mocks
    fixture = TestBed.createComponent(SubstancesComponent);
    component = fixture.componentInstance;

    // Act
    fixture.detectChanges(); // Properly initialize component

    // Assert
    expect(component.availableDevKits.length).toBe(2);
    expect(component.expiredDevKits.length).toBe(1);
    expect(component.availableDevKits.every(kit => !kit.expired)).toBeTruthy();
    expect(component.expiredDevKits.every(kit => kit.expired)).toBeTruthy();
  });

  it('should sort dev kits by purchasedOn date', () => {
    // Arrange
    const mockDevKits: DevKitDto[] = [
      {
        rowKey: '1',
        name: 'Kit June',
        url: 'http://example1.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-06-01',
        mixedOn: '2023-06-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'June kit',
        expired: false,
        imageUrl: 'test-url-1',
        imageBase64: ''
      },
      {
        rowKey: '2',
        name: 'Kit January',
        url: 'http://example2.com',
        type: DevKitType.E6,
        purchasedBy: UsernameType.Tudor,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'January kit',
        expired: false,
        imageUrl: 'test-url-2',
        imageBase64: ''
      },
      {
        rowKey: '3',
        name: 'Kit December',
        url: 'http://example3.com',
        type: DevKitType.BW,
        purchasedBy: UsernameType.Cristiana,
        purchasedOn: '2023-12-01',
        mixedOn: '2023-12-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'December kit',
        expired: false,
        imageUrl: 'test-url-3',
        imageBase64: ''
      }
    ];

    // Set up mock BEFORE component initialization
    mockDevKitService.getAllDevKits.and.returnValue(of(mockDevKits));
    
    // Create new component instance with proper mocks
    fixture = TestBed.createComponent(SubstancesComponent);
    component = fixture.componentInstance;

    // Act
    fixture.detectChanges(); // Properly initialize component

    // Assert
    expect(component.availableDevKits.length).toBe(3);
    // Should be sorted by purchasedOn (earliest first)
    expect(component.availableDevKits[0].purchasedOn).toBe('2023-01-01');
    expect(component.availableDevKits[1].purchasedOn).toBe('2023-06-01');
    expect(component.availableDevKits[2].purchasedOn).toBe('2023-12-01');
  });
});