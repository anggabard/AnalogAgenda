import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { NotesComponent } from '../../components/notes/notes.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { NotesService } from '../../services';
import { NoteDto, PagedResponseDto } from '../../DTOs';

describe('NotesComponent - Merge Functionality', () => {
  let component: NotesComponent;
  let fixture: ComponentFixture<NotesComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockNotes: NoteDto[] = [
    {
      rowKey: 'A1B2',
      name: 'Note 1',
      sideNote: 'First note',
      imageUrl: 'http://example.com/note1.jpg',
      imageBase64: '',
      entries: []
    },
    {
      rowKey: 'C3D4',
      name: 'Note 2',
      sideNote: 'Second note',
      imageUrl: 'http://example.com/note2.jpg',
      imageBase64: '',
      entries: []
    },
    {
      rowKey: 'E5F6',
      name: 'Note 3',
      sideNote: 'Third note',
      imageUrl: 'http://example.com/note3.jpg',
      imageBase64: '',
      entries: []
    }
  ];

  const mockPagedResponse: PagedResponseDto<NoteDto> = {
    data: mockNotes,
    totalCount: 3,
    pageSize: 10,
    currentPage: 1,
    hasNextPage: false,
    hasPreviousPage: false,
    totalPages: 1
  };

  beforeEach(async () => {
    const notesServiceSpy = jasmine.createSpyObj('NotesService', ['getNotesPaged']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    notesServiceSpy.getNotesPaged.and.returnValue(of(mockPagedResponse));

    await TestBed.configureTestingModule({
      declarations: [NotesComponent, CardListComponent],
      providers: [
        { provide: NotesService, useValue: notesServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotesComponent);
    component = fixture.componentInstance;
    mockNotesService = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    
    fixture.detectChanges();
  });

  it('should open merge modal', () => {
    // Act
    component.openMergeModal();

    // Assert
    expect(component.isMergeModalOpen).toBeTrue();
    expect(component.selectedNotesForMerge.size).toBe(0);
  });

  it('should close merge modal', () => {
    // Arrange
    component.isMergeModalOpen = true;
    component.selectedNotesForMerge.add('A1B2');

    // Act
    component.closeMergeModal();

    // Assert
    expect(component.isMergeModalOpen).toBeFalse();
    expect(component.selectedNotesForMerge.size).toBe(0);
  });

  it('should toggle note selection', () => {
    // Act
    component.toggleNoteSelection('A1B2');

    // Assert
    expect(component.isNoteSelected('A1B2')).toBeTrue();
    expect(component.selectedNotesForMerge.size).toBe(1);

    // Toggle again
    component.toggleNoteSelection('A1B2');

    // Assert
    expect(component.isNoteSelected('A1B2')).toBeFalse();
    expect(component.selectedNotesForMerge.size).toBe(0);
  });

  it('should check if note is selected', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');

    // Act & Assert
    expect(component.isNoteSelected('A1B2')).toBeTrue();
    expect(component.isNoteSelected('C3D4')).toBeFalse();
  });

  it('should check if can show merged note with 2 or more notes', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');

    // Act & Assert
    expect(component.canShowMergedNote()).toBeFalse();

    // Add second note
    component.selectedNotesForMerge.add('C3D4');
    expect(component.canShowMergedNote()).toBeTrue();

    // Add third note
    component.selectedNotesForMerge.add('E5F6');
    expect(component.canShowMergedNote()).toBeTrue();
  });

  it('should generate composite ID correctly for 2 notes', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');
    component.selectedNotesForMerge.add('C3D4');

    // Act
    component.showMergedNote();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes/merge', 'AC13BD24']);
  });

  it('should generate composite ID correctly for 3 notes', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');
    component.selectedNotesForMerge.add('C3D4');
    component.selectedNotesForMerge.add('E5F6');

    // Act
    component.showMergedNote();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes/merge', 'ACE135BDF246']);
  });

  it('should not navigate when less than 2 notes selected', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');

    // Act
    component.showMergedNote();

    // Assert
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should close modal after showing merged note', () => {
    // Arrange
    component.selectedNotesForMerge.add('A1B2');
    component.selectedNotesForMerge.add('C3D4');
    component.isMergeModalOpen = true;

    // Act
    component.showMergedNote();

    // Assert
    expect(component.isMergeModalOpen).toBeFalse();
    expect(component.selectedNotesForMerge.size).toBe(0);
  });
});
