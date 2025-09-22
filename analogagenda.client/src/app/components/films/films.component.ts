import { Component, inject, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { FilmService, AccountService } from "../../services";
import { FilmDto, IdentityDto } from "../../DTOs";
import { parseISO, compareDesc } from 'date-fns';

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
        
        const sortByOwnerThenDate = (a: FilmDto, b: FilmDto) => {
          // First sort by owner (purchasedBy)
          const ownerComparison = a.purchasedBy.localeCompare(b.purchasedBy);
          if (ownerComparison !== 0) {
            return ownerComparison;
          }
          // Then sort by purchased date (newest first)
          return compareDesc(parseISO(a.purchasedOn), parseISO(b.purchasedOn));
        };

        const sortByDateNewestFirst = (a: FilmDto, b: FilmDto) => {
          return compareDesc(parseISO(a.purchasedOn), parseISO(b.purchasedOn));
        };

        this.allDevelopedFilms = films.filter(film => film.developed).sort(sortByOwnerThenDate);
        this.allNotDevelopedFilms = films.filter(film => !film.developed).sort(sortByOwnerThenDate);
        
        // Split films into developed/not developed for my films (sorted by newest first)
        this.myDevelopedFilms = this.myFilms.filter(film => film.developed).sort(sortByDateNewestFirst);
        this.myNotDevelopedFilms = this.myFilms.filter(film => !film.developed).sort(sortByDateNewestFirst);
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
