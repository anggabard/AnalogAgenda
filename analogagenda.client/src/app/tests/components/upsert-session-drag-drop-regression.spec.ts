import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { UpsertSessionComponent } from '../../components/sessions/upsert-session/upsert-session.component';
import { SessionService, DevKitService, FilmService } from '../../services';
import { DevKitDto, FilmDto } from '../../DTOs';
import { DevKitType, UsernameType, FilmType } from '../../enums';
import { TestConfig } from '../test.config';

/**
 * Regression tests specifically for the drag-and-drop bug where the wrong film
 * gets transferred when dragging films between "Developed Films" and dev kits.
 * 
 * Bug Description:
 * - When Film 1 is already in a dev kit and you try to drag Film 3 from 
 *   "Developed Films" to the same dev kit, Film 2 gets transferred instead.
 * - This happens because Angular was tracking items by array index instead
 *   of unique identifiers, causing DOM/data mismatches.
 * 
 * Fix Applied:
 * - Added trackBy functions to all *ngFor loops
 * - Added cdkDragData binding to drag elements
 * - Updated drag handler to use explicit drag data instead of array indices
 */
describe('UpsertSessionComponent - Drag Drop Bug Regression Tests', () => {
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

  describe('Original Bug Scenarios', () => {
    let film1: FilmDto;
    let film2: FilmDto;
    let film3: FilmDto;
    let devKit: DevKitDto;

    beforeEach(() => {
      // Create test films with unique identifiers
      film1 = createMockFilm('film-1', 'Film 1');
      film2 = createMockFilm('film-2', 'Film 2');
      film3 = createMockFilm('film-3', 'Film 3');

      devKit = createMockDevKit('devkit-1', 'Test DevKit');

      // Setup component state to match the bug scenario
      component.sessionDevKits = [{
        devKit: devKit,
        assignedFilms: [film1] // Film 1 already in dev kit
      }];
      component.unassignedFilms = [film2, film3]; // Films 2 and 3 in "Developed Films"
    });

    it('should fix the original bug: dragging Film 3 should transfer Film 3, not Film 2', () => {
      // This test reproduces the exact scenario from the bug report
      const event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        1, 1, film3
      );

      // Execute the drag operation
      component.onFilmDrop(event);

      // VERIFICATION: Film 3 should be transferred, NOT Film 2
      expect(component.sessionDevKits[0].assignedFilms).toContain(film3, 'Film 3 should be in dev kit');
      expect(component.sessionDevKits[0].assignedFilms).not.toContain(film2, 'Film 2 should NOT be in dev kit');
      expect(component.sessionDevKits[0].assignedFilms).toContain(film1, 'Film 1 should remain in dev kit');
      
      // Films in unassigned should be updated correctly
      expect(component.unassignedFilms).not.toContain(film3, 'Film 3 should be removed from unassigned');
      expect(component.unassignedFilms).toContain(film2, 'Film 2 should remain in unassigned');
    });

    it('should fix the reverse bug: dragging Film 2 from dev kit should transfer Film 2, not Film 1', () => {
      // Setup: Films 1 and 2 in dev kit, Film 3 in unassigned
      component.sessionDevKits[0].assignedFilms = [film1, film2];
      component.unassignedFilms = [film3];

      const event = createDragDropEvent(
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        'unassigned-films', component.unassignedFilms,
        1, 1, film2
      );

      // Execute the drag operation
      component.onFilmDrop(event);

      // VERIFICATION: Film 2 should be transferred back, NOT Film 1
      expect(component.unassignedFilms).toContain(film2, 'Film 2 should be in unassigned');
      expect(component.unassignedFilms).not.toContain(film1, 'Film 1 should NOT be in unassigned');
      expect(component.unassignedFilms).toContain(film3, 'Film 3 should remain in unassigned');
      
      // Films in dev kit should be updated correctly
      expect(component.sessionDevKits[0].assignedFilms).not.toContain(film2, 'Film 2 should be removed from dev kit');
      expect(component.sessionDevKits[0].assignedFilms).toContain(film1, 'Film 1 should remain in dev kit');
    });

    it('should handle multiple drag operations without index confusion', () => {
      // Test multiple drag operations to ensure indices don't get confused
      
      // First drag: Film 2 to dev kit
      let event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        0, 1, film2
      );
      component.onFilmDrop(event);

      // Verify first drag worked
      expect(component.sessionDevKits[0].assignedFilms).toContain(film2);
      expect(component.unassignedFilms).not.toContain(film2);
      expect(component.unassignedFilms).toContain(film3);

      // Second drag: Film 3 to dev kit (this was the problematic scenario)
      event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        0, 2, film3
      );
      component.onFilmDrop(event);

      // Verify second drag worked correctly
      expect(component.sessionDevKits[0].assignedFilms).toContain(film3, 'Film 3 should be in dev kit');
      expect(component.sessionDevKits[0].assignedFilms).toContain(film2, 'Film 2 should still be in dev kit');
      expect(component.sessionDevKits[0].assignedFilms).toContain(film1, 'Film 1 should still be in dev kit');
      expect(component.unassignedFilms).not.toContain(film3, 'Film 3 should be removed from unassigned');
      expect(component.unassignedFilms.length).toBe(0, 'Unassigned should be empty');
    });
  });

  describe('TrackBy Function Correctness', () => {
    it('should return unique identifiers for films', () => {
      const film1 = createMockFilm('film-1', 'Film 1');
      const film2 = createMockFilm('film-2', 'Film 2');

      expect(component.trackByFilmId(0, film1)).toBe('film-1');
      expect(component.trackByFilmId(1, film2)).toBe('film-2');
      expect(component.trackByFilmId(0, film1)).not.toBe(component.trackByFilmId(1, film2));
    });

    it('should return unique identifiers for dev kits', () => {
      const devKit1 = createMockDevKit('devkit-1', 'DevKit 1');
      const devKit2 = createMockDevKit('devkit-2', 'DevKit 2');

      expect(component.trackByDevKitDtoId(0, devKit1)).toBe('devkit-1');
      expect(component.trackByDevKitDtoId(1, devKit2)).toBe('devkit-2');
      expect(component.trackByDevKitDtoId(0, devKit1)).not.toBe(component.trackByDevKitDtoId(1, devKit2));
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('should handle drag operations with non-existent items gracefully', () => {
      const nonExistentFilm = createMockFilm('non-existent', 'Non-existent Film');
      
      // Ensure we have a dev kit with assigned films
      component.sessionDevKits = [{
        devKit: createMockDevKit('devkit-1', 'Test Dev Kit'),
        assignedFilms: []
      }];
      
      const event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        0, 0, nonExistentFilm
      );

      const initialUnassignedCount = component.unassignedFilms.length;
      const initialAssignedCount = component.sessionDevKits[0].assignedFilms.length;

      component.onFilmDrop(event);

      // Should not change anything if item not found
      expect(component.unassignedFilms.length).toBe(initialUnassignedCount);
      expect(component.sessionDevKits[0].assignedFilms.length).toBe(initialAssignedCount);
    });

    it('should not perform drag operations when not over container', () => {
      const film = createMockFilm('film-1', 'Film 1');
      component.unassignedFilms = [film];
      
      // Ensure we have a dev kit with assigned films
      component.sessionDevKits = [{
        devKit: createMockDevKit('devkit-1', 'Test Dev Kit'),
        assignedFilms: []
      }];

      const event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        0, 0, film, false
      );

      const initialUnassignedCount = component.unassignedFilms.length;
      const initialAssignedCount = component.sessionDevKits[0].assignedFilms.length;

      component.onFilmDrop(event);

      expect(component.unassignedFilms.length).toBe(initialUnassignedCount);
      expect(component.sessionDevKits[0].assignedFilms.length).toBe(initialAssignedCount);
    });
  });

  describe('Form State Management', () => {
    it('should mark form as dirty when films are moved', () => {
      spyOn(component.form, 'markAsDirty');
      
      const film = createMockFilm('film-1', 'Film 1');
      component.unassignedFilms = [film];
      
      // Ensure we have a dev kit with assigned films
      component.sessionDevKits = [{
        devKit: createMockDevKit('devkit-1', 'Test Dev Kit'),
        assignedFilms: []
      }];

      const event = createDragDropEvent(
        'unassigned-films', component.unassignedFilms,
        'devkit-devkit-1', component.sessionDevKits[0].assignedFilms,
        0, 0, film
      );

      component.onFilmDrop(event);

      expect(component.form.markAsDirty).toHaveBeenCalled();
    });
  });

  // Helper functions for creating test data
  function createMockFilm(id: string, name: string): FilmDto {
    return {
      id,
      name,
      iso: '400',
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 10,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      imageUrl: `${name.toLowerCase().replace(' ', '')}.jpg`,
      description: `Test ${name}`,
      developed: true,
      developedInSessionId: 'session-1'
    };
  }

  function createMockDevKit(id: string, name: string): DevKitDto {
    return {
      id,
      name,
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: `Test ${name}`,
      expired: false,
      imageUrl: `${name.toLowerCase().replace(' ', '')}.jpg`
    };
  }
});
