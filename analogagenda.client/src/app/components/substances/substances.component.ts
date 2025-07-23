import { Component } from "@angular/core";
import { Router } from "@angular/router";

@Component({
  selector: 'app-substances-notes',
  templateUrl: './substances.component.html',
  styleUrl: './substances.component.css'
})

export class SubstancesComponent {
  constructor(private router: Router){}
  
  onNewKitClick() {
    this.router.navigate(['/substances/new-kit']);
  }
}
