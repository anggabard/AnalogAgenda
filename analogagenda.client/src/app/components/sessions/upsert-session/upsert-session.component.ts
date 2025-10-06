import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { BaseUpsertComponent } from '../../common/base-upsert/base-upsert.component';
import { SessionService, DevKitService, FilmService } from '../../../services';
import { SessionDto, DevKitDto, FilmDto } from '../../../DTOs';
import { DateHelper } from '../../../helpers/date.helper';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

interface DevKitWithFilms {
  devKit: DevKitDto;
  assignedFilms: FilmDto[];
}

@Component({
  selector: 'app-upsert-session',
  templateUrl: './upsert-session.component.html',
  styleUrl: './upsert-session.component.css'
})
export class UpsertSessionComponent extends BaseUpsertComponent<SessionDto> implements OnInit {

  constructor(
    private sessionService: SessionService, 
    private devKitService: DevKitService,
    private filmService: FilmService
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
  successMessage: string | null = null;

  // Expose item from base class for template
  get currentItem(): SessionDto {
    return this.form.value as SessionDto;
  }

  override ngOnInit(): void {
    // Determine view/edit mode
    this.isEditMode = !this.isInsert && this.route.snapshot.queryParams['edit'] === 'true';
    this.isViewMode = !this.isInsert && !this.isEditMode;
    
    // Call parent ngOnInit which loads the item (and our getItemObservable handles devkits/films)
    super.ngOnInit();
    
    // For new sessions, initialize empty and load available items
    if (this.isInsert) {
      this.initializeEmptySession();
      this.loadAvailableItems();
    }
  }

  protected createForm(): FormGroup {
    return this.fb.group({
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

  protected getUpdateObservable(rowKey: string, item: SessionDto): Observable<any> {
    const processedItem = this.processFormData();
    return this.sessionService.update(rowKey, processedItem);
  }

  protected getDeleteObservable(rowKey: string): Observable<any> {
    return this.sessionService.deleteById(rowKey);
  }

  protected getItemObservable(rowKey: string): Observable<SessionDto> {
    this.loading = true;
    return new Observable(observer => {
      this.sessionService.getById(rowKey).subscribe({
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

              this.sessionDevKits = usedDevKitsRowKeys.map((devKitRowKey: string) => {
                const devKit = data.allDevKits.find(dk => dk.rowKey === devKitRowKey);
                return {
                  devKit: devKit!,
                  assignedFilms: []
                };
              }).filter((item: DevKitWithFilms) => item.devKit);

              this.unassignedFilms = data.allFilms.filter(f => 
                developedFilmsRowKeys.includes(f.rowKey)
              );

              this.updateAvailableItems(data.allDevKits, data.allFilms);
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

  // Process form data before submission
  private processFormData(): SessionDto {
    const formValue = this.form.value;
    return {
      ...formValue,
      usedSubstances: JSON.stringify(this.sessionDevKits.map(sdk => sdk.devKit.rowKey)),
      developedFilms: JSON.stringify([
        ...this.unassignedFilms.map(f => f.rowKey),
        ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.rowKey))
      ]),
      usedSubstancesList: this.sessionDevKits.map(sdk => sdk.devKit.rowKey),
      developedFilmsList: [
        ...this.unassignedFilms.map(f => f.rowKey),
        ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.rowKey))
      ]
    };
  }

  // Override submit to handle custom flow
  override submit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.errorMessage = null;

    const operation$ = this.isInsert 
      ? this.getCreateObservable(this.form.value)
      : this.getUpdateObservable(this.rowKey!, this.form.value);

    operation$.subscribe({
      next: (response: any) => {
        this.loading = false;
        if (this.isInsert) {
          // For new sessions, redirect to view mode
          const createdSession = response as SessionDto;
          if (createdSession && createdSession.rowKey) {
            this.router.navigate(['/sessions', createdSession.rowKey]);
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
  }

  private updateAvailableItems(allDevKits: DevKitDto[], allFilms: FilmDto[]): void {
    const usedDevKitRowKeys = this.sessionDevKits.map(sdk => sdk.devKit.rowKey);
    const sessionFilmRowKeys = [
      ...this.unassignedFilms.map(f => f.rowKey),
      ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.rowKey))
    ];

    this.availableDevKits = allDevKits.filter(dk => !usedDevKitRowKeys.includes(dk.rowKey));
    this.availableUnassignedFilms = allFilms.filter(f => 
      !sessionFilmRowKeys.includes(f.rowKey)
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
          this.availableUnassignedFilms = data.films;
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
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.container.data.length
      );
    }
  }

  // DevKit management
  addDevKitToSession(devKit: DevKitDto): void {
    const existing = this.sessionDevKits.find(sdk => sdk.devKit.rowKey === devKit.rowKey);
    if (!existing) {
      this.sessionDevKits.push({
        devKit: devKit,
        assignedFilms: []
      });
      this.availableDevKits = this.availableDevKits.filter(dk => dk.rowKey !== devKit.rowKey);
    }
  }

  removeDevKitFromSession(devKitRowKey: string): void {
    const devKitIndex = this.sessionDevKits.findIndex(sdk => sdk.devKit.rowKey === devKitRowKey);
    if (devKitIndex >= 0) {
      const devKitWithFilms = this.sessionDevKits[devKitIndex];
      this.unassignedFilms.push(...devKitWithFilms.assignedFilms);
      this.availableDevKits.push(devKitWithFilms.devKit);
      this.sessionDevKits.splice(devKitIndex, 1);
    }
  }

  // Film management
  addFilmToSession(film: FilmDto): void {
    if (!this.unassignedFilms.find(f => f.rowKey === film.rowKey)) {
      this.unassignedFilms.push(film);
      this.availableUnassignedFilms = this.availableUnassignedFilms.filter(f => f.rowKey !== film.rowKey);
    }
  }

  removeFilmFromSession(filmRowKey: string): void {
    const filmIndex = this.unassignedFilms.findIndex(f => f.rowKey === filmRowKey);
    if (filmIndex >= 0) {
      const removedFilm = this.unassignedFilms.splice(filmIndex, 1)[0];
      this.availableUnassignedFilms.push(removedFilm);
      return;
    }
    
    for (const devKitWithFilms of this.sessionDevKits) {
      const assignedIndex = devKitWithFilms.assignedFilms.findIndex(f => f.rowKey === filmRowKey);
      if (assignedIndex >= 0) {
        const removedFilm = devKitWithFilms.assignedFilms.splice(assignedIndex, 1)[0];
        this.availableUnassignedFilms.push(removedFilm);
        break;
      }
    }
  }

  // Hover effects
  onDevKitHover(devKitRowKey: string, event: MouseEvent): void {
    this.hoveredDevKit = devKitRowKey;
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
    if (this.rowKey) {
      this.getItemObservable(this.rowKey).subscribe({
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
      ...this.sessionDevKits.map(sdk => `devkit-${sdk.devKit.rowKey}`)
    ];
  }

  get filteredAvailableDevKits(): DevKitDto[] {
    return this.showExpiredDevKits 
      ? this.availableDevKits 
      : this.availableDevKits.filter(dk => !dk.expired);
  }

  toggleDevKitSelection(devKitRowKey: string): void {
    const index = this.selectedDevKitsForModal.indexOf(devKitRowKey);
    if (index >= 0) {
      this.selectedDevKitsForModal.splice(index, 1);
    } else {
      this.selectedDevKitsForModal.push(devKitRowKey);
    }
  }

  toggleFilmSelection(filmRowKey: string): void {
    const index = this.selectedFilmsForModal.indexOf(filmRowKey);
    if (index >= 0) {
      this.selectedFilmsForModal.splice(index, 1);
    } else {
      this.selectedFilmsForModal.push(filmRowKey);
    }
  }

  isDevKitSelectedForModal(devKitRowKey: string): boolean {
    return this.selectedDevKitsForModal.includes(devKitRowKey);
  }

  isFilmSelectedForModal(filmRowKey: string): boolean {
    return this.selectedFilmsForModal.includes(filmRowKey);
  }

  addSelectedDevKits(): void {
    this.selectedDevKitsForModal.forEach(rowKey => {
      const devKit = this.availableDevKits.find(dk => dk.rowKey === rowKey);
      if (devKit) {
        this.addDevKitToSession(devKit);
      }
    });
    this.selectedDevKitsForModal = [];
    this.showAddDevKitModal = false;
  }

  addSelectedFilms(): void {
    this.selectedFilmsForModal.forEach(rowKey => {
      const film = this.availableUnassignedFilms.find(f => f.rowKey === rowKey);
      if (film) {
        this.addFilmToSession(film);
      }
    });
    this.selectedFilmsForModal = [];
    this.showAddFilmModal = false;
  }

  closeAddDevKitModal(): void {
    this.selectedDevKitsForModal = [];
    this.showAddDevKitModal = false;
  }

  closeAddFilmModal(): void {
    this.selectedFilmsForModal = [];
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
          imageUrl: reader.result as string,
          imageBase64: reader.result as string
        });
      };
    }
  }
}