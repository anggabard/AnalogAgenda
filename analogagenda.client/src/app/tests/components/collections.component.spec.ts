import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { CollectionsComponent } from '../../components/collections/collections.component';
import { CollectionService, UserSettingsService } from '../../services';
import { CollectionDto, PagedResponseDto, UserSettingsDto } from '../../DTOs';
import { TestConfig } from '../test.config';

describe('CollectionsComponent', () => {
  let component: CollectionsComponent;
  let fixture: ComponentFixture<CollectionsComponent>;
  let mockCollectionService: jasmine.SpyObj<CollectionService>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const defaultSettings: UserSettingsDto = {
    userId: 'u1',
    isSubscribed: false,
    tableView: false,
    entitiesPerPage: 3,
  };

  function createCollection(id: string, name: string): CollectionDto {
    return {
      id,
      name,
      imageUrl: '',
      owner: 'Angel',
      photoIds: [],
      photoCount: 0,
      isOpen: true,
      imageId: '',
      location: '',
      fromDate: null,
      toDate: null,
    };
  }

  function paged(data: CollectionDto[]): PagedResponseDto<CollectionDto> {
    return {
      data,
      currentPage: 1,
      pageSize: 10_000,
      totalCount: data.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    };
  }

  beforeEach(async () => {
    const collectionSpy = jasmine.createSpyObj('CollectionService', ['getMinePaged']);
    const userSettingsSpy = jasmine.createSpyObj('UserSettingsService', ['getUserSettings']);
    const routerSpy = TestConfig.createRouterSpy();

    userSettingsSpy.getUserSettings.and.returnValue(of(defaultSettings));
    collectionSpy.getMinePaged.and.returnValue(of(paged([createCollection('1', 'A')])));

    await TestBed.configureTestingModule({
      declarations: [CollectionsComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: CollectionService, useValue: collectionSpy },
        { provide: UserSettingsService, useValue: userSettingsSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    mockCollectionService = TestBed.inject(CollectionService) as jasmine.SpyObj<CollectionService>;
    mockUserSettingsService = TestBed.inject(UserSettingsService) as jasmine.SpyObj<UserSettingsService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    fixture = TestBed.createComponent(CollectionsComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads all collections in one request with large page size and sets tableView from settings', () => {
    mockCollectionService.getMinePaged.and.returnValue(
      of(paged([createCollection('1', 'First'), createCollection('2', 'Second')]))
    );

    fixture.detectChanges();

    expect(mockUserSettingsService.getUserSettings).toHaveBeenCalled();
    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(1, 10_000);
    expect(component.settingsLoaded).toBeTrue();
    expect(component.tableView).toBeFalse();
    expect(component.collections.length).toBe(2);
    expect(component.loading).toBeFalse();
  });

  it('uses tableView when user settings enable table view', () => {
    mockUserSettingsService.getUserSettings.and.returnValue(
      of({ ...defaultSettings, tableView: true })
    );

    fixture.detectChanges();

    expect(component.tableView).toBeTrue();
  });
});
