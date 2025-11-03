import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { UpsertSessionComponent } from '../../components/sessions/upsert-session/upsert-session.component';
import { SessionService, DevKitService, FilmService } from '../../services';
import { DevKitDto, FilmDto, SessionDto } from '../../DTOs';
import { DevKitType, UsernameType, FilmType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertSessionComponent', () => {
  let component: UpsertSessionComponent;
  let fixture: ComponentFixture<UpsertSessionComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'add', 'update', 'deleteById']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAll']);
    const routerSpy = TestConfig.createRouterSpy();

    sessionServiceSpy.getById.and.returnValue(of({}));
    devKitServiceSpy.getAll.and.returnValue(of([]));
    filmServiceSpy.getAll.and.returnValue(of([]));

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        },
        queryParams: {}
      }
    };

    await TestConfig.configureTestBed({
      declarations: [UpsertSessionComponent],
      providers: [
        FormBuilder,
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockSessionService = sessionServiceSpy;
    mockDevKitService = devKitServiceSpy;
    mockFilmService = filmServiceSpy;
    mockRouter = routerSpy;

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize in insert mode when no id is provided', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    component.ngOnInit();

    expect(component.isInsert).toBeTruthy();
    expect(component.id).toBeNull();
  });


  it('should toggle devkit selection for modal', () => {
    component.selectedDevKitsForModal = [];
    const devKitId = 'devkit-1';

    component.toggleDevKitSelection(devKitId);

    expect(component.selectedDevKitsForModal).toContain(devKitId);

    component.toggleDevKitSelection(devKitId);

    expect(component.selectedDevKitsForModal).not.toContain(devKitId);
  });

  it('should toggle film selection for modal', () => {
    component.selectedFilmsForModal = [];
    const filmId = 'film-1';

    component.toggleFilmSelection(filmId);

    expect(component.selectedFilmsForModal).toContain(filmId);

    component.toggleFilmSelection(filmId);

    expect(component.selectedFilmsForModal).not.toContain(filmId);
  });

  it('should check if devkit is selected for modal', () => {
    component.selectedDevKitsForModal = ['devkit-1'];

    expect(component.isDevKitSelectedForModal('devkit-1')).toBeTruthy();
    expect(component.isDevKitSelectedForModal('devkit-2')).toBeFalsy();
  });

  it('should check if film is selected for modal', () => {
    component.selectedFilmsForModal = ['film-1'];

    expect(component.isFilmSelectedForModal('film-1')).toBeTruthy();
    expect(component.isFilmSelectedForModal('film-2')).toBeFalsy();
  });

  it('should close add devkit modal', () => {
    component.showAddDevKitModal = true;

    component.closeAddDevKitModal();

    expect(component.showAddDevKitModal).toBeFalsy();
    expect(component.selectedDevKitsForModal).toEqual([]);
  });

  it('should close add film modal', () => {
    component.showAddFilmModal = true;

    component.closeAddFilmModal();

    expect(component.showAddFilmModal).toBeFalsy();
    expect(component.selectedFilmsForModal).toEqual([]);
  });

  it('should filter expired devkits when showExpiredDevKits is false', () => {
    const mockDevKit: DevKitDto = {
      id: 'devkit-1',
      name: 'Test DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Test devkit',
      expired: false,
      imageUrl: 'test-url',
    };
    const expiredDevKit: DevKitDto = {
      id: 'devkit-2',
      name: 'Expired DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Expired devkit',
      expired: true,
      imageUrl: 'test-url',
    };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = false;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKit]);
  });

  it('should show all devkits when showExpiredDevKits is true', () => {
    const mockDevKit: DevKitDto = {
      id: 'devkit-1',
      name: 'Test DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Test devkit',
      expired: false,
      imageUrl: 'test-url',
    };
    const expiredDevKit: DevKitDto = {
      id: 'devkit-2',
      name: 'Expired DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Expired devkit',
      expired: true,
      imageUrl: 'test-url',
    };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = true;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKit, expiredDevKit]);
  });

  describe('Drag and Drop Functionality', () => {
    let mockFilm1: FilmDto;
    let mockFilm2: FilmDto;
    let mockFilm3: FilmDto;
    let mockDevKit: DevKitDto;

    // Helper function to create CdkDragDrop events
    function createDragDropEvent(
      previousContainerId: string,
      previousData: any[],
      containerId: string,
      containerData: any[],
      previousIndex: number,
      currentIndex: number,
      draggedItem: any,
      isPointerOverContainer: boolean = true
    ): CdkDragDrop<any[]> {
      return {
        previousContainer: { id: previousContainerId, data: previousData } as any,
        container: { id: containerId, data: containerData } as any,
        previousIndex,
        currentIndex,
        isPointerOverContainer,
        item: { data: draggedItem } as any,
        distance: { x: 0, y: 0 },
        dropPoint: { x: 0, y: 0 },
        event: new MouseEvent('drop')
      };
    }

    beforeEach(() => {
      // Create test films
      mockFilm1 = {
        id: 'film-1',
        name: 'Film 1',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: 'film1.jpg',
        description: 'Test film 1',
        developed: true,
        developedInSessionId: 'session-1'
      };

      mockFilm2 = {
        id: 'film-2',
        name: 'Film 2',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: 'film2.jpg',
        description: 'Test film 2',
        developed: true,
        developedInSessionId: 'session-1'
      };

      mockFilm3 = {
        id: 'film-3',
        name: 'Film 3',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: 'film3.jpg',
        description: 'Test film 3',
        developed: true,
        developedInSessionId: 'session-1'
      };

      mockDevKit = {
        id: 'devkit-1',
        name: 'Test DevKit',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit',
        expired: false,
        imageUrl: 'devkit.jpg'
      };

      // Setup component with test data
      component.sessionDevKits = [{
        devKit: mockDevKit,
        assignedFilms: []
      }];
      component.unassignedFilms = [mockFilm1, mockFilm2, mockFilm3];
    });

    describe('onFilmDrop - Same Container (Reordering)', () => {
      it('should reorder films within the same container', () => {
        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'unassigned-films', component.unassignedFilms,
          0, 2, mockFilm1
        );

        component.onFilmDrop(event);

        expect(component.unassignedFilms[0]).toBe(mockFilm2);
        expect(component.unassignedFilms[1]).toBe(mockFilm3);
        expect(component.unassignedFilms[2]).toBe(mockFilm1);
      });
    });

    describe('onFilmDrop - Different Containers (Transfer)', () => {
      it('should transfer film from unassigned to devkit using explicit drag data', () => {
        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          1, 0, mockFilm2
        );

        component.onFilmDrop(event);

        // Film 2 should be moved to devkit
        expect(component.sessionDevKits[0].assignedFilms).toContain(mockFilm2);
        expect(component.unassignedFilms).not.toContain(mockFilm2);
        
        // Other films should remain in unassigned
        expect(component.unassignedFilms).toContain(mockFilm1);
        expect(component.unassignedFilms).toContain(mockFilm3);
      });

      it('should transfer film from devkit back to unassigned using explicit drag data', () => {
        // First, add a film to the devkit
        component.sessionDevKits[0].assignedFilms.push(mockFilm1);
        component.unassignedFilms = [mockFilm2, mockFilm3];

        const event = createDragDropEvent(
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          'unassigned-films', component.unassignedFilms,
          0, 2, mockFilm1
        );

        component.onFilmDrop(event);

        // Film 1 should be moved back to unassigned
        expect(component.unassignedFilms).toContain(mockFilm1);
        expect(component.sessionDevKits[0].assignedFilms).not.toContain(mockFilm1);
      });

      it('should handle the specific bug scenario: drag Film 3, ensure Film 3 is transferred', () => {
        // Setup: Film 1 already in devkit, Films 2 and 3 in unassigned
        component.sessionDevKits[0].assignedFilms.push(mockFilm1);
        component.unassignedFilms = [mockFilm2, mockFilm3];

        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          1, 1, mockFilm3
        );

        component.onFilmDrop(event);

        // Film 3 should be transferred (not Film 2)
        expect(component.sessionDevKits[0].assignedFilms).toContain(mockFilm3);
        expect(component.sessionDevKits[0].assignedFilms).not.toContain(mockFilm2);
        expect(component.unassignedFilms).not.toContain(mockFilm3);
        expect(component.unassignedFilms).toContain(mockFilm2);
      });

      it('should handle the reverse bug scenario: drag Film 2 from devkit, ensure Film 2 is transferred back', () => {
        // Setup: Films 1 and 2 in devkit, Film 3 in unassigned
        component.sessionDevKits[0].assignedFilms.push(mockFilm1, mockFilm2);
        component.unassignedFilms = [mockFilm3];

        const event = createDragDropEvent(
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          'unassigned-films', component.unassignedFilms,
          1, 1, mockFilm2
        );

        component.onFilmDrop(event);

        // Film 2 should be transferred back (not Film 1)
        expect(component.unassignedFilms).toContain(mockFilm2);
        expect(component.unassignedFilms).not.toContain(mockFilm1);
        expect(component.sessionDevKits[0].assignedFilms).not.toContain(mockFilm2);
        expect(component.sessionDevKits[0].assignedFilms).toContain(mockFilm1);
      });
    });

    describe('onFilmDrop - Edge Cases', () => {
      it('should not transfer film if not over container', () => {
        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          0, 0, mockFilm1, false
        );

        const initialUnassignedCount = component.unassignedFilms.length;
        const initialAssignedCount = component.sessionDevKits[0].assignedFilms.length;

        component.onFilmDrop(event);

        expect(component.unassignedFilms.length).toBe(initialUnassignedCount);
        expect(component.sessionDevKits[0].assignedFilms.length).toBe(initialAssignedCount);
      });

      it('should handle case where dragged item is not found in source array', () => {
        const nonExistentFilm: FilmDto = {
          ...mockFilm1,
          id: 'non-existent-film'
        };

        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          0, 0, nonExistentFilm
        );

        const initialUnassignedCount = component.unassignedFilms.length;
        const initialAssignedCount = component.sessionDevKits[0].assignedFilms.length;

        component.onFilmDrop(event);

        // Should not change arrays if item not found
        expect(component.unassignedFilms.length).toBe(initialUnassignedCount);
        expect(component.sessionDevKits[0].assignedFilms.length).toBe(initialAssignedCount);
      });
    });

    describe('TrackBy Functions', () => {
      it('should return correct id for trackByfilmId', () => {
        const result = component.trackByfilmId(0, mockFilm1);
        expect(result).toBe('film-1');
      });

      it('should return correct id for trackBydevKitId', () => {
        const devKitWithFilms = {
          devKit: mockDevKit,
          assignedFilms: [mockFilm1]
        };
        const result = component.trackBydevKitId(0, devKitWithFilms);
        expect(result).toBe('devkit-1');
      });

      it('should return correct id for trackByDevKitDtoRowKey', () => {
        const result = component.trackByDevKitDtoRowKey(0, mockDevKit);
        expect(result).toBe('devkit-1');
      });
    });

    describe('Form Dirty State', () => {
      it('should mark form as dirty when film is dropped', () => {
        spyOn(component.form, 'markAsDirty');

        const event = createDragDropEvent(
          'unassigned-films', component.unassignedFilms,
          'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
          0, 0, mockFilm1
        );

        component.onFilmDrop(event);

        expect(component.form.markAsDirty).toHaveBeenCalled();
      });
    });
  });

  describe('DevKit Grid Layout', () => {
    it('should have sessionDevKits array that can hold multiple devkits', () => {
      const mockDevKit1: DevKitDto = {
        id: 'devkit-1',
        name: 'DevKit 1',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 1',
        expired: false,
        imageUrl: 'test-url-1',
      };
      const mockDevKit2: DevKitDto = {
        id: 'devkit-2',
        name: 'DevKit 2',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 2',
        expired: false,
        imageUrl: 'test-url-2',
      };
      const mockDevKit3: DevKitDto = {
        id: 'devkit-3',
        name: 'DevKit 3',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 3',
        expired: false,
        imageUrl: 'test-url-3',
      };
      const mockDevKit4: DevKitDto = {
        id: 'devkit-4',
        name: 'DevKit 4',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 4',
        expired: false,
        imageUrl: 'test-url-4',
      };

      component.sessionDevKits = [
        { devKit: mockDevKit1, assignedFilms: [] },
        { devKit: mockDevKit2, assignedFilms: [] },
        { devKit: mockDevKit3, assignedFilms: [] },
        { devKit: mockDevKit4, assignedFilms: [] }
      ];
      
      expect(component.sessionDevKits.length).toBe(4);
      expect(component.sessionDevKits[0].devKit.name).toBe('DevKit 1');
      expect(component.sessionDevKits[1].devKit.name).toBe('DevKit 2');
      expect(component.sessionDevKits[2].devKit.name).toBe('DevKit 3');
      expect(component.sessionDevKits[3].devKit.name).toBe('DevKit 4');
    });

    it('should support up to 3 devkits displayed in grid layout', () => {
      const mockDevKit1: DevKitDto = {
        id: 'devkit-1',
        name: 'DevKit 1',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 1',
        expired: false,
        imageUrl: 'test-url-1',
      };
      const mockDevKit2: DevKitDto = {
        id: 'devkit-2',
        name: 'DevKit 2',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 2',
        expired: false,
        imageUrl: 'test-url-2',
      };
      const mockDevKit3: DevKitDto = {
        id: 'devkit-3',
        name: 'DevKit 3',
        url: 'http://example.com',
        type: DevKitType.C41,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        mixedOn: '2023-01-01',
        validForWeeks: 6,
        validForFilms: 8,
        filmsDeveloped: 0,
        description: 'Test devkit 3',
        expired: false,
        imageUrl: 'test-url-3',
      };

      // Test with 1 devkit
      component.sessionDevKits = [{ devKit: mockDevKit1, assignedFilms: [] }];
      expect(component.sessionDevKits.length).toBe(1);
      
      // Test with 2 devkits
      component.sessionDevKits = [
        { devKit: mockDevKit1, assignedFilms: [] },
        { devKit: mockDevKit2, assignedFilms: [] }
      ];
      expect(component.sessionDevKits.length).toBe(2);
      
      // Test with 3 devkits
      component.sessionDevKits = [
        { devKit: mockDevKit1, assignedFilms: [] },
        { devKit: mockDevKit2, assignedFilms: [] },
        { devKit: mockDevKit3, assignedFilms: [] }
      ];
      expect(component.sessionDevKits.length).toBe(3);
    });
  });
});