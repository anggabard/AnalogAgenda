import { Component, OnInit, inject } from '@angular/core';
import { of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { FilmService, UserSettingsService } from '../../../services';
import { UsernameType, FilmType } from '../../../enums';
import { FilmCheckCountsByType } from './film-check-user/film-check-user.component';

export interface FilmCheckRow {
  user: string;
  count: number;
  countsByType: FilmCheckCountsByType;
}

function emptyCountsByType(): FilmCheckCountsByType {
  return {
    [FilmType.ColorNegative]: 0,
    [FilmType.ColorPositive]: 0,
    [FilmType.BlackAndWhite]: 0
  };
}

@Component({
  selector: 'app-film-check-section',
  templateUrl: './film-check-section.component.html',
  styleUrl: './film-check-section.component.css',
  standalone: false
})
export class FilmCheckSectionComponent implements OnInit {
  private filmService = inject(FilmService);
  private userSettingsService = inject(UserSettingsService);

  displayRows: FilmCheckRow[] = [];
  popoverOpen = false;

  ngOnInit(): void {
    this.loadUserStats();
  }

  loadUserStats(): void {
    this.userSettingsService.getSubscribedUsers().pipe(
      switchMap((subscribedUsers) => {
        const subscribedUserEnums = subscribedUsers
          .map(u => u.username)
          .filter(username => Object.values(UsernameType).includes(username as UsernameType)) as string[];

        if (subscribedUserEnums.length === 0) {
          return of([] as FilmCheckRow[]);
        }

        return this.filmService.getNotDevelopedFilms().pipe(
          map((films) => {
            const userCountMap = new Map<string, number>();
            const userCountsByTypeMap = new Map<string, FilmCheckCountsByType>();

            subscribedUserEnums.forEach(userEnum => {
              userCountMap.set(userEnum, 0);
              userCountsByTypeMap.set(userEnum, { ...emptyCountsByType() });
            });

            films.forEach(film => {
              const user = film.purchasedBy;
              if (subscribedUserEnums.includes(user)) {
                userCountMap.set(user, (userCountMap.get(user) ?? 0) + 1);
                const counts = userCountsByTypeMap.get(user)!;
                const key = film.type as keyof FilmCheckCountsByType;
                if (key in counts) {
                  counts[key] = (counts[key] ?? 0) + 1;
                }
              }
            });

            const userRows: FilmCheckRow[] = subscribedUserEnums.map(userEnum => ({
              user: this.getUserDisplayName(userEnum),
              count: userCountMap.get(userEnum) ?? 0,
              countsByType: userCountsByTypeMap.get(userEnum) ?? emptyCountsByType()
            })).sort((a, b) => a.user.localeCompare(b.user));

            return this.appendTotalRow(userRows);
          })
        );
      })
    ).subscribe({
      next: (rows) => {
        this.displayRows = rows;
      },
      error: (error) => {
        console.error('Error loading user stats:', error);
        this.displayRows = [];
      }
    });
  }

  private appendTotalRow(userRows: FilmCheckRow[]): FilmCheckRow[] {
    if (userRows.length === 0) return [];
    const total = userRows.reduce(
      (acc, row) => ({
        user: 'Total',
        count: acc.count + row.count,
        countsByType: {
          [FilmType.ColorNegative]: acc.countsByType[FilmType.ColorNegative] + row.countsByType[FilmType.ColorNegative],
          [FilmType.ColorPositive]: acc.countsByType[FilmType.ColorPositive] + row.countsByType[FilmType.ColorPositive],
          [FilmType.BlackAndWhite]: acc.countsByType[FilmType.BlackAndWhite] + row.countsByType[FilmType.BlackAndWhite]
        } as FilmCheckCountsByType
      }),
      { user: 'Total', count: 0, countsByType: emptyCountsByType() }
    );
    return [...userRows, total];
  }

  onPopoverShow(): void {
    this.popoverOpen = true;
  }

  onPopoverHide(): void {
    this.popoverOpen = false;
  }

  private getUserDisplayName(userEnum: string): string {
    const userMap: Record<string, string> = {
      [UsernameType.Angel]: 'Angel',
      [UsernameType.Tudor]: 'Tudor',
      [UsernameType.Cristiana]: 'Cristiana'
    };
    return userMap[userEnum] || userEnum;
  }
}
