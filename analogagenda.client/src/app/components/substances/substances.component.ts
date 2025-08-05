import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { DevKitDto } from "../../DTOs";
import { DevKitService } from "../../services";
import { isAfter, addWeeks, parseISO, compareAsc } from 'date-fns';

@Component({
  selector: 'app-substances',
  templateUrl: './substances.component.html',
  styleUrl: './substances.component.css'
})

export class SubstancesComponent {
  private dk = inject(DevKitService);

  availableDevKits: DevKitDto[] = [];
  expiredDevKits: DevKitDto[] = [];

  constructor(private router: Router) {
    this.dk.getAllDevKits().subscribe({
      next: (devKits: DevKitDto[]) => {
        this.splitAndSortDevKits(devKits);
      }
    });
  }

  onNewKitClick() {
    this.router.navigate(['/substances/new-kit']);
  }


  splitAndSortDevKits(devKits: DevKitDto[]) {
    const today = new Date();

    const { availableDevKits, expiredDevKits } = devKits.reduce(
      (acc, kit) => {
        const mixedOnDate = parseISO(kit.mixedOn);
        const validUntil = addWeeks(mixedOnDate, kit.validForWeeks);
        const isValidByFilms = kit.filmsDeveloped < kit.validForFilms;
        const isValidByTime = isAfter(validUntil, today);

        if (isValidByFilms && isValidByTime) {
          acc.availableDevKits.push(kit);
        } else {
          acc.expiredDevKits.push(kit);
        }

        return acc;
      },
      { availableDevKits: [] as DevKitDto[], expiredDevKits: [] as DevKitDto[] }
    );

    const sortByPurchasedOn = (a: DevKitDto, b: DevKitDto) =>
      compareAsc(parseISO(a.purchasedOn), parseISO(b.purchasedOn));

    this.availableDevKits = availableDevKits.sort(sortByPurchasedOn);
    this.expiredDevKits = expiredDevKits.sort(sortByPurchasedOn);
  }
}
