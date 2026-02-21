import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { FilmDto, UserSettingsDto, IdeaDto } from '../../DTOs';
import { FilmService, UserSettingsService } from '../../services';
import { WackyIdeasSectionComponent } from './wacky-ideas-section/wacky-ideas-section.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.css',
    standalone: false
})
export class HomeComponent implements OnInit {
    private filmService = inject(FilmService);
    private userSettingsService = inject(UserSettingsService);

    @ViewChild('wackyIdeasSection') wackyIdeasSection?: WackyIdeasSectionComponent;

    userSettings: UserSettingsDto | null = null;
    currentFilm: FilmDto | null = null;

    // Modal state
    isChangeCurrentFilmModalOpen = false;
    availableFilms: FilmDto[] = [];
    selectedFilmId: string | null = null;

    // Upsert Idea modal state
    isUpsertIdeaModalOpen = false;
    selectedIdea: IdeaDto | null = null;
    private upsertIdeaMouseDownOnOverlay = false;

    ngOnInit(): void {
        this.loadUserSettings();
    }

    loadUserSettings(): void {
        this.userSettingsService.getUserSettings().subscribe({
            next: (settings) => {
                this.userSettings = settings;
                if (settings.currentFilmId) {
                    this.loadCurrentFilm(settings.currentFilmId);
                }
            },
            error: (error) => {
                console.error('Error loading user settings:', error);
            }
        });
    }

    loadCurrentFilm(filmId: string): void {
        this.filmService.getById(filmId).subscribe({
            next: (film) => {
                this.currentFilm = film;
            },
            error: (error) => {
                console.error('Error loading current film:', error);
                this.currentFilm = null;
            }
        });
    }

    openChangeCurrentFilmModal(): void {
        this.isChangeCurrentFilmModalOpen = true;
        this.selectedFilmId = null;

        this.filmService.getNotDevelopedFilms().subscribe({
            next: (films) => {
                this.availableFilms = films;
            },
            error: (error) => {
                console.error('Error loading available films:', error);
                this.availableFilms = [];
            }
        });
    }

    closeChangeCurrentFilmModal(): void {
        this.isChangeCurrentFilmModalOpen = false;
        this.selectedFilmId = null;
        this.availableFilms = [];
    }

    changeCurrentFilm(): void {
        if (!this.selectedFilmId || !this.userSettings) return;

        const updatedSettings = {
            userId: this.userSettings.userId,
            currentFilmId: this.selectedFilmId,
            isSubscribed: this.userSettings.isSubscribed,
            tableView: this.userSettings.tableView,
            entitiesPerPage: this.userSettings.entitiesPerPage
        };

        this.userSettingsService.updateUserSettings(updatedSettings).subscribe({
            next: () => {
                this.loadUserSettings();
                this.closeChangeCurrentFilmModal();
            },
            error: (error) => {
                console.error('Error changing current film:', error);
            }
        });
    }

    openAddIdeaModal(): void {
        this.selectedIdea = null;
        this.isUpsertIdeaModalOpen = true;
    }

    openEditIdeaModal(idea: IdeaDto): void {
        this.selectedIdea = idea;
        this.isUpsertIdeaModalOpen = true;
    }

    closeUpsertIdeaModal(): void {
        this.isUpsertIdeaModalOpen = false;
        this.selectedIdea = null;
    }

    onUpsertIdeaOverlayMouseDown(event: MouseEvent): void {
        const target = event.target as HTMLElement;
        if (target?.classList?.contains('modal-overlay')) {
            this.upsertIdeaMouseDownOnOverlay = true;
        }
    }

    onUpsertIdeaOverlayMouseUp(event: MouseEvent): void {
        const target = event.target as HTMLElement;
        if (this.upsertIdeaMouseDownOnOverlay && target?.classList?.contains('modal-overlay')) {
            this.closeUpsertIdeaModal();
        }
        this.upsertIdeaMouseDownOnOverlay = false;
    }

    onUpsertIdeaModalContentMouseDown(): void {
        this.upsertIdeaMouseDownOnOverlay = false;
    }

    onIdeaSaved(): void {
        this.closeUpsertIdeaModal();
        this.wackyIdeasSection?.loadIdeas();
    }

    onIdeaDeleted(): void {
        this.closeUpsertIdeaModal();
        this.wackyIdeasSection?.loadIdeas();
    }
}
