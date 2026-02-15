import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { SubstancesComponent } from '../../components/substances/substances.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { ListComponent } from '../../components/common/list/list.component';
import { TableListComponent } from '../../components/common/table-list/table-list.component';
import { ImagePreviewComponent } from '../../components/common/image-preview/image-preview.component';
import { DevKitService, UserSettingsService } from '../../services';
import { DevKitDto, PagedResponseDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('SubstancesComponent', () => {
  let component: SubstancesComponent;
  let fixture: ComponentFixture<SubstancesComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const devKitServiceSpy = TestConfig.createCrudServiceSpy('DevKitService', [
      'getAvailableDevKitsPaged',
      'getExpiredDevKitsPaged'
    ]);
    const routerSpy = TestConfig.createRouterSpy();
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['getUserSettings']);
    userSettingsServiceSpy.getUserSettings.and.returnValue(of({
      userId: 'user1',
      isSubscribed: false,
      tableView: false,
      entitiesPerPage: 5
    }));

    const emptyPagedResponse = TestConfig.createEmptyPagedResponse<DevKitDto>();
    TestConfig.setupPaginatedServiceMocks(devKitServiceSpy, [], {
      getAvailableDevKitsPaged: emptyPagedResponse,
      getExpiredDevKitsPaged: emptyPagedResponse
    });

    await TestConfig.configureTestBed({
      declarations: [SubstancesComponent, CardListComponent, ListComponent, TableListComponent, ImagePreviewComponent],
      providers: [
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
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
        id: '1',
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
      }
    ];

    const availablePagedResponse = TestConfig.createPagedResponse(mockDevKits);
    const emptyExpiredPagedResponse = TestConfig.createEmptyPagedResponse<DevKitDto>();

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
    const id = 'test-row-key';

    // Act
    component.onKitSelected(id);

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances/' + id]);
  });

});