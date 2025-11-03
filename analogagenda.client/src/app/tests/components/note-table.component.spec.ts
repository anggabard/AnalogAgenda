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
    expect(component.noteId).toBeNull();
  });

  it('should initialize in view mode and load note when ID is provided', () => {
    // Arrange
    const testid = 'test-row-key';
    const mockNote: NoteDto = {
      id: testRowKey,
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
    expect(component.noteId).toBe(testRowKey);
    expect(mockNotesService.getById).toHaveBeenCalledWith(testRowKey);
    expect(component.note).toEqual(mockNote);
    expect(component.originalNote).toEqual(mockNote);
  });

  it('should handle error when loading note from backend', () => {
    // Arrange
    spyOn(console, 'error');
    const testid = 'test-row-key';
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
    const newid = 'new-row-key';
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
    component.noteid = 'existing-key';
    component.note = { 
      id: 'existing-key', 
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
        { id: '1', noteid: '', time: 10, process: 'Step 1', film: '', details: '' }
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
        { id: '1', noteid: '', time: 0, process: 'Step 1', film: '', details: '' },
        { id: '2', noteid: '', time: 10, process: 'Step 2', film: '', details: '' }
      ]
    };

    // Act
    component.removeRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(1);
    expect(component.note.entries[0].process).toBe('Step 2');
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
        { id: '1', noteid: '', time: 0, process: 'Step 1', film: '', details: '' }
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
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: 'original-key', noteid: '', time: 5, process: 'Original Step', film: 'Film1', details: 'Details1' }
      ]
    };

    // Act
    component.copyRow(0);

    // Assert
    expect(component.note.entries).toHaveSize(2);
    expect(component.note.entries[1].id).toBe(''); // Should be empty for copied row
    expect(component.note.entries[1].process).toBe('Original Step');
    expect(component.note.entries[1].film).toBe('Film1');
  });

  it('should validate time change - reject lower than previous', () => {
    // Arrange
    spyOn(window, 'alert');
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteid: '', time: 10, process: 'Step 1', film: '', details: '' },
        { id: '2', noteid: '', time: 20, process: 'Step 2', film: '', details: '' }
      ]
    };

    // Act
    component.onTimeChange(1, 5); // Try to set time lower than previous

    // Assert
    expect(window.alert).toHaveBeenCalledWith('Time cannot be lower than the previous step!');
    expect(component.note.entries[1].time).toBe(10); // Should be set to previous time
  });

  it('should validate time change - reject higher than next', () => {
    // Arrange
    spyOn(window, 'alert');
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteid: '', time: 10, process: 'Step 1', film: '', details: '' },
        { id: '2', noteid: '', time: 20, process: 'Step 2', film: '', details: '' },
        { id: '3', noteid: '', time: 30, process: 'Step 3', film: '', details: '' }
      ]
    };

    // Act
    component.onTimeChange(1, 35); // Try to set time higher than next

    // Assert
    expect(window.alert).toHaveBeenCalledWith('Time cannot be higher than the next step!');
    expect(component.note.entries[1].time).toBe(30); // Should be set to next time
  });

  it('should accept valid time change', () => {
    // Arrange
    component.note = {
      id: '',
      name: '',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        { id: '1', noteid: '', time: 10, process: 'Step 1', film: '', details: '' },
        { id: '2', noteid: '', time: 20, process: 'Step 2', film: '', details: '' }
      ]
    };

    // Act
    component.onTimeChange(1, 15); // Valid time between 10 and infinity

    // Assert
    expect(component.note.entries[1].time).toBe(15);
  });

  it('should delete note and navigate to notes list', () => {
    // Arrange
    component.note = { 
      id: 'test-key', 
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
      id: 'test-key', 
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
});