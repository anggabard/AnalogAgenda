import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotesComponent } from '../../components/notes/notes.component';
import { CardListComponent, ListComponent, TableListComponent, ImagePreviewComponent } from '../../components/common';
import { NotesService, UserSettingsService } from '../../services';
import { NoteDto, PagedResponseDto } from '../../DTOs';
import { TestConfig } from '../test.config';

describe('NotesComponent', () => {
  let component: NotesComponent;
  let fixture: ComponentFixture<NotesComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const notesServiceSpy = TestConfig.createCrudServiceSpy('NotesService', ['getNotesPaged']);
    const routerSpy = TestConfig.createRouterSpy();
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['getUserSettings']);
    userSettingsServiceSpy.getUserSettings.and.returnValue(of({
      userId: 'user1',
      isSubscribed: false,
      tableView: false,
      entitiesPerPage: 5
    }));

    const emptyPagedResponse = TestConfig.createEmptyPagedResponse<NoteDto>();
    TestConfig.setupPaginatedServiceMocks(notesServiceSpy, [], {
      getNotesPaged: emptyPagedResponse
    });

    await TestConfig.configureTestBed({
      declarations: [NotesComponent, CardListComponent, ListComponent, TableListComponent, ImagePreviewComponent],
      providers: [
        { provide: NotesService, useValue: notesServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotesComponent);
    component = fixture.componentInstance;
    mockNotesService = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });


  it('should load notes on initialization', () => {
    // Arrange - BasePaginatedListComponent loads user settings then loadItems()
    const mockNotes: NoteDto[] = [
      {
        id: '1',
        name: 'Test Note 1',
        sideNote: 'Test Side Note 1',
        imageUrl: 'test-url-1',
        imageBase64: '',
        entries: []
      },
      {
        id: '2',
        name: 'Test Note 2',
        sideNote: 'Test Side Note 2',
        imageUrl: 'test-url-2',
        imageBase64: '',
        entries: []
      }
    ];

    const pagedResponse = TestConfig.createPagedResponse(mockNotes);

    mockNotesService.getNotesPaged.and.returnValue(of(pagedResponse));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getNotesPaged).toHaveBeenCalledWith(1, 5);
    expect(component.notes).toEqual(mockNotes);
    expect(component.notes.length).toBe(2);
  });

  it('should handle error when loading notes', () => {
    // Arrange
    spyOn(console, 'error');
    const errorResponse = 'Failed to load notes';
    mockNotesService.getNotesPaged.and.returnValue(throwError(() => errorResponse));

    // Act
    component.ngOnInit();

    // Assert
    expect(mockNotesService.getNotesPaged).toHaveBeenCalledWith(1, 5);
    expect(console.error).toHaveBeenCalledWith('Error loading items:', errorResponse);
    expect(component.notes).toEqual([]); // Should remain empty on error
  });

  it('should navigate to new note page when onNewNoteClick is called', () => {
    // Act
    component.onNewNoteClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes/new']);
  });

  it('should navigate to note details when onNoteSelected is called', () => {
    // Arrange
    const id = 'test-row-key';

    // Act
    component.onNoteSelected(id);

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/notes', id]);
  });


});
