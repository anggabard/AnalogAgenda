import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoteTableComponent } from '../../components/notes/note-table/note-table.component';
import { NotesService } from '../../services';
import { NoteDto } from '../../DTOs';

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
    expect(component.noteRowKey).toBeNull();
  });

  it('should initialize in view mode and load note when ID is provided', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    const mockNote: NoteDto = {
      rowKey: testRowKey,
      name: 'Test Note',
      sideNote: 'Test Side Note',
      imageUrl: 'test-url',
      imageBase64: '',
      entries: []
    };

    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockNotesService.getById.and.returnValue(of(mockNote));

    // Act
    component.ngOnInit();

    // Assert
    expect(component.noteRowKey).toBe(testRowKey);
    expect(mockNotesService.getById).toHaveBeenCalledWith(testRowKey);
    expect(component.note).toEqual(mockNote);
    expect(component.originalNote).toEqual(mockNote);
  });

  it('should handle error when loading note from backend', () => {
    // Arrange
    spyOn(console, 'error');
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockNotesService.getById.and.returnValue(throwError(() => 'Load error'));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getById).toHaveBeenCalledWith(testRowKey);
    expect(console.error).toHaveBeenCalledWith('Load error');
  });

  it('should return empty note with correct structure', () => {
    // Act
    const emptyNote = component.getEmptyNote();

    // Assert
    expect(emptyNote.rowKey).toBe('');
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
      rowKey: '1', 
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
      rowKey: '', 
      name: '', 
      sideNote: 'Test note', 
      imageUrl: '', 
      imageBase64: '', 
      entries: [] 
    };
    const newRowKey = 'new-row-key';
    mockNotesService.addNewNote.and.returnValue(of(newRowKey));

    // Act
    component.saveNote();

    // Assert
    expect(component.note.name).toBe('Untitled Note'); // Should set default name
    expect(mockNotesService.addNewNote).toHaveBeenCalledWith(component.note);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes/' + newRowKey]);
  });

  it('should update existing note', () => {
    // Arrange
    component.isNewNote = false;
    component.noteRowKey = 'existing-key';
    component.note = { 
      rowKey: 'existing-key', 
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
    expect(mockNotesService.update).toHaveBeenCalledWith('existing-key', component.note);
    expect(component.originalNote).toEqual(component.note);
    expect(component.isEditMode).toBeFalse();
  });

  it('should handle save error', () => {
    // Arrange
    spyOn(console, 'error');
    component.isNewNote = true;
    component.note = { 
      rowKey: '', 
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
      rowKey: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { rowKey: '1', noteRowKey: '', time: 10, step: 'Step 1', details: '', overrides: [], rules: [] } as any
      ]
    };

    // Act
    component.addRow();

    // Assert
    expect(component.note.entries).toHaveSize(2);
    expect(component.note.entries[1].time).toBe(10); // Should use last entry's time
    expect(component.note.entries[1].rowKey).toBe('');
  });

  it('should remove row when more than one entry exists', () => {
    // Arrange
    component.note = {
      rowKey: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { rowKey: '1', noteRowKey: '', time: 0, step: 'Step 1', details: '', overrides: [], rules: [] } as any,
        { rowKey: '2', noteRowKey: '', time: 10, step: 'Step 2', details: '', overrides: [], rules: [] } as any
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
      rowKey: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { rowKey: '1', noteRowKey: '', time: 0, step: 'Step 1', details: '', overrides: [], rules: [] } as any
      ]
    };

    // Act
    component.removeRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(1);
  });

  it('should copy row with empty rowKey', () => {
    // Arrange
    component.note = {
      rowKey: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { rowKey: 'original-key', noteRowKey: '', time: 5, step: 'Original Step', details: 'Details1', overrides: [], rules: [] } as any
      ]
    };

    // Act
    component.copyRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(2);
    expect(component.note.entries[1].rowKey).toBe(''); // Should be empty for copied row
    expect(component.note.entries[1].step).toBe('Original Step');
    expect(component.note.entries[1].details).toBe('Details1');
  });


  it('should delete note and navigate to notes list', () => {
    // Arrange
    component.note = { 
      rowKey: 'test-key', 
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
    expect(mockNotesService.deleteById).toHaveBeenCalledWith('test-key');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes']);
  });

  it('should handle delete error', () => {
    // Arrange
    spyOn(console, 'error');
    component.note = { 
      rowKey: 'test-key', 
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
      const rowKey = 'test-key';
      
      component.toggleOverrideExpansion(rowKey);
      expect(component.isOverrideExpanded(rowKey)).toBeTrue();
      
      component.toggleOverrideExpansion(rowKey);
      expect(component.isOverrideExpanded(rowKey)).toBeFalse();
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
      const entry1: any = { rowKey: '1', step: 'DEV', rules: [] };
      const entry2: any = { rowKey: '2', step: 'BLEACH', rules: [] };
      
      component.selectedStepsForRule = [entry1, entry2];
      component.newRule = {
        rowKey: '',
        noteEntryRowKey: '',
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
      const rule: any = { rowKey: 'rule-1', filmInterval: 3, timeIncrement: 0.5 };
      const entry: any = {
        rowKey: 'entry-1',
        rules: [rule]
      };
      component.note = {
        rowKey: '',
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
        rowKey: '',
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
        rowKey: '',
        name: '',
        sideNote: '',
        imageUrl: '',
        imageBase64: '',
        entries: [
          { rowKey: '1', noteRowKey: '', time: 3, step: 'PRESOAK', overrides: [], rules: [] } as any,
          { rowKey: '2', noteRowKey: '', time: 5, step: 'DEV', overrides: [], rules: [] } as any,
          { rowKey: '3', noteRowKey: '', time: 1, step: 'BLEACH', overrides: [], rules: [] } as any
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
});