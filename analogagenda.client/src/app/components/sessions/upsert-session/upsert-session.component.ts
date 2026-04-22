import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { BaseUpsertComponent } from '../../common';
import { SessionService, DevKitService, FilmService, IdeaService } from '../../../services';
import { SessionDto, DevKitDto, FilmDto, IdeaDto } from '../../../DTOs';
import { DateHelper } from '../../../helpers/date.helper';
import { modalListMatches } from '../../../helpers/modal-list-search.helper';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

interface DevKitWithFilms {
  devKit: DevKitDto;
  assignedFilms: FilmDto[];
}

@Component({
    selector: 'app-upsert-session',
    templateUrl: './upsert-session.component.html',
    styleUrl: './upsert-session.component.css',
    standalone: false
})
export class UpsertSessionComponent extends BaseUpsertComponent<SessionDto> implements OnInit {

  constructor(
    private sessionService: SessionService,
    private devKitService: DevKitService,
    private filmService: FilmService,
    private ideaService: IdeaService
  ) {
    super();
  }

  // Session devkits and films
  sessionDevKits: DevKitWithFilms[] = [];
  unassignedFilms: FilmDto[] = [];
  availableDevKits: DevKitDto[] = [];
  availableUnassignedFilms: FilmDto[] = [];

  // UI state
  hoveredDevKit: string | null = null;
  isViewMode = false;
  isEditMode = false;
  showAddDevKitModal = false;
  showAddFilmModal = false;
  showSessionImageModal = false;
  showExpiredDevKits = false;
  selectedDevKitsForModal: string[] = [];
  selectedFilmsForModal: string[] = [];
  addDevKitModalSearch = '';
  addFilmModalSearch = '';
  successMessage: string | null = null;
  
  // Participants management
  participants: string[] = [];
  newParticipant: string = '';

  /** Wacky ideas linked to this session (ids + titles for tags). */
  allIdeas: IdeaDto[] = [];
  linkedIdeaIds: string[] = [];
  /** Titles from GET session (preferred over allIdeas for view labels). */
  connectedIdeaSummaries: { id: string; title: string }[] = [];
  selectedIdeaIdToAdd = '';

  nextSessionIndexForPlaceholder = 1;

  // Expose item from base class for template
  get currentItem(): SessionDto {
    return this.form.value as SessionDto;
  }

  get ideasAvailableToAdd(): IdeaDto[] {
    const set = new Set(this.linkedIdeaIds);
    return this.allIdeas.filter((i) => !set.has(i.id));
  }

  private loadAllIdeas(): void {
    this.ideaService.getAll().subscribe({
      next: (ideas: IdeaDto[]) => {
        this.allIdeas = [...ideas].sort((a, b) =>
          (a.title ?? '').localeCompare(b.title ?? '', undefined, { sensitivity: 'base' })
        );
      },
      error: () => {
        this.allIdeas = [];
      },
    });
  }

  linkedIdeaTitle(id: string): string {
    const fromSession = this.connectedIdeaSummaries.find((x) => x.id === id);
    if (fromSession?.title?.trim()) {
      return fromSession.title.trim();
    }
    const fromApi = this.allIdeas.find((i) => i.id === id);
    return fromApi?.title?.trim() || id;
  }

  /** Comma-separated idea names for view mode (uses loaded ideas + linked ids). */
  get wackyIdeasNamesDisplay(): string {
    return this.linkedIdeaIds.map((id) => this.linkedIdeaTitle(id)).join(', ');
  }

  get trimmedSessionDescription(): string {
    const d = (this.form.get('description')?.value as string) ?? '';
    return d.trim();
  }

  addLinkedIdea(): void {
    const id = this.selectedIdeaIdToAdd?.trim();
    if (!id || this.linkedIdeaIds.includes(id)) {
      return;
    }
    this.linkedIdeaIds = [...this.linkedIdeaIds, id];
    this.selectedIdeaIdToAdd = '';
    this.form.markAsDirty();
  }

  removeLinkedIdea(ideaId: string): void {
    this.linkedIdeaIds = this.linkedIdeaIds.filter((x) => x !== ideaId);
    this.form.markAsDirty();
  }

  override ngOnInit(): void {
    // Determine view/edit mode
    this.isEditMode = !this.isInsert && this.route.snapshot.queryParams['edit'] === 'true';
    this.isViewMode = !this.isInsert && !this.isEditMode;

    this.loadAllIdeas();

    // Call parent ngOnInit which loads the item (and our getItemObservable handles devkits/films)
    super.ngOnInit();
    
    // For new sessions, initialize empty and load available items
    if (this.isInsert) {
      this.initializeEmptySession();
      this.loadAvailableItems();
      this.sessionService.getNextSessionIndex().subscribe({
        next: (res) => {
          this.nextSessionIndexForPlaceholder = Math.max(1, res.nextIndex);
        },
        error: () => {
          /* keep default 1 */
        },
      });
    }
  }

  protected createForm(): FormGroup {
    return this.fb.group({
      name: [''],
      sessionDate: [DateHelper.getTodayForInput(), Validators.required],
      location: ['', Validators.required],
      participants: [[]],
      imageUrl: [''],
      imageBase64: [''],
      description: [''],
      usedSubstances: [[]],
      developedFilms: [[]]
    });
  }

  protected getCreateObservable(item: SessionDto): Observable<any> {
    const processedItem = this.processFormData();
    return this.sessionService.add(processedItem);
  }

  protected getUpdateObservable(id: string, item: SessionDto): Observable<any> {
    const processedItem = this.processFormData();
    return this.sessionService.update(id, processedItem);
  }

  protected getDeleteObservable(id: string): Observable<any> {
    return this.sessionService.deleteById(id);
  }

  protected getItemObservable(id: string): Observable<SessionDto> {
    this.loading = true;
    return new Observable(observer => {
      this.sessionService.getById(id).subscribe({
        next: (session) => {
          // After getting the session, load devkits and films
          forkJoin({
            allDevKits: this.devKitService.getAll(),
            allFilms: this.filmService.getAll()
          }).subscribe({
            next: (data) => {
              // Initialize devkits and films
              const usedDevKitsRowKeys = session.usedSubstancesList || [];
              const developedFilmsRowKeys = session.developedFilmsList || [];
              const filmToDevKitMapping = session.filmToDevKitMapping || {};

              // Initialize devkits with their assigned films based on the mapping
              this.sessionDevKits = usedDevKitsRowKeys.map((devKitId: string) => {
                const devKit = data.allDevKits.find(dk => dk.id === devKitId);
                const filmIds = filmToDevKitMapping[devKitId] || [];
                const assignedFilms = filmIds
                  .map(filmId => data.allFilms.find(f => f.id === filmId))
                  .filter(f => f !== undefined) as FilmDto[];
                
                return {
                  devKit: devKit!,
                  assignedFilms: assignedFilms
                };
              }).filter((item: DevKitWithFilms) => item.devKit);

              // Unassigned films are those in the session but not in any devkit
              const assignedfilmIds = Object.values(filmToDevKitMapping).flat();
              
              // Get films that are in the session's developedFilmsList but not assigned to any DevKit
              const sessionFilmsWithoutDevKit = data.allFilms.filter(f => 
                developedFilmsRowKeys.includes(f.id) && !assignedfilmIds.includes(f.id)
              );
              
              // Get films that have this session assigned but no DevKit assigned
              const filmsWithSessionButNoDevKit = data.allFilms.filter(f => 
                f.developedInSessionId === session.id && !f.developedWithDevKitId
              );
              
              // Combine both lists and remove duplicates
              this.unassignedFilms = [...sessionFilmsWithoutDevKit, ...filmsWithSessionButNoDevKit]
                .filter((film, index, self) => 
                  index === self.findIndex(f => f.id === film.id)
                );

              this.updateAvailableItems(data.allDevKits, data.allFilms);
              
              // Load participants
              this.participants = session.participantsList || [];

              this.linkedIdeaIds =
                session.connectedIdeaIds?.length
                  ? [...session.connectedIdeaIds]
                  : (session.connectedIdeas?.map((x) => x.id) ?? []);
              this.connectedIdeaSummaries = session.connectedIdeas ? [...session.connectedIdeas] : [];

              this.loading = false;
              observer.next(session);
              observer.complete();
            },
            error: (err) => {
              this.loading = false;
              observer.error(err);
            }
          });
        },
        error: (err) => {
          this.loading = false;
          observer.error(err);
        }
      });
    });
  }

  protected getBaseRoute(): string {
    return '/sessions';
  }

  protected getEntityName(): string {
    return 'Session';
  }

  protected override afterPatchValueForEdit(_item: SessionDto): void {
    this.sessionService.getNextSessionIndex().subscribe({
      next: (res) => {
        this.nextSessionIndexForPlaceholder = Math.max(1, res.nextIndex);
      },
      error: () => { /* keep default */ }
    });
  }

  // Process form data before submission
  private processFormData(): SessionDto {
    const formValue = this.form.value;
    
    // Build filmToDevKitMapping
    const filmToDevKitMapping: { [devKitId: string]: string[] } = {};
    for (const devKitWithFilms of this.sessionDevKits) {
      filmToDevKitMapping[devKitWithFilms.devKit.id] = devKitWithFilms.assignedFilms.map(f => f.id);
    }
    
    // Build the DTO with only the properties the backend expects
    const dto: any = {
      id: this.id || '',
      name: (formValue.name as string)?.trim() || null,
      sessionDate: formValue.sessionDate,
      location: formValue.location,
      participants: JSON.stringify(this.participants),
      description: formValue.description || '',
      usedSubstances: this.sessionDevKits.map(sdk => sdk.devKit.id).join(','),
      developedFilms: [
        ...this.unassignedFilms.map(f => f.id),
        ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.id))
      ].join(','),
      imageUrl: formValue.imageUrl || '',
      imageBase64: formValue.imageBase64 || '',
      filmToDevKitMapping: filmToDevKitMapping,
      connectedIdeaIds: [...this.linkedIdeaIds]
    };
    
    return dto as SessionDto;
  }

  // Override submit to handle custom flow
  override submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = 'Please set a location and a date before saving.';
      return;
    }

    this.loading = true;
    this.errorMessage = null;

    const operation$ = this.isInsert 
      ? this.getCreateObservable(this.form.value)
      : this.getUpdateObservable(this.id!, this.form.value);

    operation$.subscribe({
      next: (response: any) => {
        this.loading = false;
        if (this.isInsert) {
          // For new sessions, redirect to view mode
          const createdSession = response as SessionDto;
          if (createdSession && createdSession.id) {
            this.router.navigate(['/sessions', createdSession.id]);
          } else {
            this.router.navigate(['/sessions']);
          }
        } else {
          // For updates, show success message
          this.successMessage = 'Session updated successfully!';
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = `Error ${this.isInsert ? 'creating' : 'updating'} session: ${err.message || 'Unknown error'}`;
      }
    });
  }

  private initializeEmptySession(): void {
    this.sessionDevKits = [];
    this.unassignedFilms = [];
    this.linkedIdeaIds = [];
    this.connectedIdeaSummaries = [];
    this.selectedIdeaIdToAdd = '';
  }

  private updateAvailableItems(allDevKits: DevKitDto[], allFilms: FilmDto[]): void {
    const useddevKitIds = this.sessionDevKits.map(sdk => sdk.devKit.id);
    const sessionfilmIds = [
      ...this.unassignedFilms.map(f => f.id),
      ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.id))
    ];

    this.availableDevKits = allDevKits.filter(dk => !useddevKitIds.includes(dk.id));
    this.availableUnassignedFilms = allFilms.filter(f => 
      !sessionfilmIds.includes(f.id) && 
      f.developedInSessionId !== this.id && // Don't show films already assigned to this session
      !f.developed &&
      !!(f.formattedExposureDate && f.formattedExposureDate.trim())
    );
  }

  private loadAvailableItems(): void {
    forkJoin({
      devKits: this.devKitService.getAll(),
      films: this.filmService.getAll()
    }).subscribe({
      next: (data) => {
        if (this.isInsert) {
          this.availableDevKits = data.devKits;
          this.availableUnassignedFilms = data.films.filter(f =>
            !f.developed && !!(f.formattedExposureDate && f.formattedExposureDate.trim()));
        }
      },
      error: (err) => {
        console.error('Error loading available items:', err);
      }
    });
  }

  // Drag and drop handlers
  onFilmDrop(event: CdkDragDrop<any[]>): void {
    if (!event.isPointerOverContainer) {
      return;
    }
    
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Use the explicit drag data to find the correct item
      const draggedItem = event.item.data;
      const sourceArray = event.previousContainer.data;
      const targetArray = event.container.data;
      
      // Find the actual index of the dragged item in the source array
      const actualIndex = sourceArray.findIndex(item => item.id === draggedItem.id);
      
      if (actualIndex !== -1) {
        // Remove from source array
        const [movedItem] = sourceArray.splice(actualIndex, 1);
        // Add to target array
        targetArray.push(movedItem);
      }
    }
    
    // Mark form as dirty when films are moved
    this.form.markAsDirty();
  }

  // DevKit management
  addDevKitToSession(devKit: DevKitDto): void {
    const existing = this.sessionDevKits.find(sdk => sdk.devKit.id === devKit.id);
    if (!existing) {
      this.sessionDevKits.push({
        devKit: devKit,
        assignedFilms: []
      });
      this.availableDevKits = this.availableDevKits.filter(dk => dk.id !== devKit.id);
      this.form.markAsDirty();
    }
  }

  removeDevKitFromSession(devKitId: string): void {
    const devKitIndex = this.sessionDevKits.findIndex(sdk => sdk.devKit.id === devKitId);
    if (devKitIndex >= 0) {
      const devKitWithFilms = this.sessionDevKits[devKitIndex];
      this.unassignedFilms.push(...devKitWithFilms.assignedFilms);
      this.availableDevKits.push(devKitWithFilms.devKit);
      this.sessionDevKits.splice(devKitIndex, 1);
      this.form.markAsDirty();
    }
  }

  // Film management
  addFilmToSession(film: FilmDto): void {
    if (!this.unassignedFilms.find(f => f.id === film.id)) {
      this.unassignedFilms.push(film);
      this.availableUnassignedFilms = this.availableUnassignedFilms.filter(f => f.id !== film.id);
      this.form.markAsDirty();
    }
  }

  removeFilmFromSession(filmId: string): void {
    const filmIndex = this.unassignedFilms.findIndex(f => f.id === filmId);
    if (filmIndex >= 0) {
      const removedFilm = this.unassignedFilms.splice(filmIndex, 1)[0];
      this.availableUnassignedFilms.push(removedFilm);
      this.form.markAsDirty();
      return;
    }
    
    for (const devKitWithFilms of this.sessionDevKits) {
      const assignedIndex = devKitWithFilms.assignedFilms.findIndex(f => f.id === filmId);
      if (assignedIndex >= 0) {
        const removedFilm = devKitWithFilms.assignedFilms.splice(assignedIndex, 1)[0];
        this.availableUnassignedFilms.push(removedFilm);
        this.form.markAsDirty();
        break;
      }
    }
  }

  // Hover effects
  onDevKitHover(devKitId: string, event: MouseEvent): void {
    this.hoveredDevKit = devKitId;
    const button = event.target as HTMLElement;
    const rect = button.getBoundingClientRect();
    
    setTimeout(() => {
      const tooltip = document.querySelector('.devkit-image-tooltip') as HTMLElement;
      if (tooltip) {
        const tooltipWidth = 190;
        const rightPosition = rect.right + 10 + tooltipWidth;
        
        if (rightPosition > window.innerWidth) {
          tooltip.style.left = `${rect.left - tooltipWidth - 10}px`;
        } else {
          tooltip.style.left = `${rect.right + 10}px`;
        }
        tooltip.style.top = `${rect.top}px`;
      }
    }, 0);
  }

  onDevKitLeave(): void {
    this.hoveredDevKit = null;
  }

  // Toggle between view and edit mode
  toggleEditMode(): void {
    this.isEditMode = !this.isEditMode;
    this.isViewMode = !this.isEditMode;
    
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { edit: this.isEditMode ? 'true' : null },
      queryParamsHandling: 'merge'
    });
    
    // Reload data by calling getItemObservable
    if (this.id) {
      this.getItemObservable(this.id).subscribe({
        next: (session) => {
          this.form.patchValue(session);
        }
      });
    }
  }

  // Utility methods
  getConnectedLists(): string[] {
    return [
      'unassigned-films',
      ...this.sessionDevKits.map(sdk => `devkit-${sdk.devKit.id}`)
    ];
  }

  get filteredAvailableDevKits(): DevKitDto[] {
    return this.showExpiredDevKits 
      ? this.availableDevKits 
      : this.availableDevKits.filter(dk => !dk.expired);
  }

  get devKitsForAddModal(): DevKitDto[] {
    return this.filteredAvailableDevKits.filter((dk) =>
      modalListMatches(this.addDevKitModalSearch, dk.name, dk.type));
  }

  get filmsForAddModal(): FilmDto[] {
    return this.availableUnassignedFilms.filter((f) =>
      modalListMatches(
        this.addFilmModalSearch,
        f.name,
        f.brand,
        f.type,
        f.iso,
        f.purchasedBy
      ));
  }

  toggleDevKitSelection(devKitId: string): void {
    const index = this.selectedDevKitsForModal.indexOf(devKitId);
    if (index >= 0) {
      this.selectedDevKitsForModal.splice(index, 1);
    } else {
      this.selectedDevKitsForModal.push(devKitId);
    }
  }

  toggleFilmSelection(filmId: string): void {
    const index = this.selectedFilmsForModal.indexOf(filmId);
    if (index >= 0) {
      this.selectedFilmsForModal.splice(index, 1);
    } else {
      this.selectedFilmsForModal.push(filmId);
    }
  }

  isDevKitSelectedForModal(devKitId: string): boolean {
    return this.selectedDevKitsForModal.includes(devKitId);
  }

  isFilmSelectedForModal(filmId: string): boolean {
    return this.selectedFilmsForModal.includes(filmId);
  }

  addSelectedDevKits(): void {
    this.selectedDevKitsForModal.forEach(id => {
      const devKit = this.availableDevKits.find(dk => dk.id === id);
      if (devKit) {
        this.addDevKitToSession(devKit);
      }
    });
    this.selectedDevKitsForModal = [];
    this.showAddDevKitModal = false;
  }

  addSelectedFilms(): void {
    this.selectedFilmsForModal.forEach(id => {
      const film = this.availableUnassignedFilms.find(f => f.id === id);
      if (film) {
        this.addFilmToSession(film);
      }
    });
    this.selectedFilmsForModal = [];
    this.showAddFilmModal = false;
  }

  closeAddDevKitModal(): void {
    this.selectedDevKitsForModal = [];
    this.addDevKitModalSearch = '';
    this.showAddDevKitModal = false;
  }

  closeAddFilmModal(): void {
    this.selectedFilmsForModal = [];
    this.addFilmModalSearch = '';
    this.showAddFilmModal = false;
  }

  onSessionImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        this.form.patchValue({
          imageBase64: reader.result as string
        });
      };
    }
  }

  // Participants management
  addParticipant(): void {
    const participant = this.newParticipant.trim();
    if (participant && !this.participants.includes(participant)) {
      this.participants.push(participant);
      this.newParticipant = '';
      this.form.patchValue({ participants: this.participants });
      this.form.markAsDirty();
    }
  }

  removeParticipant(participant: string): void {
    this.participants = this.participants.filter(p => p !== participant);
    this.form.patchValue({ participants: this.participants });
    this.form.markAsDirty();
  }

  // Helper method to calculate total films count for delete modal
  getTotalFilmsCount(): number {
    const unassignedCount = this.unassignedFilms.length;
    const assignedCount = this.sessionDevKits.reduce((sum, sdk) => sum + sdk.assignedFilms.length, 0);
    return unassignedCount + assignedCount;
  }

  // TrackBy functions for ngFor loops
  trackByFilmId(index: number, film: FilmDto): string {
    return film.id;
  }

  trackByDevKitId(index: number, devKitWithFilms: DevKitWithFilms): string {
    return devKitWithFilms.devKit.id;
  }

  trackByDevKitDtoId(index: number, devKit: DevKitDto): string {
    return devKit.id;
  }

  formatDisplaySessionDate(value: string | null | undefined): string {
    return DateHelper.formatDdMmYyyy(value ?? '');
  }

  get sessionNamePlaceholder(): string {
    const raw = this.isInsert ? this.nextSessionIndexForPlaceholder : this.currentItem.index;
    const n =
      typeof raw === 'number' && raw >= 1 ? raw : this.nextSessionIndexForPlaceholder;
    return `Session ${Math.max(1, n)}`;
  }
}