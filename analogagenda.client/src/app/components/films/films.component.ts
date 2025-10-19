import { Component, inject, OnInit, ViewChild, TemplateRef } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService } from "../../services";
import { FilmDto, IdentityDto, PagedResponseDto } from "../../DTOs";
import { SearchParams } from "./film-search/film-search.component";

@Component({
    selector: 'app-films',
    templateUrl: './films.component.html',
    styleUrl: './films.component.css',
    standalone: false
})

export class FilmsComponent implements OnInit {
  private router = inject(Router);
  private filmService = inject(FilmService);
  private accountService = inject(AccountService);

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
  isSearching = false;

  ngOnInit(): void {
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

  // Methods for loading "My Films" tab data
  loadMyDevelopedFilms(): void {
    if (this.loadingMyDeveloped) return;
    
    this.loadingMyDeveloped = true;
    const searchParams = this.isSearching ? this.myFilmsSearchParams : undefined;
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
    const searchParams = this.isSearching ? this.myFilmsSearchParams : undefined;
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
    const searchParams = this.isSearching ? this.allFilmsSearchParams : undefined;
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
    const searchParams = this.isSearching ? this.allFilmsSearchParams : undefined;
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

  onFilmSelected(rowKey: string) {
    this.router.navigate(['/films/' + rowKey]);
  }

  setActiveTab(tab: 'my' | 'all') {
    this.activeTab = tab;
  }

  // Search methods
  onSearch(searchParams: SearchParams): void {
    this.isSearching = true;
    
    if (this.activeTab === 'my') {
      this.myFilmsSearchParams = searchParams;
    } else {
      this.allFilmsSearchParams = searchParams;
    }

    // Reset pagination and clear existing results
    this.resetPagination();
    this.clearResults();

    // Load films with search parameters
    this.loadFilmsWithSearch();
  }

  onClearFilters(): void {
    this.isSearching = false;
    
    if (this.activeTab === 'my') {
      this.myFilmsSearchParams = {};
    } else {
      this.allFilmsSearchParams = {};
    }

    // Reset pagination and clear results
    this.resetPagination();
    this.clearResults();

    // Load films without search
    this.loadFilmsWithoutSearch();
  }

  private resetPagination(): void {
    this.allDevelopedPage = 1;
    this.allNotDevelopedPage = 1;
    this.myDevelopedPage = 1;
    this.myNotDevelopedPage = 1;
  }

  private clearResults(): void {
    this.allDevelopedFilms = [];
    this.allNotDevelopedFilms = [];
    this.myDevelopedFilms = [];
    this.myNotDevelopedFilms = [];
  }

  private loadFilmsWithSearch(): void {
    if (this.activeTab === 'my') {
      this.loadMyDevelopedFilms();
      this.loadMyNotDevelopedFilms();
    } else {
      this.loadAllDevelopedFilms();
      this.loadAllNotDevelopedFilms();
    }
  }

  private loadFilmsWithoutSearch(): void {
    if (this.activeTab === 'my') {
      this.loadMyDevelopedFilms();
      this.loadMyNotDevelopedFilms();
    } else {
      this.loadAllDevelopedFilms();
      this.loadAllNotDevelopedFilms();
    }
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
}
