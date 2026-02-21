import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { FilmDto, UserSettingsDto } from '../../../DTOs';

@Component({
  selector: 'app-current-film-section',
  templateUrl: './current-film-section.component.html',
  styleUrl: './current-film-section.component.css',
  standalone: false
})
export class CurrentFilmSectionComponent {
  @Input() userSettings: UserSettingsDto | null = null;
  @Input() currentFilm: FilmDto | null = null;
  @Output() changeCurrentFilmRequested = new EventEmitter<void>();

  constructor(private router: Router) {}

  editCurrentFilm(): void {
    if (this.currentFilm) {
      this.router.navigate(['/films', this.currentFilm.id]);
    }
  }

  onChangeCurrentFilm(): void {
    this.changeCurrentFilmRequested.emit();
  }
}
