import { Component, inject, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService } from "../../services";
import { FilmDto, IdentityDto } from "../../DTOs";

@Component({
  selector: 'app-films',
  templateUrl: './films.component.html',
  styleUrl: './films.component.css'
})

export class FilmsComponent implements OnInit {
  private router = inject(Router);
  private filmService = inject(FilmService);
  private accountService = inject(AccountService);

  allFilms: FilmDto[] = [];
  myFilms: FilmDto[] = [];
  myDevelopedFilms: FilmDto[] = [];
  myNotDevelopedFilms: FilmDto[] = [];
  allDevelopedFilms: FilmDto[] = [];
  allNotDevelopedFilms: FilmDto[] = [];
  activeTab: 'my' | 'all' = 'my';
  currentUsername: string = '';

  ngOnInit(): void {
    this.accountService.whoAmI().subscribe({
      next: (identity: IdentityDto) => {
        this.currentUsername = identity.username;
        this.loadFilms();
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  loadFilms(): void {
    this.filmService.getAllFilms().subscribe({
      next: (films: FilmDto[]) => {
        this.allFilms = films;
        this.myFilms = films.filter(film => film.purchasedBy === this.currentUsername);
        
        // Split films into developed/not developed for all films
        this.allDevelopedFilms = films.filter(film => film.developed);
        this.allNotDevelopedFilms = films.filter(film => !film.developed);
        
        // Split films into developed/not developed for my films
        this.myDevelopedFilms = this.myFilms.filter(film => film.developed);
        this.myNotDevelopedFilms = this.myFilms.filter(film => !film.developed);
      },
      error: (err) => {
        console.error(err);
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
}
