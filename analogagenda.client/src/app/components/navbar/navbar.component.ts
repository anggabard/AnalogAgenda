import { Component, EventEmitter, inject, Output } from '@angular/core';
import { Router } from '@angular/router';
import { AccountService } from '../../services';
import { IdentityDto } from '../../DTOs';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  @Output() isOpenEvent = new EventEmitter<boolean>();

  isSidebarOpen = false;
  username: string = "";

  constructor(
    private router: Router,
    private api: AccountService
  ) {
    this.api.whoAmI().subscribe({ next: (response: IdentityDto) => this.username = response.username });
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
    this.isOpenEvent.emit(this.isSidebarOpen);
  }

  onNotesClick() {
    this.router.navigate(['/notes']);
    this.closeMobileSidebar();
  }

  onHomeClick() {
    this.router.navigate(['/home']);
    this.closeMobileSidebar();
  }

  onSubstancesClick() {
    this.router.navigate(['/substances']);
    this.closeMobileSidebar();
  }

  onFilmsClick() {
    this.router.navigate(['/films']);
    this.closeMobileSidebar();
  }

  onSessionsClick() {
    this.router.navigate(['/sessions']);
    this.closeMobileSidebar();
  }

  onChangePasswordClick() {
    this.router.navigate(['/change-password']);
    this.closeMobileSidebar();
  }

  onLogoutClick() {
    this.api.logout().subscribe({ next: () => this.router.navigate(['/login']), error: () => this.router.navigate(['/login']) });
  }

  getToggleArrow() {
    return this.isSidebarOpen ? '<' : '>'
  }

  private closeMobileSidebar() {
    this.isSidebarOpen = false;
    this.isOpenEvent.emit(this.isSidebarOpen);
  }

}
