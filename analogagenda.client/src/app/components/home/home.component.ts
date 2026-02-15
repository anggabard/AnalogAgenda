import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { FilmDto, UserSettingsDto, ExposureDateDto, IdeaDto } from '../../DTOs';
import { FilmService, UserSettingsService } from '../../services';
import { UsernameType } from '../../enums';
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
    private router = inject(Router);

    @ViewChild('wackyIdeasSection') wackyIdeasSection?: WackyIdeasSectionComponent;

    userSettings: UserSettingsDto | null = null;
    currentFilm: FilmDto | null = null;
    userStats: Array<{ user: string; count: number }> = [];
    
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
        this.loadUserStats();
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

    loadUserStats(): void {
        // First get all subscribed users
        this.userSettingsService.getSubscribedUsers().pipe(
            switchMap((subscribedUsers) => {
                // The username should match the enum values directly (Angel, Tudor, Cristiana)
                const subscribedUserEnums = subscribedUsers
                    .map(u => u.username)
                    .filter(username => Object.values(UsernameType).includes(username as UsernameType)) as string[];

                if (subscribedUserEnums.length === 0) {
                    return of([]);
                }

                // Get all not-developed films and calculate counts
                return this.filmService.getNotDevelopedFilms().pipe(
                    switchMap((films) => {
                        if (films.length === 0) {
                            // Return stats for all subscribed users with 0 count
                            return of(subscribedUserEnums.map(userEnum => ({
                                user: this.getUserDisplayName(userEnum),
                                count: 0
                            })));
                        }

                        // For each film, get its exposure dates to check if it's in progress
                        const exposureDateRequests = films.map(film =>
                            this.filmService.getExposureDates(film.id).pipe(
                                map((exposureDates) => ({
                                    film,
                                    hasExposureDates: exposureDates.length > 0
                                })),
                                catchError(() => of({ film, hasExposureDates: false }))
                            )
                        );

                        return forkJoin(exposureDateRequests).pipe(
                            map((results) => {
                                // Filter films that are in progress (have exposure dates)
                                const inProgressFilms = results
                                    .filter(r => r.hasExposureDates)
                                    .map(r => r.film);

                                // Group by purchasedBy and count, only for subscribed users
                                const statsMap = new Map<string, number>();
                                
                                inProgressFilms.forEach(film => {
                                    const user = film.purchasedBy;
                                    if (subscribedUserEnums.includes(user)) {
                                        const currentCount = statsMap.get(user) || 0;
                                        statsMap.set(user, currentCount + 1);
                                    }
                                });

                                // Create stats for all subscribed users, showing 0 for those without films
                                return subscribedUserEnums.map(userEnum => ({
                                    user: this.getUserDisplayName(userEnum),
                                    count: statsMap.get(userEnum) || 0
                                }));
                            })
                        );
                    })
                );
            })
        ).subscribe({
            next: (stats) => {
                // Sort by user name
                this.userStats = stats.sort((a, b) => a.user.localeCompare(b.user));
            },
            error: (error) => {
                console.error('Error loading user stats:', error);
                this.userStats = [];
            }
        });
    }

    getUserDisplayName(userEnum: string): string {
        // Map enum values to display names
        const userMap: Record<string, string> = {
            [UsernameType.Angel]: 'Angel',
            [UsernameType.Tudor]: 'Tudor',
            [UsernameType.Cristiana]: 'Cristiana'
        };
        return userMap[userEnum] || userEnum;
    }

    onSettingsChange(): void {
        if (!this.userSettings) return;

        const updatedSettings: Partial<UserSettingsDto> = {
            userId: this.userSettings.userId,
            isSubscribed: this.userSettings.isSubscribed,
            tableView: this.userSettings.tableView,
            entitiesPerPage: this.userSettings.entitiesPerPage,
            currentFilmId: this.userSettings.currentFilmId
        };

        this.userSettingsService.updateUserSettings(updatedSettings).subscribe({
            next: () => {
                // Settings updated successfully
            },
            error: (error) => {
                console.error('Error updating settings:', error);
                // Reload settings to revert changes
                this.loadUserSettings();
            }
        });
    }

    openChangeCurrentFilmModal(): void {
        this.isChangeCurrentFilmModalOpen = true;
        this.selectedFilmId = null;
        
        // Load available not-developed films
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

        const updatedSettings: Partial<UserSettingsDto> = {
            userId: this.userSettings.userId,
            currentFilmId: this.selectedFilmId,
            isSubscribed: this.userSettings.isSubscribed,
            tableView: this.userSettings.tableView,
            entitiesPerPage: this.userSettings.entitiesPerPage
        };

        this.userSettingsService.updateUserSettings(updatedSettings).subscribe({
            next: () => {
                // Reload settings to get updated current film
                this.loadUserSettings();
                this.closeChangeCurrentFilmModal();
            },
            error: (error) => {
                console.error('Error changing current film:', error);
            }
        });
    }

    editCurrentFilm(): void {
        if (this.currentFilm) {
            this.router.navigate(['/films', this.currentFilm.id]);
        }
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
