import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NEVER, of } from 'rxjs';
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

  function paged(
    data: CollectionDto[],
    opts: { hasNextPage: boolean; currentPage?: number; pageSize?: number; totalCount?: number }
  ): PagedResponseDto<CollectionDto> {
    const pageSize = opts.pageSize ?? 3;
    const currentPage = opts.currentPage ?? 1;
    const totalCount = opts.totalCount ?? data.length;
    return {
      data,
      currentPage,
      pageSize,
      totalCount,
      totalPages: Math.ceil(totalCount / pageSize),
      hasNextPage: opts.hasNextPage,
      hasPreviousPage: currentPage > 1,
    };
  }

  beforeEach(async () => {
    const collectionSpy = jasmine.createSpyObj('CollectionService', ['getMinePaged']);
    const userSettingsSpy = jasmine.createSpyObj('UserSettingsService', ['getUserSettings']);
    const routerSpy = TestConfig.createRouterSpy();

    userSettingsSpy.getUserSettings.and.returnValue(of(defaultSettings));
    collectionSpy.getMinePaged.and.returnValue(
      of(paged([createCollection('1', 'A')], { hasNextPage: false }))
    );

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

  it('loads first page with page size from user settings and sets hasMore from response', () => {
    const first = paged([createCollection('1', 'First')], {
      hasNextPage: true,
      totalCount: 10,
      currentPage: 1,
      pageSize: 3,
    });
    mockCollectionService.getMinePaged.and.returnValue(of(first));

    fixture.detectChanges();

    expect(mockUserSettingsService.getUserSettings).toHaveBeenCalled();
    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(1, 3);
    expect(component.collections.length).toBe(1);
    expect(component.hasMore).toBeTrue();
    expect(component.loading).toBeFalse();
  });

  it('requests the next page number after a successful load (paging contract)', () => {
    const page1 = paged([createCollection('1', 'One')], { hasNextPage: true, totalCount: 5 });
    const page2 = paged([createCollection('2', 'Two')], { hasNextPage: false, totalCount: 5, currentPage: 2 });
    mockCollectionService.getMinePaged.and.returnValues(of(page1), of(page2));

    fixture.detectChanges();
    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(1, 3);

    component.loadMoreCollections();

    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(2, 3);
    expect(component.collections.length).toBe(2);
    expect(component.hasMore).toBeFalse();
  });

  it('does not call the API again while a page request is in flight (loading guard)', () => {
    mockCollectionService.getMinePaged.and.returnValue(NEVER);

    fixture.detectChanges();

    expect(component.loading).toBeTrue();
    const callsAfterFirst = mockCollectionService.getMinePaged.calls.count();

    component.loadNextPage();

    expect(mockCollectionService.getMinePaged.calls.count()).toBe(callsAfterFirst);
  });

  it('after scroll debounce, loads more when near bottom and hasMore and not loading', fakeAsync(() => {
    const page1 = paged([createCollection('1', 'One')], { hasNextPage: true, totalCount: 4 });
    const page2 = paged([createCollection('2', 'Two')], { hasNextPage: false, totalCount: 4, currentPage: 2 });
    mockCollectionService.getMinePaged.and.returnValues(of(page1), of(page2));

    fixture.detectChanges();
    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(1, 3);

    spyOnProperty(window, 'innerHeight', 'get').and.returnValue(800);
    spyOnProperty(window, 'scrollY', 'get').and.returnValue(1000);
    spyOnProperty(document.body, 'offsetHeight', 'get').and.returnValue(1500);
    // pos = 1800, max = 1200 → near bottom

    component.onWindowScroll();
    tick(150);

    expect(mockCollectionService.getMinePaged).toHaveBeenCalledWith(2, 3);
  }));

  it('does not load more on scroll when above threshold', fakeAsync(() => {
    const page1 = paged([createCollection('1', 'One')], { hasNextPage: true, totalCount: 4 });
    mockCollectionService.getMinePaged.and.returnValue(of(page1));

    fixture.detectChanges();
    const callsAfterInit = mockCollectionService.getMinePaged.calls.count();

    spyOnProperty(window, 'innerHeight', 'get').and.returnValue(600);
    spyOnProperty(window, 'scrollY', 'get').and.returnValue(100);
    spyOnProperty(document.body, 'offsetHeight', 'get').and.returnValue(5000);
    // pos = 700, max = 4700 → not near bottom

    component.onWindowScroll();
    tick(150);

    expect(mockCollectionService.getMinePaged.calls.count()).toBe(callsAfterInit);
  }));

  it('does not throw when destroyed with a pending scroll debounce', fakeAsync(() => {
    mockCollectionService.getMinePaged.and.returnValue(
      of(paged([createCollection('1', 'One')], { hasNextPage: false }))
    );
    fixture.detectChanges();

    component.onWindowScroll();
    expect(() => component.ngOnDestroy()).not.toThrow();
    tick(500);
  }));
});
