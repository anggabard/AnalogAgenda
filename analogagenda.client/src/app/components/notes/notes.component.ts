import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrl: './notes.component.css'
})

export class NotesComponent {
  private router = inject(Router)
  
  onNewNoteClick() {
    this.router.navigate(['/notes/new']);
  }
}
