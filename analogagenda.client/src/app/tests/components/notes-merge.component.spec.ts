import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotesMergeComponent } from '../../components/notes/notes-merge/notes-merge.component';
import { NotesService } from '../../services';
import { NoteDto, NoteEntryDto } from '../../DTOs';

describe('NotesMergeComponent', () => {
  let component: NotesMergeComponent;
  let fixture: ComponentFixture<NotesMergeComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockNotes: NoteDto[] = [
    {
      id: 'note1',
      name: 'Note 1',
      sideNote: 'First note',
      imageUrl: 'http://example.com/image1.jpg',
      imageBase64: '',
      entries: [
        {
          id: 'entry1',
          noteId: 'note1',
          time: 3.5,
          step: 'Developer',
          details: 'Mix developer',
          index: 0,
          temperatureMin: 20,
          temperatureMax: 25,
          rules: [],
          overrides: []
        },
        {
          id: 'entry2',
          noteId: 'note1',
          time: 2.0,
          step: 'Stop Bath',
          details: 'Stop development',
          index: 1,
          temperatureMin: 18,
          temperatureMax: 22,
          rules: [],
          overrides: []
        }
      ] as NoteEntryDto[]
    },
    {
      id: 'note2',
      name: 'Note 2',
      sideNote: 'Second note',
      imageUrl: 'http://example.com/image2.jpg',
      imageBase64: '',
      entries: [
        {
          id: 'entry3',
          noteId: 'note2',
          time: 1.5,
          step: 'Fixer',
          details: 'Fix the image',
          index: 0,
          temperatureMin: 20,
          temperatureMax: undefined,
          rules: [],
          overrides: []
        }
      ] as NoteEntryDto[]
    }
  ];

  beforeEach(async () => {
    const notesServiceSpy = jasmine.createSpyObj('NotesService', ['getMergedNotes']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    
    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('ABCD1234')
        }
      }
    };

    await TestBed.configureTestingModule({
      declarations: [NotesMergeComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: NotesService, useValue: notesServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotesMergeComponent);
    component = fixture.componentInstance;
    mockNotesService = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load merged notes on init', () => {
    // Arrange
    mockNotesService.getMergedNotes.and.returnValue(of(mockNotes));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getMergedNotes).toHaveBeenCalledWith('ABCD1234');
    expect(component.notes).toEqual(mockNotes);
    expect(component.mergedName).toBe('Note 1 + Note 2');
    expect(component.mergedSideNote).toBe('First note\n\nSecond note');
    expect(component.mergedImageUrl).toBe('http://example.com/image1.jpg');
    expect(component.loading).toBeFalse();
    expect(component.error).toBeNull();
  });

  it('should handle error when loading merged notes fails', () => {
    // Arrange
    mockNotesService.getMergedNotes.and.returnValue(throwError(() => new Error('Failed to load')));

    // Act
    component.ngOnInit();

    // Assert
    expect(component.error).toBe('Failed to load merged notes');
    expect(component.loading).toBeFalse();
    expect(component.notes).toEqual([]);
  });

  it('should set error when compositeId is missing', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);

    // Act
    component.ngOnInit();

    // Assert
    expect(component.error).toBe('Invalid composite ID');
  });

  it('should set start times correctly for entries', () => {
    // Arrange
    component.notes = mockNotes;
    component.recalculateAndSortEntries();

    // Act & Assert
    // First entry should start at 0
    const firstEntry = component.sortedEntries[0];
    expect(firstEntry.startTime).toBeGreaterThanOrEqual(0);
    // All entries should have valid start times
    component.sortedEntries.forEach(entry => {
      expect(entry.startTime).toBeGreaterThanOrEqual(0);
    });
  });

  it('should recalculate and sort entries correctly', () => {
    // Arrange
    component.notes = mockNotes;
    component.filmCount = 1;

    // Act
    component.recalculateAndSortEntries();

    // Assert
    expect(component.sortedEntries.length).toBeGreaterThan(0);
    // Should include OUT/DONE rows for each process
    const outDoneRows = component.sortedEntries.filter(e => component.isOutDoneRow(e));
    expect(outDoneRows.length).toBe(2); // One for each note
  });

  it('should sort entries by start time', () => {
    // Arrange
    component.notes = mockNotes;
    component.recalculateAndSortEntries();

    // Act & Assert
    for (let i = 1; i < component.sortedEntries.length; i++) {
      expect(component.sortedEntries[i].startTime).toBeGreaterThanOrEqual(
        component.sortedEntries[i - 1].startTime
      );
    }
  });

  it('should include OUT/DONE row for each process', () => {
    // Arrange
    component.notes = mockNotes;
    component.recalculateAndSortEntries();

    // Act
    const outDoneRows = component.sortedEntries.filter(e => component.isOutDoneRow(e));

    // Assert
    expect(outDoneRows.length).toBe(2);
    // OUT/DONE rows are sorted, so check that both substances are present
    const substances = outDoneRows.map(r => r.substance);
    expect(substances).toContain(mockNotes[0].name);
    expect(substances).toContain(mockNotes[1].name);
  });

  it('should check if entry is OUT/DONE row', () => {
    // Arrange
    component.notes = mockNotes;
    component.recalculateAndSortEntries();

    // Act
    const outDoneEntry = component.sortedEntries.find(e => component.isOutDoneRow(e));
    const regularEntry = component.sortedEntries.find(e => !component.isOutDoneRow(e));

    // Assert
    expect(outDoneEntry).toBeDefined();
    expect(component.isOutDoneRow(outDoneEntry!)).toBeTrue();
    expect(regularEntry).toBeDefined();
    expect(component.isOutDoneRow(regularEntry!)).toBeFalse();
  });

  it('should increment film count and recalculate', () => {
    // Arrange
    component.notes = mockNotes;
    component.filmCount = 1;
    component.recalculateAndSortEntries();
    const initialEntries = [...component.sortedEntries];

    // Act
    component.incrementFilmCount();

    // Assert
    expect(component.filmCount).toBe(2);
    // Entries should be recalculated (may have different times due to rules)
    expect(component.sortedEntries.length).toBeGreaterThan(0);
  });

  it('should not increment film count beyond 100', () => {
    // Arrange
    component.filmCount = 100;

    // Act
    component.incrementFilmCount();

    // Assert
    expect(component.filmCount).toBe(100);
  });

  it('should decrement film count and recalculate', () => {
    // Arrange
    component.notes = mockNotes;
    component.filmCount = 5;
    component.recalculateAndSortEntries();

    // Act
    component.decrementFilmCount();

    // Assert
    expect(component.filmCount).toBe(4);
    expect(component.sortedEntries.length).toBeGreaterThan(0);
  });

  it('should not decrement film count below 1', () => {
    // Arrange
    component.filmCount = 1;

    // Act
    component.decrementFilmCount();

    // Assert
    expect(component.filmCount).toBe(1);
  });

  it('should format time for display', () => {
    // Act
    const formatted = component.formatTimeForDisplay(1.5);

    // Assert
    expect(formatted).toBe('1m 30s');
  });

  it('should handle notes with rules and overrides', () => {
    // Arrange
    const noteWithRules: NoteDto = {
      id: 'note3',
      name: 'Note 3',
      sideNote: '',
      imageUrl: '',
      imageBase64: '',
      entries: [
        {
          id: 'entry4',
          noteId: 'note3',
          time: 5.0,
          step: 'Development',
          details: 'Develop',
          index: 0,
          temperatureMin: 20,
          temperatureMax: undefined,
          rules: [{ id: '', noteEntryId: '', filmInterval: 3, timeIncrement: 1.0 }],
          overrides: [
            {
              id: '',
              noteEntryId: '',
              filmCountMin: 4,
              filmCountMax: 6,
              time: 10.0
            }
          ]
        }
      ] as NoteEntryDto[]
    };
    component.notes = [noteWithRules];
    component.filmCount = 5; // In override range

    // Act
    component.recalculateAndSortEntries();

    // Assert
    // MergedNoteEntryDto uses rowKey (which is set from entry.id)
    const entry = component.sortedEntries.find(e => e.rowKey === 'entry4');
    expect(entry).toBeDefined();
    expect(entry!.time).toBe(10.0); // Should use override time, not rule
  });

  it('should handle empty notes array', () => {
    // Arrange
    component.notes = [];

    // Act
    component.recalculateAndSortEntries();

    // Assert
    expect(component.sortedEntries.length).toBe(0);
  });

  it('should handle empty sortedEntries in recalculateAndSortEntries', () => {
    // Arrange
    component.notes = [];

    // Act
    component.recalculateAndSortEntries();

    // Assert
    expect(component.sortedEntries.length).toBe(0);
  });

  it('should set merged side note correctly with empty side notes filtered', () => {
    // Arrange
    const notesWithEmptySideNote: NoteDto[] = [
      {
        id: 'note1',
        name: 'Note 1',
        sideNote: '',
        imageUrl: '',
        imageBase64: '',
        entries: []
      },
      {
        id: 'note2',
        name: 'Note 2',
        sideNote: 'Has content',
        imageUrl: '',
        imageBase64: '',
        entries: []
      }
    ];
    mockNotesService.getMergedNotes.and.returnValue(of(notesWithEmptySideNote));

    // Act
    component.ngOnInit();

    // Assert
    expect(component.mergedSideNote).toBe('Has content');
  });
});
