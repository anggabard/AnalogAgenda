import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotesMergeComponent } from '../../components/notes/notes-merge/notes-merge.component';
import { NotesService } from '../../services';
import { MergedNoteDto, MergedNoteEntryDto } from '../../DTOs';

describe('NotesMergeComponent', () => {
  let component: NotesMergeComponent;
  let fixture: ComponentFixture<NotesMergeComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockMergedNote: MergedNoteDto = {
    compositeId: 'ABCD1234',
    name: 'Note 1 + Note 2',
    sideNote: 'Combined notes',
    imageUrl: 'http://example.com/image.jpg',
    imageBase64: '',
    entries: [
      {
        rowKey: 'entry1',
        noteRowKey: 'note1',
        time: 3.5,
        step: 'Developer',
        details: 'Mix developer',
        index: 0,
        temperatureMin: 20,
        temperatureMax: 25,
        substance: 'Note 1',
        startTime: 0
      },
      {
        rowKey: 'entry2',
        noteRowKey: 'note2',
        time: 2.0,
        step: 'Stop Bath',
        details: 'Stop development',
        index: 1,
        temperatureMin: 18,
        temperatureMax: 22,
        substance: 'Note 2',
        startTime: 3.5
      }
    ]
  };

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

  it('should load merged note on init', () => {
    // Arrange
    mockNotesService.getMergedNotes.and.returnValue(of(mockMergedNote));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getMergedNotes).toHaveBeenCalledWith('ABCD1234');
    expect(component.mergedNote).toEqual(mockMergedNote);
    expect(component.loading).toBeFalse();
    expect(component.error).toBeNull();
  });

  it('should handle error when loading merged note fails', () => {
    // Arrange
    mockNotesService.getMergedNotes.and.returnValue(throwError(() => new Error('Failed to load')));

    // Act
    component.ngOnInit();

    // Assert
    expect(component.error).toBe('Failed to load merged note');
    expect(component.loading).toBeFalse();
    expect(component.mergedNote).toBeNull();
  });

  it('should set error when compositeId is missing', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);

    // Act
    component.ngOnInit();

    // Assert
    expect(component.error).toBe('Invalid composite ID');
  });

  it('should calculate accumulated start time correctly', () => {
    // Arrange
    component.mergedNote = mockMergedNote;

    // Act
    const startTime0 = component.getAccumulatedStartTime(0);
    const startTime1 = component.getAccumulatedStartTime(1);

    // Assert
    expect(startTime0).toBe(0);
    expect(startTime1).toBe(3.5);
  });

  it('should get effective time correctly', () => {
    // Arrange
    const entry = mockMergedNote.entries[0];

    // Act
    const effectiveTime = component.getEffectiveTime(entry);

    // Assert
    expect(effectiveTime).toBe(3.5);
  });

  it('should get effective step correctly', () => {
    // Arrange
    const entry = mockMergedNote.entries[0];

    // Act
    const effectiveStep = component.getEffectiveStep(entry);

    // Assert
    expect(effectiveStep).toBe('Developer');
  });

  it('should get effective details correctly', () => {
    // Arrange
    const entry = mockMergedNote.entries[0];

    // Act
    const effectiveDetails = component.getEffectiveDetails(entry);

    // Assert
    expect(effectiveDetails).toBe('Mix developer');
  });

  it('should get effective temperature correctly', () => {
    // Arrange
    const entry = mockMergedNote.entries[0];

    // Act
    const effectiveTemp = component.getEffectiveTemperature(entry);

    // Assert
    expect(effectiveTemp.min).toBe(20);
    expect(effectiveTemp.max).toBe(25);
  });

  it('should navigate back to notes', () => {
    // Act
    component.goBack();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes']);
  });
});
