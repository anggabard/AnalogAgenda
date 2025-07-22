import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountService } from '../../services';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private api = inject(AccountService);

  loading = false;
  errorMessage: string | null = null;

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid) return;
    const { username, password } = this.form.value;
    this.loading = true;
    this.errorMessage = null;
    this.api.login(username!, password!).subscribe({
    next: () => {
      this.loading = false;
      this.router.navigate(['/home']);
    },
    error: (err) => {
      this.loading = false;
      this.errorMessage = 'Login failed. Please check your credentials.';
    }
  });
  }

  isAuth(){
    this.api.isAuth().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }

  whoAmI(){
    this.api.whoAmI().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }

  logout(){
    this.api.logout().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }
}