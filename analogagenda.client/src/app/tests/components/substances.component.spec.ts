import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { SubstancesComponent } from '../../components/substances/substances.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { DevKitService } from '../../services';
import { DevKitDto, PagedResponseDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';

describe('SubstancesComponent', () => {
  let component: SubstancesComponent;
  let fixture: ComponentFixture<SubstancesComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', [
      'getAllDevKits', 
      'getAvailableDevKitsPaged', 
      'getExpiredDevKitsPaged'
    ]);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    // Set up default return values to avoid subscription errors
    const emptyPagedResponse: PagedResponseDto<DevKitDto> = {
      data: [],
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false
    };
    
    devKitServiceSpy.getAllDevKits.and.returnValue(of([]));
    devKitServiceSpy.getAvailableDevKitsPaged.and.returnValue(of(emptyPagedResponse));
    devKitServiceSpy.getExpiredDevKitsPaged.and.returnValue(of(emptyPagedResponse));

    await TestBed.configureTestingModule({
      declarations: [SubstancesComponent, CardListComponent],
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

    const availablePagedResponse: PagedResponseDto<DevKitDto> = {
      data: mockDevKits,
      totalCount: 1,
      pageSize: 5,
      currentPage: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    };

    const emptyExpiredPagedResponse: PagedResponseDto<DevKitDto> = {
      data: [],
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false
    };

    // Set up mock BEFORE component initialization
    mockDevKitService.getAvailableDevKitsPaged.and.returnValue(of(availablePagedResponse));
    mockDevKitService.getExpiredDevKitsPaged.and.returnValue(of(emptyExpiredPagedResponse));
    
    // Create new component instance with proper mocks
    fixture = TestBed.createComponent(SubstancesComponent);
    component = fixture.componentInstance;

    // Act
    fixture.detectChanges(); // This triggers constructor

    // Assert
    expect(mockDevKitService.getAvailableDevKitsPaged).toHaveBeenCalledWith(1, 5);
    expect(mockDevKitService.getExpiredDevKitsPaged).toHaveBeenCalledWith(1, 5);
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

});