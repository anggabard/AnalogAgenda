import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SessionService, DevKitService, FilmService } from '../../../services';
import { SessionDto, DevKitDto, FilmDto } from '../../../DTOs';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

interface DevKitWithFilms {
  devKit: DevKitDto;
  assignedFilms: FilmDto[];
}

@Component({
  selector: 'app-session-management',
  templateUrl: './session-management.component.html',
  styleUrl: './session-management.component.css'
})
export class SessionManagementComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private sessionService = inject(SessionService);
  private devKitService = inject(DevKitService);
  private filmService = inject(FilmService);

  // Session data
  sessionRowKey: string | null = null;
  session: SessionDto | null = null;
  isNewSession = false;
  loading = false;
  saving = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Session devkits and films
  sessionDevKits: DevKitWithFilms[] = [];
  unassignedFilms: FilmDto[] = [];
  allAvailableFilms: FilmDto[] = [];
  availableDevKits: DevKitDto[] = []; // DevKits that can be added to session
  availableUnassignedFilms: FilmDto[] = []; // Films that can be added to session

  // UI state
  hoveredDevKit: string | null = null;
  isViewMode = false; // New property for view mode
  isEditMode = false; // New property for edit mode
  showAddDevKitModal = false;
  showAddFilmModal = false;
  showSessionImageModal = false;
  showExpiredDevKits = false;
  selectedDevKitsForModal: string[] = [];
  selectedFilmsForModal: string[] = [];

  ngOnInit(): void {
    this.sessionRowKey = this.route.snapshot.paramMap.get('id');
    this.isNewSession = this.sessionRowKey === null;

    // Check if we're in edit mode (query parameter)
    this.isEditMode = this.route.snapshot.queryParams['edit'] === 'true';
    this.isViewMode = !this.isNewSession && !this.isEditMode;

    if (this.isNewSession) {
      // For new sessions, we don't have any data yet
      this.initializeEmptySession();
    } else {
      this.loadSessionData();
    }
  }

  private initializeEmptySession(): void {
    this.session = {
      rowKey: '',
      sessionDate: new Date().toISOString().split('T')[0],
      location: '',
      participants: '[]',
      description: '',
      usedSubstances: '[]',
      developedFilms: '[]',
      participantsList: [],
      usedSubstancesList: [],
      developedFilmsList: [],
      imageUrl: '',
      imageBase64: ''
    };
    this.sessionDevKits = [];
    this.unassignedFilms = [];
  }

  private loadSessionData(): void {
    if (!this.sessionRowKey) return;

    this.loading = true;
    forkJoin({
      session: this.sessionService.getById(this.sessionRowKey),
      allDevKits: this.devKitService.getAll(),
      allFilms: this.filmService.getAll()
    }).subscribe({
      next: (data) => {
        this.session = data.session;
        
        // Available films = all films for editing, specific films for viewing
        if (this.isEditMode) {
          this.allAvailableFilms = data.allFilms; // Show all films for editing
        } else {
          this.allAvailableFilms = data.allFilms; // Show all films for viewing (we'll filter in initializeDevKitsWithFilms)
        }
        
        // Initialize devkits with their assigned films
        this.initializeDevKitsWithFilms(data.allDevKits, data.allFilms);
        
        // Set available items for adding
        this.updateAvailableItems(data.allDevKits, data.allFilms);
        
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error loading session data';
        this.loading = false;
        console.error(err);
      }
    });
  }

  private initializeDevKitsWithFilms(allDevKits: DevKitDto[], allFilms: FilmDto[]): void {
    if (!this.session) return;

    const usedDevKitsRowKeys = this.session.usedSubstancesList || [];
    const developedFilmsRowKeys = this.session.developedFilmsList || [];

    // Create devkit containers
    this.sessionDevKits = usedDevKitsRowKeys.map(devKitRowKey => {
      const devKit = allDevKits.find(dk => dk.rowKey === devKitRowKey);
      return {
        devKit: devKit!,
        assignedFilms: [] // We'll populate this based on current assignments
      };
    }).filter(item => item.devKit); // Remove any missing devkits

    // Put appropriate films in unassigned based on mode
    if (this.isEditMode) {
      // In edit mode, show films already assigned to this session
      this.unassignedFilms = allFilms.filter(f => 
        developedFilmsRowKeys.includes(f.rowKey)
      );
    } else {
      // In view mode, show the films that were marked as developed in this session
      this.unassignedFilms = allFilms.filter(f => 
        developedFilmsRowKeys.includes(f.rowKey)
      );
    }
  }

  private updateAvailableItems(allDevKits: DevKitDto[], allFilms: FilmDto[]): void {
    if (!this.session) return;

    const usedDevKitRowKeys = this.sessionDevKits.map(sdk => sdk.devKit.rowKey);
    const sessionFilmRowKeys = [
      ...this.unassignedFilms.map(f => f.rowKey),
      ...this.sessionDevKits.flatMap(sdk => sdk.assignedFilms.map(f => f.rowKey))
    ];

    // Available devkits = all devkits minus those already in session
    this.availableDevKits = allDevKits.filter(dk => !usedDevKitRowKeys.includes(dk.rowKey));

    // Available films = all films not already in session (user can add any film)
    this.availableUnassignedFilms = allFilms.filter(f => 
      !sessionFilmRowKeys.includes(f.rowKey)
    );
  }

  // Drag and drop handlers
  onFilmDrop(event: CdkDragDrop<any[]>, targetDevKitRowKey?: string): void {
    // Only process drop if it's actually dropped in a valid container
    if (!event.isPointerOverContainer) {
      return; // Don't drop if not over a valid drop zone
    }
    
    if (event.previousContainer === event.container) {
      // Reordering within the same container
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Moving between containers - always add to the end
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.container.data.length  // Add to the end
      );
    }
  }

  // Devkit management
  addDevKitToSession(devKit: DevKitDto): void {
    const existing = this.sessionDevKits.find(sdk => sdk.devKit.rowKey === devKit.rowKey);
    if (!existing) {
      this.sessionDevKits.push({
        devKit: devKit,
        assignedFilms: []
      });
      // Update available items
      this.availableDevKits = this.availableDevKits.filter(dk => dk.rowKey !== devKit.rowKey);
    }
  }

  removeDevKitFromSession(devKitRowKey: string): void {
    const devKitIndex = this.sessionDevKits.findIndex(sdk => sdk.devKit.rowKey === devKitRowKey);
    if (devKitIndex >= 0) {
      // Move all assigned films back to unassigned
      const devKitWithFilms = this.sessionDevKits[devKitIndex];
      this.unassignedFilms.push(...devKitWithFilms.assignedFilms);
      
      // Add the devkit back to available list
      this.availableDevKits.push(devKitWithFilms.devKit);
      
      this.sessionDevKits.splice(devKitIndex, 1);
    }
  }

  // Film management
  addFilmToSession(film: FilmDto): void {
    if (!this.unassignedFilms.find(f => f.rowKey === film.rowKey)) {
      this.unassignedFilms.push(film);
      // Remove from available list
      this.availableUnassignedFilms = this.availableUnassignedFilms.filter(f => f.rowKey !== film.rowKey);
    }
  }

  removeFilmFromSession(filmRowKey: string): void {
    // Remove from unassigned films
    const filmIndex = this.unassignedFilms.findIndex(f => f.rowKey === filmRowKey);
    if (filmIndex >= 0) {
      const removedFilm = this.unassignedFilms.splice(filmIndex, 1)[0];
      this.availableUnassignedFilms.push(removedFilm);
      return;
    }
    
    // Remove from devkit assigned films
    for (const devKitWithFilms of this.sessionDevKits) {
      const assignedIndex = devKitWithFilms.assignedFilms.findIndex(f => f.rowKey === filmRowKey);
      if (assignedIndex >= 0) {
        const removedFilm = devKitWithFilms.assignedFilms.splice(assignedIndex, 1)[0];
        this.availableUnassignedFilms.push(removedFilm);
        break;
      }
    }
  }

  // Hover effects for devkit images
  onDevKitHover(devKitRowKey: string, event: MouseEvent): void {
    this.hoveredDevKit = devKitRowKey;
    const button = event.target as HTMLElement;
    const rect = button.getBoundingClientRect();
    
    // Position tooltip to the right or left of the button
    setTimeout(() => {
      const tooltip = document.querySelector('.devkit-image-tooltip') as HTMLElement;
      if (tooltip) {
        const tooltipWidth = 190; // Width of tooltip
        const rightPosition = rect.right + 10 + tooltipWidth;
        
        // Check if tooltip would exit the screen on the right
        if (rightPosition > window.innerWidth) {
          // Position on the left
          tooltip.style.left = `${rect.left - tooltipWidth - 10}px`;
        } else {
          // Position on the right
          tooltip.style.left = `${rect.right + 10}px`;
        }
        tooltip.style.top = `${rect.top}px`;
      }
    }, 0);
  }

  onDevKitLeave(): void {
    this.hoveredDevKit = null;
  }

  // Save session
  saveSession(): void {
    if (!this.session) return;

    this.saving = true;
    this.errorMessage = null;
    this.successMessage = null;

    // Get all film-to-devkit mappings
    const filmToDevKitMap: { [filmRowKey: string]: string } = {};
    
    // Map films assigned to specific devkits
    this.sessionDevKits.forEach(devKitWithFilms => {
      devKitWithFilms.assignedFilms.forEach(film => {
        filmToDevKitMap[film.rowKey] = devKitWithFilms.devKit.rowKey;
      });
    });

    // Prepare session data with proper film assignments
    const sessionData: SessionDto = {
      ...this.session,
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

    const saveOperation = this.sessionRowKey 
      ? this.sessionService.update(this.sessionRowKey, sessionData)
      : this.sessionService.add(sessionData);

    saveOperation.subscribe({
      next: (response) => {
        this.saving = false;
        this.successMessage = 'Session saved successfully!';
        
        if (!this.sessionRowKey && typeof response === 'object' && response !== null) {
          // Update the session with the returned data for new sessions
          this.session = response as SessionDto;
          this.sessionRowKey = this.session.rowKey;
          // Update the URL without navigating
          this.router.navigate(['/sessions', this.sessionRowKey], { replaceUrl: true });
        }
        
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = 'Error saving session';
        console.error(err);
      }
    });
  }

  // Navigation
  goBack(): void {
    this.router.navigate(['/sessions']);
  }

  // Toggle between view and edit mode
  toggleEditMode(): void {
    this.isEditMode = !this.isEditMode;
    this.isViewMode = !this.isEditMode;
    
    // Update URL query parameter
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { edit: this.isEditMode ? 'true' : null },
      queryParamsHandling: 'merge'
    });
    
    // Reload data to show appropriate films
    this.loadSessionData();
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
    if (input.files && input.files.length > 0 && this.session) {
      const file = input.files[0];
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        if (this.session) {
          this.session.imageUrl = reader.result as string;
          this.session.imageBase64 = reader.result as string;
        }
      };
    }
  }
}
