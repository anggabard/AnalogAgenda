import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { NotesComponent } from '../../components/notes/notes.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { NotesService, UserSettingsService } from '../../services';
import { NoteDto, PagedResponseDto } from '../../DTOs';

describe('NotesComponent - Merge Functionality', () => {
  let component: NotesComponent;
  let fixture: ComponentFixture<NotesComponent>;
  let mockNotesService: jasmine.SpyObj<NotesService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockNotes: NoteDto[] = [
    {
      id: 'A1B2',
      name: 'Note 1',
      sideNote: 'First note',
      imageUrl: 'http://example.com/note1.jpg',
      imageBase64: '',
      entries: []
    },
    {
      id: 'C3D4',
      name: 'Note 2',
      sideNote: 'Second note',
      imageUrl: 'http://example.com/note2.jpg',
      imageBase64: '',
      entries: []
    },
    {
      id: 'E5F6',
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
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['getUserSettings']);
    userSettingsServiceSpy.getUserSettings.and.returnValue(of({
      userId: 'user1',
      isSubscribed: false,
      tableView: false,
      entitiesPerPage: 5
    }));

    notesServiceSpy.getNotesPaged.and.returnValue(of(mockPagedResponse));

    await TestBed.configureTestingModule({
      declarations: [NotesComponent, CardListComponent],
      providers: [
        { provide: NotesService, useValue: notesServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    })
      .overrideComponent(NotesComponent, {
        set: {
          template: `
            <div class="container">
              <div class="header">
                <button class="add-note" (click)="onNewNoteClick()">+ New Note +</button>
                <button class="add-note merge-notes" (click)="openMergeModal()">Merge Notes</button>
              </div>
              <div class="notes-section">
                <h2 class="notes-title">Notes</h2>
                <app-card-list
                  *ngIf="cardTemplate"
                  [items]="notes"
                  [cardTemplate]="cardTemplate"
                  [hasMore]="hasMoreNotes"
                  [loading]="loadingNotes"
                  (loadMore)="loadMoreNotes()"
                  (itemClick)="onNoteSelected($event.id)">
                </app-card-list>
              </div>
            </div>
            <ng-template #noteCardTemplate let-note>
              <div class="card"><span>{{ note.name }}</span></div>
            </ng-template>
            <div class="modal-overlay" *ngIf="isMergeModalOpen" (click)="closeMergeModal()">
              <div class="modal-content" (click)="$event.stopPropagation()">
                <div class="modal-header">
                  <h3>Merge Notes</h3>
                  <button class="modal-close" (click)="closeMergeModal()">✕</button>
                </div>
                <div class="modal-body">
                  <p style="text-align: center; margin-bottom: 20px;">Select 2 or more notes to merge:</p>
                  <div class="items-grid">
                    <div *ngFor="let note of notes" class="item-card"
                         [class.selected]="isNoteSelected(note.id)"
                         (click)="toggleNoteSelection(note.id)">
                      <span class="item-name">{{ note.name }}</span>
                    </div>
                  </div>
                  <div class="modal-footer" *ngIf="selectedNotesForMerge.size >= 2">
                    <button class="btn btn-primary" (click)="showMergedNote()">
                      Merge {{ selectedNotesForMerge.size }} Notes
                    </button>
                  </div>
                </div>
              </div>
            </div>
          `
        }
      })
      .compileComponents();

    fixture = TestBed.createComponent(NotesComponent);
    component = fixture.componentInstance;
    mockNotesService = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
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
