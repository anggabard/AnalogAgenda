import { Component, inject, OnInit, ViewChild, TemplateRef, OnDestroy, HostListener } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService, LocalStorageService, UserSettingsService } from "../../services";
import { FilmDto, IdentityDto, PagedResponseDto } from "../../DTOs";
import { SearchParams } from "./film-search/film-search.component";

@Component({
    selector: 'app-films',
    templateUrl: './films.component.html',
    styleUrl: './films.component.css',
    standalone: false
})

export class FilmsComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private filmService = inject(FilmService);
  private accountService = inject(AccountService);
  private localStorageService = inject(LocalStorageService);
  private userSettingsService = inject(UserSettingsService);

  @ViewChild('myFilmCardTemplate') myFilmCardTemplate!: TemplateRef<any>;
  @ViewChild('allFilmCardTemplate') allFilmCardTemplate!: TemplateRef<any>;
  @ViewChild('myFilmRowTemplate') myFilmRowTemplate!: TemplateRef<any>;
  @ViewChild('allFilmRowTemplate') allFilmRowTemplate!: TemplateRef<any>;

  myFilmTableHeaders = ['Title', 'Type', 'ISO', 'Photos', 'Preview'];
  allFilmTableHeaders = ['Title', 'Type', 'ISO', 'Owner', 'Photos', 'Preview'];

  allDevelopedFilms: FilmDto[] = [];
  allNotDevelopedFilms: FilmDto[] = [];
  myDevelopedFilms: FilmDto[] = [];
  myNotDevelopedFilms: FilmDto[] = [];
  activeTab: 'my' | 'all' = 'my';
  currentUsername: string = '';
  currentFilmId: string | null = null;

  // Pagination state - separate for each list
  allDevelopedPage = 1;
  allNotDevelopedPage = 1;
  myDevelopedPage = 1;
  myNotDevelopedPage = 1;
  /** Default page size when user hasn't set entitiesPerPage; backend also needs to respect requested pageSize. */
  pageSize = 10;
  
  // Has more state - separate for each list
  hasMoreAllDeveloped = false;
  hasMoreAllNotDeveloped = false;
  hasMoreMyDeveloped = false;
  hasMoreMyNotDeveloped = false;
  
  // Loading state - separate for each list
  loadingAllDeveloped = false;
  loadingAllNotDeveloped = false;
  loadingMyDeveloped = false;
  loadingMyNotDeveloped = false;

  // Search state - separate for each tab
  myFilmsSearchParams: SearchParams = {};
  allFilmsSearchParams: SearchParams = {};
  myFilmsIsSearching = false;
  allFilmsIsSearching = false;

  ngOnInit(): void {
    this.restoreState();
    this.loadUserSettings();
  }

  loadUserSettings(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings) => {
        this.currentFilmId = settings.currentFilmId || null;
        this.pageSize = Math.max(1, settings.entitiesPerPage ?? 20);
        this.accountService.whoAmI().subscribe({
          next: (identity: IdentityDto) => {
            this.currentUsername = identity.username;
            this.loadMyDevelopedFilms();
            this.loadMyNotDevelopedFilms();
            this.loadAllDevelopedFilms();
            this.loadAllNotDevelopedFilms();
          },
          error: (err) => {
            console.error(err);
          }
        });
      },
      error: (error) => {
        console.error('Error loading user settings:', error);
        this.accountService.whoAmI().subscribe({
          next: (identity: IdentityDto) => {
            this.currentUsername = identity.username;
            this.loadMyDevelopedFilms();
            this.loadMyNotDevelopedFilms();
            this.loadAllDevelopedFilms();
            this.loadAllNotDevelopedFilms();
          },
          error: (err) => {
            console.error(err);
          }
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.saveState();
  }

  // Search parameter getters
  private getMyFilmsSearchParams(): SearchParams | undefined {
    return this.myFilmsIsSearching ? this.myFilmsSearchParams : undefined;
  }

  private getAllFilmsSearchParams(): SearchParams | undefined {
    return this.allFilmsIsSearching ? this.allFilmsSearchParams : undefined;
  }

  // Methods for loading "My Films" tab data
  loadMyDevelopedFilms(): void {
    if (this.loadingMyDeveloped) return;
    
    this.loadingMyDeveloped = true;
    const searchParams = this.getMyFilmsSearchParams();
    this.filmService.getMyDevelopedFilmsPaged(this.myDevelopedPage, this.pageSize, searchParams).subscribe({
      next: (response: PagedResponseDto<FilmDto>) => {
        this.myDevelopedFilms.push(...response.data);
        this.hasMoreMyDeveloped = response.hasNextPage;
        this.myDevelopedPage++;
        this.loadingMyDeveloped = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingMyDeveloped = false;
      }
    });
  }

  loadMyNotDevelopedFilms(): void {
    if (this.loadingMyNotDeveloped) return;
    
    this.loadingMyNotDeveloped = true;
    const searchParams = this.getMyFilmsSearchParams();
    this.filmService.getMyNotDevelopedFilmsPaged(this.myNotDevelopedPage, this.pageSize, searchParams).subscribe({
      next: (response: PagedResponseDto<FilmDto>) => {
        const newFilms = response.data;
        this.myNotDevelopedFilms.push(...newFilms);
        this.sortFilmsWithCurrentFirst(this.myNotDevelopedFilms);
        this.hasMoreMyNotDeveloped = response.hasNextPage;
        this.myNotDevelopedPage++;
        this.loadingMyNotDeveloped = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingMyNotDeveloped = false;
      }
    });
  }

  // Methods for loading "All Films" tab data
  loadAllDevelopedFilms(): void {
    if (this.loadingAllDeveloped) return;
    
    this.loadingAllDeveloped = true;
    const searchParams = this.getAllFilmsSearchParams();
    this.filmService.getDevelopedFilmsPaged(this.allDevelopedPage, this.pageSize, searchParams).subscribe({
      next: (response: PagedResponseDto<FilmDto>) => {
        this.allDevelopedFilms.push(...response.data);
        this.hasMoreAllDeveloped = response.hasNextPage;
        this.allDevelopedPage++;
        this.loadingAllDeveloped = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingAllDeveloped = false;
      }
    });
  }

  loadAllNotDevelopedFilms(): void {
    if (this.loadingAllNotDeveloped) return;
    
    this.loadingAllNotDeveloped = true;
    const searchParams = this.getAllFilmsSearchParams();
    this.filmService.getNotDevelopedFilmsPaged(this.allNotDevelopedPage, this.pageSize, searchParams).subscribe({
      next: (response: PagedResponseDto<FilmDto>) => {
        const newFilms = response.data;
        this.allNotDevelopedFilms.push(...newFilms);
        this.sortFilmsWithCurrentFirst(this.allNotDevelopedFilms);
        this.hasMoreAllNotDeveloped = response.hasNextPage;
        this.allNotDevelopedPage++;
        this.loadingAllNotDeveloped = false;
      },
      error: (err) => {
        console.error(err);
        this.loadingAllNotDeveloped = false;
      }
    });
  }

  private sortFilmsWithCurrentFirst(films: FilmDto[]): void {
    if (!this.currentFilmId) return;
    
    const currentFilmIndex = films.findIndex(f => f.id === this.currentFilmId);
    if (currentFilmIndex > 0) {
      const currentFilm = films.splice(currentFilmIndex, 1)[0];
      films.unshift(currentFilm);
    }
  }



  onNewFilmClick() {
    this.router.navigate(['/films/new']);
  }

  onFilmSelected(id: string) {
    this.router.navigate(['/films/' + id]);
  }

  setActiveTab(tab: 'my' | 'all') {
    this.activeTab = tab;
    this.saveState();
  }

  // Search methods
  onSearch(searchParams: SearchParams): void {
    if (this.activeTab === 'my') {
      this.myFilmsIsSearching = true;
      this.myFilmsSearchParams = searchParams;
      this.resetMyFilmsPagination();
      this.clearMyFilms();
      this.loadMyDevelopedFilms();
      this.loadMyNotDevelopedFilms();
    } else {
      this.allFilmsIsSearching = true;
      this.allFilmsSearchParams = searchParams;
      this.resetAllFilmsPagination();
      this.clearAllFilms();
      this.loadAllDevelopedFilms();
      this.loadAllNotDevelopedFilms();
    }
    this.saveState();
  }

  onClearFilters(): void {
    if (this.activeTab === 'my') {
      this.myFilmsIsSearching = false;
      this.myFilmsSearchParams = {};
      this.resetMyFilmsPagination();
      this.clearMyFilms();
      this.loadMyDevelopedFilms();
      this.loadMyNotDevelopedFilms();
    } else {
      this.allFilmsIsSearching = false;
      this.allFilmsSearchParams = {};
      this.resetAllFilmsPagination();
      this.clearAllFilms();
      this.loadAllDevelopedFilms();
      this.loadAllNotDevelopedFilms();
    }
    this.saveState();
  }

  private resetMyFilmsPagination(): void {
    this.myDevelopedPage = 1;
    this.myNotDevelopedPage = 1;
  }

  private resetAllFilmsPagination(): void {
    this.allDevelopedPage = 1;
    this.allNotDevelopedPage = 1;
  }

  private clearMyFilms(): void {
    this.myDevelopedFilms = [];
    this.myNotDevelopedFilms = [];
  }

  private clearAllFilms(): void {
    this.allDevelopedFilms = [];
    this.allNotDevelopedFilms = [];
  }



  // Update existing load more methods to use search if active
  loadMoreMyDevelopedFilms(): void {
    this.loadMyDevelopedFilms();
  }

  loadMoreMyNotDevelopedFilms(): void {
    this.loadMyNotDevelopedFilms();
  }

  loadMoreAllDevelopedFilms(): void {
    this.loadAllDevelopedFilms();
  }

  loadMoreAllNotDevelopedFilms(): void {
    this.loadAllNotDevelopedFilms();
  }

  /** Infinite scroll: load more when user scrolls near the bottom (same threshold as Photos page). */
  @HostListener('window:scroll', [])
  onWindowScroll(): void {
    const threshold = 300;
    const pos = window.innerHeight + window.scrollY;
    const max = document.body.offsetHeight - threshold;
    if (pos < max) return;
    if (this.activeTab === 'all') {
      if (this.hasMoreAllNotDeveloped && !this.loadingAllNotDeveloped) this.loadMoreAllNotDevelopedFilms();
      if (this.hasMoreAllDeveloped && !this.loadingAllDeveloped) this.loadMoreAllDevelopedFilms();
    } else {
      if (this.hasMoreMyNotDeveloped && !this.loadingMyNotDeveloped) this.loadMoreMyNotDevelopedFilms();
      if (this.hasMoreMyDeveloped && !this.loadingMyDeveloped) this.loadMoreMyDevelopedFilms();
    }
  }

  // State persistence methods
  private readonly FILMS_PAGE_STATE_KEY = 'analogagenda_films_page_state';

  private saveState(): void {
    const state = {
      activeTab: this.activeTab,
      myFilmsSearchParams: this.myFilmsSearchParams,
      allFilmsSearchParams: this.allFilmsSearchParams,
      myFilmsIsSearching: this.myFilmsIsSearching,
      allFilmsIsSearching: this.allFilmsIsSearching
    };
    this.localStorageService.saveState(this.FILMS_PAGE_STATE_KEY, state);
  }

  private restoreState(): void {
    const state = this.localStorageService.getState(this.FILMS_PAGE_STATE_KEY);
    if (state) {
      this.activeTab = state.activeTab || 'my';
      this.myFilmsSearchParams = state.myFilmsSearchParams || {};
      this.allFilmsSearchParams = state.allFilmsSearchParams || {};
      this.myFilmsIsSearching = state.myFilmsIsSearching || false;
      this.allFilmsIsSearching = state.allFilmsIsSearching || false;
    }
  }
}
