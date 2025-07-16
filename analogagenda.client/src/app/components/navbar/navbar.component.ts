import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  isOpenOnMobile = false;

  constructor(private router: Router) {}

  toggleSidebar() {
    this.isOpenOnMobile = !this.isOpenOnMobile;
  }

  onBookClick() {
    this.router.navigate(['/notes']);
  }

   onHomeClick() {
    this.router.navigate(['/home']);
  }

   onInventoryClick() {
    this.router.navigate(['/inventory']);
  }

}
