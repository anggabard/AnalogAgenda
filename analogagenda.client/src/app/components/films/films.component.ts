import { Component, inject, OnInit, ViewChild, TemplateRef, OnDestroy } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService, LocalStorageService } from "../../services";
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

  @ViewChild('myFilmCardTemplate') myFilmCardTemplate!: TemplateRef<any>;
  @ViewChild('allFilmCardTemplate') allFilmCardTemplate!: TemplateRef<any>;

  allDevelopedFilms: FilmDto[] = [];
  allNotDevelopedFilms: FilmDto[] = [];
  myDevelopedFilms: FilmDto[] = [];
  myNotDevelopedFilms: FilmDto[] = [];
  activeTab: 'my' | 'all' = 'my';
  currentUsername: string = '';

  // Pagination state - separate for each list
  allDevelopedPage = 1;
  allNotDevelopedPage = 1;
  myDevelopedPage = 1;
  myNotDevelopedPage = 1;
  pageSize = 5;
  
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
    this.accountService.whoAmI().subscribe({
      next: (identity: IdentityDto) => {
        this.currentUsername = identity.username;
        // Load both "my" and "all" films
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
        this.myNotDevelopedFilms.push(...response.data);
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
        this.allNotDevelopedFilms.push(...response.data);
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



  onNewFilmClick() {
    this.router.navigate(['/films/new']);
  }

  onFilmSelected(id: string) {
    this.router.navigate(['/films/' + rowKey]);
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
