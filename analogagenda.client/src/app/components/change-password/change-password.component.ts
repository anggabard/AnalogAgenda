import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService } from '../../services';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css'
})
export class ChangePasswordComponent {
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private api = inject(AccountService);


  loading = false;
  errorMessage: string | null = null;

  form = this.fb.group({
    oldPassword: ['', Validators.required],
    newPassword: ['', Validators.required],
    newPasswordConfirmation: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid) return;
    const { oldPassword, newPassword, newPasswordConfirmation } = this.form.value;

    if (newPassword !== newPasswordConfirmation) {
      this.errorMessage = 'New password does not match confirmation.';
      return;
    }

    if (oldPassword == newPassword) {
      this.errorMessage = 'New password cannot be the same as the old one.';
      return;
    }

    this.loading = true;
    this.errorMessage = null;

    this.api.changePassword(oldPassword!, newPassword!).subscribe({
      next: () => {
        this.loading = false;
        this.api.logout().subscribe({ next: () => this.router.navigate(['/login']) });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Password changing failed. Please check your credentials.';
      }
    });
  }

}
