import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AccountService } from '../../services';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  isOpenOnMobile = false;

  constructor(
    private router: Router, 
    private accountService: AccountService
  ) {}

  toggleSidebar() {
    this.isOpenOnMobile = !this.isOpenOnMobile;
  }

  onBookClick() {
    this.router.navigate(['/notes']);
    this.closeMobileSidebar();
  }

   onHomeClick() {
    this.router.navigate(['/home']);
    this.closeMobileSidebar();
  }

   onInventoryClick() {
    this.router.navigate(['/inventory']);
    this.closeMobileSidebar();
  }

  onLogoutClick() {
    this.accountService.logout().subscribe({ next: () => this.router.navigate(['/login']), error: () => this.router.navigate(['/login']) });
  }

  private closeMobileSidebar() {
    if (window.innerWidth <= 768) {
      this.isOpenOnMobile = false;
    }
  }

}
