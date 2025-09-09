import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { DevKitDto } from "../../DTOs";
import { DevKitService } from "../../services";
import { parseISO, compareAsc } from 'date-fns';

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
    this.router.navigate(['/substances/kit']);
  }

  onKitSelected(rowKey: string): void {
    this.router.navigate(['/substances/kit/' + rowKey]);
  }


  splitAndSortDevKits(devKits: DevKitDto[]) {
    const { availableDevKits, expiredDevKits } = devKits.reduce(
      (acc, kit) => {
        if (!kit.expired) {
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
