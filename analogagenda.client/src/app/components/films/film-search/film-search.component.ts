import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DevKitService, SessionService, LocalStorageService } from '../../../services';
import { DevKitDto, SessionDto } from '../../../DTOs';

export interface SearchField {
  key: string;
  label: string;
  type: 'text' | 'number' | 'dropdown';
  visible: boolean;
  defaultVisible: boolean;
  value: any;
  options?: any[];
  availableInMyFilms: boolean;
}

export interface SearchParams {
  name?: string;
  id?: string;
  iso?: string;
  type?: string;
  numberOfExposures?: number;
  purchasedBy?: string;
  developedWithDevKitId?: string;
  developedInSessionId?: string;
}

@Component({
  selector: 'app-film-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './film-search.component.html',
  styleUrl: './film-search.component.css'
})
export class FilmSearchComponent implements OnInit, OnDestroy {
  @Input() isMyFilmsTab: boolean = false;
  @Input() initialSearchParams: SearchParams = {};
  @Output() search = new EventEmitter<SearchParams>();
  @Output() clearFilters = new EventEmitter<void>();

  private devKitService = inject(DevKitService);
  private sessionService = inject(SessionService);
  private localStorageService = inject(LocalStorageService);
  private elementRef = inject(ElementRef);

  showFieldSelector = false;
  devKits: DevKitDto[] = [];
  sessions: SessionDto[] = [];
  filmTypes = [
    { value: 'ColorNegative', label: 'Color Negative' },
    { value: 'ColorPositive', label: 'Color Positive' },
    { value: 'BlackAndWhite', label: 'Black And White' }
  ];
  usernameTypes = [
    { value: 'Angel', label: 'Angel' },
    { value: 'Cristiana', label: 'Cristiana' },
    { value: 'Tudor', label: 'Tudor' }
  ];

  searchFields: SearchField[] = [
    {
      key: 'name',
      label: 'Name',
      type: 'text',
      visible: true,
      defaultVisible: true,
      value: '',
      availableInMyFilms: true
    },
    {
      key: 'id',
      label: 'ID',
      type: 'text',
      visible: false,
      defaultVisible: false,
      value: '',
      availableInMyFilms: true
    },
    {
      key: 'iso',
      label: 'ISO',
      type: 'text',
      visible: false,
      defaultVisible: false,
      value: '',
      availableInMyFilms: true
    },
    {
      key: 'type',
      label: 'Type',
      type: 'dropdown',
      visible: false,
      defaultVisible: false,
      value: '',
      options: this.filmTypes,
      availableInMyFilms: true
    },
    {
      key: 'purchasedBy',
      label: 'Owner',
      type: 'dropdown',
      visible: false,
      defaultVisible: false,
      value: '',
      options: this.usernameTypes,
      availableInMyFilms: false
    },
    {
      key: 'developedWithDevKitId',
      label: 'Developed with DevKit',
      type: 'dropdown',
      visible: false,
      defaultVisible: false,
      value: '',
      options: [],
      availableInMyFilms: true
    },
    {
      key: 'developedInSessionId',
      label: 'Developed in Session',
      type: 'dropdown',
      visible: false,
      defaultVisible: false,
      value: '',
      options: [],
      availableInMyFilms: true
    }
  ];

  ngOnInit(): void {
    this.loadDropdownData();
    this.restoreSearchFieldsState();
    this.applyInitialSearchParams();
  }

  ngOnDestroy(): void {
    this.saveSearchFieldsState();
  }

  private loadDropdownData(): void {
    // Load DevKits
    this.devKitService.getAll().subscribe({
      next: (devKits) => {
        this.devKits = devKits;
        const kitField = this.searchFields.find(f => f.key === 'developedWithDevKitId');
        if (kitField) {
          kitField.options = devKits.map(dk => ({ value: dk.id, label: `${dk.name} - ${dk.type}` }));
        }
      },
      error: (err) => console.error('Error loading dev kits:', err)
    });

    // Load Sessions
    this.sessionService.getAll().subscribe({
      next: (sessions) => {
        this.sessions = sessions;
        const sessionField = this.searchFields.find(f => f.key === 'developedInSessionId');
        if (sessionField) {
          sessionField.options = sessions.map(s => ({ value: s.id, label: `${s.sessionDate} - ${s.location}` }));
        }
      },
      error: (err) => console.error('Error loading sessions:', err)
    });
  }

  toggleFieldSelector(): void {
    this.showFieldSelector = !this.showFieldSelector;
  }

  toggleFieldVisibility(field: SearchField): void {
    field.visible = !field.visible;
    if (!field.visible) {
      field.value = field.type === 'number' ? null : '';
    }
    this.saveSearchFieldsState();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const fieldSelector = this.elementRef.nativeElement.querySelector('.field-selector');
    
    if (fieldSelector && !fieldSelector.contains(target)) {
      this.showFieldSelector = false;
    }
  }

  @HostListener('keyup.enter')
  onEnterKey(): void {
    this.onSearch();
  }

  onSearch(): void {
    const searchParams: SearchParams = {};
    
    this.searchFields.forEach(field => {
      if (field.visible && this.hasValue(field.value)) {
        (searchParams as any)[field.key] = field.value;
      }
    });

    this.search.emit(searchParams);
  }

  onClearFilters(): void {
    this.searchFields.forEach(field => {
      field.value = field.type === 'number' ? null : '';
    });
    this.saveSearchFieldsState();
    this.clearFilters.emit();
  }

  onFieldValueChange(): void {
    this.saveSearchFieldsState();
  }

  private hasValue(value: any): boolean {
    if (value === null || value === undefined) return false;
    if (typeof value === 'string') return value.trim() !== '';
    return true;
  }

  getVisibleFields(): SearchField[] {
    return this.searchFields.filter(field => 
      field.visible && 
      (this.isMyFilmsTab ? field.availableInMyFilms : true)
    );
  }

  getAvailableFields(): SearchField[] {
    return this.searchFields.filter(field => 
      this.isMyFilmsTab ? field.availableInMyFilms : true
    );
  }

  // State persistence methods
  private getSearchFieldsStateKey(): string {
    return this.isMyFilmsTab ? 'analogagenda_myFilmsSearchFields' : 'analogagenda_allFilmsSearchFields';
  }

  private saveSearchFieldsState(): void {
    const state = {
      searchFields: this.searchFields.map(field => ({
        key: field.key,
        visible: field.visible,
        value: field.value
      }))
    };
    this.localStorageService.saveState(this.getSearchFieldsStateKey(), state);
  }

  private restoreSearchFieldsState(): void {
    const state = this.localStorageService.getState(this.getSearchFieldsStateKey());
    
    if (state && state.searchFields) {
      state.searchFields.forEach((savedField: any) => {
        const field = this.searchFields.find(f => f.key === savedField.key);
        if (field) {
          field.visible = savedField.visible;
          field.value = savedField.value;
        }
      });
    }
  }

  private applyInitialSearchParams(): void {
    if (this.initialSearchParams && Object.keys(this.initialSearchParams).length > 0) {
      Object.keys(this.initialSearchParams).forEach(key => {
        const field = this.searchFields.find(f => f.key === key);
        if (field) {
          field.visible = true;
          field.value = (this.initialSearchParams as any)[key];
        }
      });
    }
  }
}
