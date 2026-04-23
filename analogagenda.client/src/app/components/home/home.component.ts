import { CdkDragDrop, CdkDragMove, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { FilmDto, UserSettingsDto, IdeaDto } from '../../DTOs';
import { normalizeHomeSectionOrder } from '../../helpers/home-section-order.helper';
import { modalListMatches } from '../../helpers/modal-list-search.helper';
import { FilmService, UserSettingsService } from '../../services';
import { WackyIdeasSectionComponent } from './wacky-ideas-section/wacky-ideas-section.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.css',
    standalone: false
})
export class HomeComponent implements OnInit, OnDestroy {
    private filmService = inject(FilmService);
    private userSettingsService = inject(UserSettingsService);

    @ViewChild(WackyIdeasSectionComponent) wackyIdeasSection?: WackyIdeasSectionComponent;

    /**
     * CDK drop list only tracks scroll targets it knows about. Page scroll usually fires on
     * `document.documentElement` / `body`, which were missing from the default ancestor list, so
     * sorting stayed stale after scrolling. Merge those roots on each drag start (after CDK
     * resolves ancestors, before positions are cached).
     */
    private homeDropListScrollParentsSub?: Subscription;

    @ViewChild(CdkDropList)
    set homeSectionDropList(list: CdkDropList<string[]> | undefined) {
        this.homeDropListScrollParentsSub?.unsubscribe();
        this.homeDropListScrollParentsSub = undefined;
        if (!list) {
            return;
        }
        this.homeDropListScrollParentsSub = list._dropListRef.beforeStarted.subscribe(() => {
            const ref = list._dropListRef;
            const current = [...ref.getScrollableParents()];
            const merged: HTMLElement[] = [...current];
            for (const el of [document.documentElement, document.body]) {
                if (el && merged.indexOf(el) === -1) {
                    merged.push(el);
                }
            }
            if (merged.length > current.length) {
                ref.withScrollableParents(merged);
            }
        });
    }

    userSettings: UserSettingsDto | null = null;
    currentFilm: FilmDto | null = null;

    /** Render order for home widgets (normalized permutation of the five section ids). */
    homeSectionOrder: string[] = normalizeHomeSectionOrder(undefined);
    isEditingHomeLayout = false;

    // Modal state
    isChangeCurrentFilmModalOpen = false;
    availableFilms: FilmDto[] = [];
    selectedFilmId: string | null = null;
    changeCurrentFilmModalSearch = '';

    // Upsert Idea modal state
    isUpsertIdeaModalOpen = false;
    selectedIdea: IdeaDto | null = null;
    private upsertIdeaMouseDownOnOverlay = false;

    ngOnInit(): void {
        this.loadUserSettings();
    }

    ngOnDestroy(): void {
        this.homeDropListScrollParentsSub?.unsubscribe();
    }

    loadUserSettings(): void {
        this.userSettingsService.getUserSettings().subscribe({
            next: (settings) => {
                this.userSettings = settings;
                this.homeSectionOrder = normalizeHomeSectionOrder(settings.homeSectionOrder);
                if (settings.currentFilmId) {
                    this.loadCurrentFilm(settings.currentFilmId);
                } else {
                    this.currentFilm = null;
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
        this.changeCurrentFilmModalSearch = '';

        this.filmService.getMyNotDevelopedFilmsAll().subscribe({
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
        this.changeCurrentFilmModalSearch = '';
    }

    get filmsForChangeModal(): FilmDto[] {
        return this.availableFilms.filter((f) =>
            modalListMatches(
                this.changeCurrentFilmModalSearch,
                f.name,
                f.brand,
                f.type,
                f.iso
            ));
    }

    changeCurrentFilm(): void {
        if (!this.selectedFilmId || !this.userSettings) return;

        const updatedSettings: UserSettingsDto = {
            ...this.userSettings,
            currentFilmId: this.selectedFilmId,
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

    trackBySectionId(_index: number, id: string): string {
        return id;
    }

    toggleEditHomeLayout(): void {
        this.isEditingHomeLayout = !this.isEditingHomeLayout;
    }

    /**
     * While dragging in edit mode, nudge the window when the pointer hugs the top/bottom so
     * reordering long lists (e.g. last zone → first) still works with natural page scroll.
     */
    onHomeDragMoved(event: CdkDragMove): void {
        if (!this.isEditingHomeLayout) {
            return;
        }
        const y = event.pointerPosition.y;
        const margin = Math.min(96, Math.max(56, window.innerHeight * 0.12));
        const step = 40;
        const doc = document.documentElement;
        const winScrollable = doc.scrollHeight > window.innerHeight + 1;
        if (winScrollable) {
            if (y < margin) {
                window.scrollBy(0, -step);
            } else if (y > window.innerHeight - margin) {
                window.scrollBy(0, step);
            }
        }
    }

    onHomeSectionDrop(event: CdkDragDrop<string[]>): void {
        if (!this.userSettings || event.previousIndex === event.currentIndex) {
            return;
        }
        moveItemInArray(this.homeSectionOrder, event.previousIndex, event.currentIndex);
        const newOrder = [...this.homeSectionOrder];
        this.userSettingsService
            .updateUserSettings({
                userId: this.userSettings.userId,
                isSubscribed: this.userSettings.isSubscribed,
                currentFilmId: this.userSettings.currentFilmId,
                tableView: this.userSettings.tableView,
                entitiesPerPage: this.userSettings.entitiesPerPage,
                homeSectionOrder: newOrder,
            })
            .subscribe({
                next: () => {
                    this.userSettings = {
                        ...this.userSettings!,
                        homeSectionOrder: newOrder,
                    };
                },
                error: (err) => console.error('Error saving home section order:', err),
            });
    }
}
