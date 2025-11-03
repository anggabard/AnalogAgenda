import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoteTableComponent } from '../../components/notes/note-table/note-table.component';
import { NotesService } from '../../services';
import { NoteDto, NoteEntryDto } from '../../DTOs';

describe('NoteTableComponent', () => {
  let component: NoteTableComponent;
  let fixture: ComponentFixture<NoteTableComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const notesServiceSpy = jasmine.createSpyObj('NotesService', ['getById', 'addNewNote', 'update', 'deleteById']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get')
        }
      }
    };

    await TestBed.configureTestingModule({
      declarations: [NoteTableComponent],
      providers: [
        { provide: NotesService, useValue: notesServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NoteTableComponent);
    component = fixture.componentInstance;
    mockNotesService = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize in create mode when no ID is provided', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);

    // Act
    component.ngOnInit();

    // Assert
    expect(component.isNewNote).toBeTrue();
    expect(component.isEditMode).toBeTrue();
    expect(component.noteId).toBeNull();
  });

  it('should initialize in view mode and load note when ID is provided', () => {
    // Arrange
    const testId = 'test-id';
    const mockNote: NoteDto = {
      id: testId,
      name: 'Test Note',
      sideNote: 'Test Side Note',
      imageUrl: 'test-url',
      imageBase64: '',
      entries: []
    };

    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testId);
    mockNotesService.getById.and.returnValue(of(mockNote));

    // Act
    component.ngOnInit();

    // Assert
    expect(component.noteId).toBe(testId);
    expect(mockNotesService.getById).toHaveBeenCalledWith(testId);
    expect(component.note).toEqual(mockNote);
    expect(component.originalNote).toEqual(mockNote);
  });

  it('should handle error when loading note from backend', () => {
    // Arrange
    spyOn(console, 'error');
    const testId = 'test-id';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testId);
    mockNotesService.getById.and.returnValue(throwError(() => 'Load error'));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getById).toHaveBeenCalledWith(testId);
    expect(console.error).toHaveBeenCalledWith('Load error');
  });

  it('should return empty note with correct structure', () => {
    // Act
    const emptyNote = component.getEmptyNote();

    // Assert
    expect(emptyNote.id).toBe('');
    expect(emptyNote.name).toBe('');
    expect(emptyNote.sideNote).toBe('');
    expect(emptyNote.imageBase64).toBe('');
    expect(emptyNote.imageUrl).toBe('');
    expect(emptyNote.entries).toHaveSize(1);
    expect(emptyNote.entries[0].time).toBe(0);
  });

  it('should toggle edit mode', () => {
    // Arrange
    component.isEditMode = false;

    // Act
    component.toggleEditMode();

    // Assert
    expect(component.isEditMode).toBeTrue();
  });

  it('should discard changes for new note and navigate to notes', () => {
    // Arrange
    component.isNewNote = true;
    component.isEditMode = true;

    // Act
    component.discardChanges();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes']);
    expect(component.isEditMode).toBeFalse();
  });

  it('should discard changes for existing note and restore original', () => {
    // Arrange
    const originalNote: NoteDto = { 
      id: '1', 
      name: 'Original', 
      sideNote: 'Original side note', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    component.isNewNote = false;
    component.originalNote = originalNote;
    component.note = { ...originalNote, name: 'Modified' };
    component.isEditMode = true;

    // Act
    component.discardChanges();

    // Assert
    expect(component.note.name).toBe('Original');
    expect(component.isEditMode).toBeFalse();
  });

  it('should save new note and navigate to note details', () => {
    // Arrange
    component.isNewNote = true;
    component.note = { 
      id: '', 
      name: '', 
      sideNote: 'Test note', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    const newId = 'new-id';
    mockNotesService.addNewNote.and.returnValue(of(newId));

    // Act
    component.saveNote();

    // Assert
    expect(component.note.name).toBe('Untitled Note'); // Should set default name
    expect(mockNotesService.addNewNote).toHaveBeenCalledWith(component.note);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes/' + newId]);
  });

  it('should update existing note', () => {
    // Arrange
    component.isNewNote = false;
    component.noteId = 'existing-id';
    component.note = { 
      id: 'existing-id', 
      name: 'Updated Note', 
      sideNote: 'Updated side note', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    mockNotesService.update.and.returnValue(of({}));

    // Act
    component.saveNote();

    // Assert
    expect(mockNotesService.update).toHaveBeenCalledWith('existing-id', component.note);
    expect(component.originalNote).toEqual(component.note);
    expect(component.isEditMode).toBeFalse();
  });

  it('should handle save error', () => {
    // Arrange
    spyOn(console, 'error');
    component.isNewNote = true;
    component.note = { 
      id: '', 
      name: 'Test', 
      sideNote: '', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    mockNotesService.addNewNote.and.returnValue(throwError(() => 'Save error'));

    // Act
    component.saveNote();

    // Assert
    expect(console.error).toHaveBeenCalledWith('Save error');
  });

  it('should add new row with correct time', () => {
    // Arrange
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteId: '', time: 10, step: 'Step 1', details: '', index: 0, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] }
      ]
    };

    // Act
    component.addRow();

    // Assert
    expect(component.note.entries).toHaveSize(2);
    expect(component.note.entries[1].time).toBe(10); // Should use last entry's time
    expect(component.note.entries[1].id).toBe('');
  });

  it('should remove row when more than one entry exists', () => {
    // Arrange
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteId: '', time: 0, step: 'Step 1', details: '', index: 0, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] },
        { id: '2', noteId: '', time: 10, step: 'Step 2', details: '', index: 1, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] }
      ]
    };

    // Act
    component.removeRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(1);
    expect(component.note.entries[0].step).toBe('Step 2');
  });

  it('should not remove row when only one entry exists', () => {
    // Arrange
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteId: '', time: 0, step: 'Step 1', details: '', index: 0, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] }
      ]
    };

    // Act
    component.removeRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(1);
  });

  it('should copy row with empty id', () => {
    // Arrange
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: 'original-key', noteId: '', time: 5, step: 'Original Step', details: 'Details1', index: 0, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] }
      ]
    };

    // Act
    component.copyRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(2);
    expect(component.note.entries[1].id).toBe(''); // Should be empty for copied row
    expect(component.note.entries[1].step).toBe('Original Step');
    expect(component.note.entries[1].details).toBe('Details1');
  });

  it('should delete note and navigate to notes list', () => {
    // Arrange
    component.note = { 
      id: 'test-id', 
      name: 'Test Note', 
      sideNote: '', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    mockNotesService.deleteById.and.returnValue(of({}));

    // Act
    component.onDelete();

    // Assert
    expect(mockNotesService.deleteById).toHaveBeenCalledWith('test-id');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes']);
  });

  it('should handle delete error', () => {
    // Arrange
    spyOn(console, 'error');
    component.note = { 
      id: 'test-id', 
      name: 'Test Note', 
      sideNote: '', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    mockNotesService.deleteById.and.returnValue(throwError(() => 'Delete error'));

    // Act
    component.onDelete();

    // Assert
    expect(console.error).toHaveBeenCalledWith('Delete error');
  });

  describe('Validation Methods', () => {
    beforeEach(() => {
      component.isEditMode = true;
    });

    it('should validate duration - invalid when time is undefined', () => {
      const entry: any = { time: undefined };
      expect(component.isDurationInvalid(entry)).toBeTrue();
    });

    it('should validate duration - invalid when time is null', () => {
      const entry: any = { time: null };
      expect(component.isDurationInvalid(entry)).toBeTrue();
    });

    it('should validate duration - invalid when time is 0', () => {
      const entry: any = { time: 0 };
      expect(component.isDurationInvalid(entry)).toBeTrue();
    });

    it('should validate duration - invalid when time is negative', () => {
      const entry: any = { time: -1 };
      expect(component.isDurationInvalid(entry)).toBeTrue();
    });

    it('should validate duration - valid when time is positive', () => {
      const entry: any = { time: 5 };
      expect(component.isDurationInvalid(entry)).toBeFalse();
    });

    it('should validate override time - invalid when time is 0', () => {
      const override: any = { time: 0 };
      expect(component.isOverrideTimeInvalid(override)).toBeTrue();
    });

    it('should validate override time - invalid when time is negative', () => {
      const override: any = { time: -1 };
      expect(component.isOverrideTimeInvalid(override)).toBeTrue();
    });

    it('should validate override time - valid when time is positive', () => {
      const override: any = { time: 2.5 };
      expect(component.isOverrideTimeInvalid(override)).toBeFalse();
    });

    it('should validate override range order - invalid when max < min', () => {
      const override: any = { filmCountMin: 10, filmCountMax: 5 };
      expect(component.isOverrideRangeOrderInvalid(override)).toBeTrue();
    });

    it('should validate override range order - valid when max >= min', () => {
      const override: any = { filmCountMin: 5, filmCountMax: 10 };
      expect(component.isOverrideRangeOrderInvalid(override)).toBeFalse();
    });

    it('should validate override range order - valid when max == min', () => {
      const override: any = { filmCountMin: 5, filmCountMax: 5 };
      expect(component.isOverrideRangeOrderInvalid(override)).toBeFalse();
    });
  });

  describe('Override Management', () => {
    it('should add override with default time increment', () => {
      const entry: any = {
        id: 'entry1',
        time: 3.0,
        overrides: []
      };

      component.addOverride(entry);

      expect(entry.overrides.length).toBe(1);
      expect(entry.overrides[0].time).toBe(3.25); // base + 0.25 (15 seconds)
      expect(entry.overrides[0].filmCountMin).toBe(1);
      expect(entry.overrides[0].filmCountMax).toBe(1);
    });

    it('should add override based on previous override time', () => {
      const entry: any = {
        id: 'entry1',
        time: 3.0,
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 }
        ]
      };

      component.addOverride(entry);

      expect(entry.overrides.length).toBe(2);
      expect(entry.overrides[1].time).toBe(4.25); // previous override + 0.25
      expect(entry.overrides[1].filmCountMin).toBe(6);
    });

    it('should remove override at specified index', () => {
      const entry: any = {
        id: 'entry1',
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 }
        ]
      };

      component.removeOverride(entry, 0);

      expect(entry.overrides.length).toBe(1);
      expect(entry.overrides[0].filmCountMin).toBe(6);
    });

    it('should toggle override expansion', () => {
      const entry: NoteEntryDto = {
        id: 'test-key',
        noteId: '',
        time: 5,
        step: 'Test',
        details: '',
        index: 0,
        temperatureMin: 20,
        temperatureMax: undefined,
        rules: [],
        overrides: []
      };
      const index = 0;
      
      component.toggleOverrideExpansion(entry, index);
      expect(component.isOverrideExpanded(entry, index)).toBeTrue();
      
      component.toggleOverrideExpansion(entry, index);
      expect(component.isOverrideExpanded(entry, index)).toBeFalse();
    });
  });

  describe('Rule Management', () => {
    it('should initialize newRule with default values', () => {
      expect(component.newRule.filmInterval).toBe(1);
      expect(component.newRule.timeIncrement).toBe(0.5);
    });

    it('should open add rule modal with reset values', () => {
      component.openAddRuleModal();

      expect(component.isAddRuleModalOpen).toBeTrue();
      expect(component.selectedStepsForRule).toEqual([]);
      expect(component.newRule.filmInterval).toBe(1);
      expect(component.newRule.timeIncrement).toBe(0.5);
    });

    it('should close add rule modal', () => {
      component.isAddRuleModalOpen = true;
      component.closeAddRuleModal();
      expect(component.isAddRuleModalOpen).toBeFalse();
    });

    it('should add rule to selected steps', () => {
      const entry1: any = { id: '1', step: 'DEV', rules: [] };
      const entry2: any = { id: '2', step: 'BLEACH', rules: [] };
      
      component.selectedStepsForRule = [entry1, entry2];
      component.newRule = {
        id: '',
        noteEntryId: '',
        filmInterval: 3,
        timeIncrement: 0.5
      };

      component.addRuleToSelectedSteps();

      expect(entry1.rules.length).toBe(1);
      expect(entry2.rules.length).toBe(1);
      expect(entry1.rules[0].filmInterval).toBe(3);
      expect(entry1.rules[0].timeIncrement).toBe(0.5);
      expect(component.isAddRuleModalOpen).toBeFalse();
    });

    it('should delete rule from entry', () => {
      const rule: any = { id: 'rule-1', filmInterval: 3, timeIncrement: 0.5 };
      const entry: any = {
        id: 'entry-1',
        rules: [rule]
      };
      component.note = {
        id: '',
        name: '',
        sideNote: '',
        imageUrl: '',
        imageBase64: '',
        entries: [entry]
      };

      component.deleteRule(rule);

      expect(entry.rules.length).toBe(0);
    });

    it('should group rules by filmInterval and timeIncrement', () => {
      const entry1: any = {
        step: 'DEV',
        rules: [{ filmInterval: 3, timeIncrement: 0.5 }]
      };
      const entry2: any = {
        step: 'BLEACH',
        rules: [{ filmInterval: 3, timeIncrement: 0.5 }]
      };
      component.note = {
        id: '',
        name: '',
        sideNote: '',
        imageUrl: '',
        imageBase64: '',
        entries: [entry1, entry2]
      };

      const grouped = component.getAllRules();

      expect(grouped.length).toBe(1);
      expect(grouped[0].steps).toContain('DEV');
      expect(grouped[0].steps).toContain('BLEACH');
    });
  });

  describe('Time Formatting', () => {
    it('should format time for display', () => {
      expect(component.formatTimeForDisplay(1.5)).toBe('1m 30s');
      expect(component.formatTimeForDisplay(0.25)).toBe('0m 15s');
      expect(component.formatTimeForDisplay(3.75)).toBe('3m 45s');
    });

    it('should calculate accumulated start time', () => {
      component.note = {
        id: '',
        name: '',
        sideNote: '',
        imageUrl: '',
        imageBase64: '',
        entries: [
          { id: '1', noteId: '', time: 3, step: 'PRESOAK', details: '', index: 0, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] },
          { id: '2', noteId: '', time: 5, step: 'DEV', details: '', index: 1, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] },
          { id: '3', noteId: '', time: 1, step: 'BLEACH', details: '', index: 2, temperatureMin: 38, temperatureMax: undefined, rules: [], overrides: [] }
        ]
      };

      expect(component.getAccumulatedStartTime(0)).toBe(0);
      expect(component.getAccumulatedStartTime(1)).toBe(3);
      expect(component.getAccumulatedStartTime(2)).toBe(8);
    });
  });

  describe('Film Count Management', () => {
    it('should increment film count', () => {
      component.filmCount = 5;
      component.incrementFilmCount();
      expect(component.filmCount).toBe(6);
    });

    it('should not increment film count beyond 100', () => {
      component.filmCount = 100;
      component.incrementFilmCount();
      expect(component.filmCount).toBe(100);
    });

    it('should decrement film count', () => {
      component.filmCount = 5;
      component.decrementFilmCount();
      expect(component.filmCount).toBe(4);
    });

    it('should not decrement film count below 1', () => {
      component.filmCount = 1;
      component.decrementFilmCount();
      expect(component.filmCount).toBe(1);
    });
  });

  describe('Rule Calculation Logic', () => {
    beforeEach(() => {
      component.filmCount = 1;
    });

    it('should calculate time with rule - every 3 films, films 1-3 get base time only', () => {
      const entry: any = {
        time: 5.0,
        rules: [{ filmInterval: 3, timeIncrement: 1.0 }],
        overrides: []
      };

      component.filmCount = 1;
      expect(component.getEffectiveTime(entry)).toBe(5.0); // base only

      component.filmCount = 2;
      expect(component.getEffectiveTime(entry)).toBe(5.0); // base only

      component.filmCount = 3;
      expect(component.getEffectiveTime(entry)).toBe(5.0); // base only
    });

    it('should calculate time with rule - every 3 films, films 4-6 get +1 increment', () => {
      const entry: any = {
        time: 5.0,
        rules: [{ filmInterval: 3, timeIncrement: 1.0 }],
        overrides: []
      };

      component.filmCount = 4;
      expect(component.getEffectiveTime(entry)).toBe(6.0); // base + 1 increment

      component.filmCount = 5;
      expect(component.getEffectiveTime(entry)).toBe(6.0); // base + 1 increment

      component.filmCount = 6;
      expect(component.getEffectiveTime(entry)).toBe(6.0); // base + 1 increment
    });

    it('should calculate time with rule - every 3 films, films 7-9 get +2 increments', () => {
      const entry: any = {
        time: 5.0,
        rules: [{ filmInterval: 3, timeIncrement: 1.0 }],
        overrides: []
      };

      component.filmCount = 7;
      expect(component.getEffectiveTime(entry)).toBe(7.0); // base + 2 increments

      component.filmCount = 8;
      expect(component.getEffectiveTime(entry)).toBe(7.0); // base + 2 increments

      component.filmCount = 9;
      expect(component.getEffectiveTime(entry)).toBe(7.0); // base + 2 increments
    });

    it('should calculate time with rule - every 3 films, films 10-12 get +3 increments', () => {
      const entry: any = {
        time: 5.0,
        rules: [{ filmInterval: 3, timeIncrement: 1.0 }],
        overrides: []
      };

      component.filmCount = 10;
      expect(component.getEffectiveTime(entry)).toBe(8.0); // base + 3 increments

      component.filmCount = 11;
      expect(component.getEffectiveTime(entry)).toBe(8.0); // base + 3 increments

      component.filmCount = 12;
      expect(component.getEffectiveTime(entry)).toBe(8.0); // base + 3 increments
    });

    it('should calculate time with rule - every 2 films', () => {
      const entry: any = {
        time: 3.0,
        rules: [{ filmInterval: 2, timeIncrement: 0.5 }],
        overrides: []
      };

      component.filmCount = 1;
      expect(component.getEffectiveTime(entry)).toBe(3.0); // base only

      component.filmCount = 2;
      expect(component.getEffectiveTime(entry)).toBe(3.0); // base only

      component.filmCount = 3;
      expect(component.getEffectiveTime(entry)).toBe(3.5); // base + 1 increment

      component.filmCount = 4;
      expect(component.getEffectiveTime(entry)).toBe(3.5); // base + 1 increment

      component.filmCount = 5;
      expect(component.getEffectiveTime(entry)).toBe(4.0); // base + 2 increments

      component.filmCount = 6;
      expect(component.getEffectiveTime(entry)).toBe(4.0); // base + 2 increments
    });

    it('should prioritize override over rule when both exist', () => {
      const entry: any = {
        time: 5.0,
        rules: [{ filmInterval: 3, timeIncrement: 1.0 }],
        overrides: [
          { filmCountMin: 4, filmCountMax: 6, time: 10.0 }
        ]
      };

      component.filmCount = 5; // Falls in override range
      expect(component.getEffectiveTime(entry)).toBe(10.0); // Should use override, not rule
    });
  });

  describe('Override Selection Logic', () => {
    it('should return matching override when film count is within range', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 3.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 }
        ]
      };

      component.filmCount = 3;
      const override = component.getApplicableOverride(entry);
      expect(override?.time).toBe(3.0);

      component.filmCount = 8;
      const override2 = component.getApplicableOverride(entry);
      expect(override2?.time).toBe(5.0);
    });

    it('should return last override that ended before current film count when outside all ranges', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 4, filmCountMax: 6, time: 3.0 },
          { filmCountMin: 8, filmCountMax: 10, time: 5.0 }
        ]
      };

      component.filmCount = 7; // Between 6 and 8
      const override = component.getApplicableOverride(entry);
      expect(override?.time).toBe(3.0); // Should use override 1 (4-6), the last that ended before 7

      component.filmCount = 11; // After all ranges
      const override2 = component.getApplicableOverride(entry);
      expect(override2?.time).toBe(5.0); // Should use override 2 (8-10), the last that ended before 11
    });

    it('should return null when film count is less than all override mins', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 4, filmCountMax: 6, time: 3.0 },
          { filmCountMin: 8, filmCountMax: 10, time: 5.0 }
        ]
      };

      component.filmCount = 1; // Before all ranges
      const override = component.getApplicableOverride(entry);
      expect(override).toBeNull(); // Should return null, use base time
    });

    it('should return null when no overrides exist', () => {
      const entry: any = {
        overrides: []
      };

      component.filmCount = 5;
      const override = component.getApplicableOverride(entry);
      expect(override).toBeNull();
    });

    it('should handle multiple overrides with gaps correctly', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 3, time: 2.0 },
          { filmCountMin: 7, filmCountMax: 9, time: 4.0 },
          { filmCountMin: 15, filmCountMax: 20, time: 6.0 }
        ]
      };

      component.filmCount = 5; // Between 3 and 7
      const override1 = component.getApplicableOverride(entry);
      expect(override1?.time).toBe(2.0); // Should use 1-3 range

      component.filmCount = 12; // Between 9 and 15
      const override2 = component.getApplicableOverride(entry);
      expect(override2?.time).toBe(4.0); // Should use 7-9 range

      component.filmCount = 25; // After all ranges
      const override3 = component.getApplicableOverride(entry);
      expect(override3?.time).toBe(6.0); // Should use 15-20 range
    });
  });

  describe('Override filmCountMin Auto-calculation', () => {
    it('should set first override min to 1 when no overrides exist', () => {
      const entry: any = {
        id: 'entry1',
        time: 3.0,
        overrides: []
      };

      component.addOverride(entry);

      expect(entry.overrides.length).toBe(1);
      expect(entry.overrides[0].filmCountMin).toBe(1);
    });

    it('should set new override min to previous override max + 1', () => {
      const entry: any = {
        id: 'entry1',
        time: 3.0,
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 }
        ]
      };

      component.addOverride(entry);

      expect(entry.overrides.length).toBe(2);
      expect(entry.overrides[1].filmCountMin).toBe(6); // 5 + 1
    });

    it('should handle multiple overrides in sequence', () => {
      const entry: any = {
        id: 'entry1',
        time: 3.0,
        overrides: [
          { filmCountMin: 1, filmCountMax: 3, time: 4.0 },
          { filmCountMin: 4, filmCountMax: 6, time: 5.0 }
        ]
      };

      component.addOverride(entry);

      expect(entry.overrides.length).toBe(3);
      expect(entry.overrides[2].filmCountMin).toBe(7); // 6 + 1
    });
  });

  describe('First Override Detection', () => {
    it('should identify first override correctly', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 },
          { filmCountMin: 11, filmCountMax: 15, time: 6.0 }
        ]
      };

      expect(component.isFirstOverride(entry, 0)).toBeTrue();
      expect(component.isFirstOverride(entry, 1)).toBeFalse();
      expect(component.isFirstOverride(entry, 2)).toBeFalse();
    });

    it('should handle overrides added in different order', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 11, filmCountMax: 15, time: 6.0 }, // Added last but has lowest min
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 },
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 } // Added last but has lowest min
        ]
      };

      // The first override is determined by lowest filmCountMin, not array order
      expect(component.isFirstOverride(entry, 2)).toBeTrue(); // Index 2 has min 1
      expect(component.isFirstOverride(entry, 0)).toBeFalse();
      expect(component.isFirstOverride(entry, 1)).toBeFalse();
    });

    it('should return false for invalid indices', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 }
        ]
      };

      expect(component.isFirstOverride(entry, -1)).toBeFalse();
      expect(component.isFirstOverride(entry, 1)).toBeFalse();
      expect(component.isFirstOverride(entry, 10)).toBeFalse();
    });

    it('should return false when no overrides exist', () => {
      const entry: any = {
        overrides: []
      };

      expect(component.isFirstOverride(entry, 0)).toBeFalse();
    });
  });

  describe('Update Subsequent Override Mins', () => {
    it('should update subsequent override mins when first override max changes', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 },
          { filmCountMin: 11, filmCountMax: 15, time: 6.0 }
        ]
      };

      // Change first override max from 5 to 7
      entry.overrides[0].filmCountMax = 7;
      component.updateSubsequentOverrideMins(entry, 0);

      // Second override min should update to 8 (7 + 1)
      expect(entry.overrides[1].filmCountMin).toBe(8);
      // Third override min should update to 11 (10 + 1)
      expect(entry.overrides[2].filmCountMin).toBe(11);
    });

    it('should update subsequent override mins when middle override max changes', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 },
          { filmCountMin: 11, filmCountMax: 15, time: 6.0 }
        ]
      };

      // Change second override max from 10 to 12
      entry.overrides[1].filmCountMax = 12;
      component.updateSubsequentOverrideMins(entry, 1);

      // First override should remain unchanged
      expect(entry.overrides[0].filmCountMin).toBe(1);
      // Third override min should update to 13 (12 + 1)
      expect(entry.overrides[2].filmCountMin).toBe(13);
    });

    it('should not update anything when last override max changes', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 },
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 }
        ]
      };

      const originalMin1 = entry.overrides[0].filmCountMin;
      const originalMin2 = entry.overrides[1].filmCountMin;

      // Change last override max
      entry.overrides[1].filmCountMax = 15;
      component.updateSubsequentOverrideMins(entry, 1);

      // Nothing should change
      expect(entry.overrides[0].filmCountMin).toBe(originalMin1);
      expect(entry.overrides[1].filmCountMin).toBe(originalMin2);
    });

    it('should handle overrides in different order', () => {
      const entry: any = {
        overrides: [
          { filmCountMin: 11, filmCountMax: 15, time: 6.0 }, // Added last
          { filmCountMin: 1, filmCountMax: 5, time: 4.0 }, // Added first
          { filmCountMin: 6, filmCountMax: 10, time: 5.0 } // Added second
        ]
      };

      // Change the first override (by min value, index 1) max from 5 to 7
      entry.overrides[1].filmCountMax = 7;
      component.updateSubsequentOverrideMins(entry, 1);

      // After sorting by min: [index1 (min1), index2 (min6), index0 (min11)]
      // Update index1 max to 7:
      // - Next in sorted order is index2 (min6): should update to 8 (7 + 1)
      // - Then index0 (min11): should update to 11 (10 + 1, using index2's max)
      expect(entry.overrides[2].filmCountMin).toBe(8);
      expect(entry.overrides[0].filmCountMin).toBe(11);
    });
  });
});

