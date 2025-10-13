import { Component, inject, OnInit, ViewChild, TemplateRef } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService } from "../../services";
import { FilmDto, IdentityDto, PagedResponseDto } from "../../DTOs";

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
    this.filmService.getMyDevelopedFilmsPaged(this.myDevelopedPage, this.pageSize).subscribe({
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
    this.filmService.getMyNotDevelopedFilmsPaged(this.myNotDevelopedPage, this.pageSize).subscribe({
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
    this.filmService.getDevelopedFilmsPaged(this.allDevelopedPage, this.pageSize).subscribe({
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
    this.filmService.getNotDevelopedFilmsPaged(this.allNotDevelopedPage, this.pageSize).subscribe({
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

  // Load more methods
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


  onNewFilmClick() {
    this.router.navigate(['/films/new']);
  }

  onFilmSelected(rowKey: string) {
    this.router.navigate(['/films/' + rowKey]);
  }

  setActiveTab(tab: 'my' | 'all') {
    this.activeTab = tab;
  }
}
