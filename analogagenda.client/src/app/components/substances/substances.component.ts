import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { DevKitDto } from "../../DTOs";
import { DevKitService } from "../../services";

@Component({
  selector: 'app-substances-notes',
  templateUrl: './substances.component.html',
  styleUrl: './substances.component.css'
})

export class SubstancesComponent {
  private dk = inject(DevKitService);

  availableDevKits: DevKitDto[] = [];

  constructor(private router: Router){
    this.dk.getAllDevKits().subscribe({ next: (response: DevKitDto[]) => this.availableDevKits = response});
  }
  
  onNewKitClick() {
    this.router.navigate(['/substances/new-kit']);
  }
}

