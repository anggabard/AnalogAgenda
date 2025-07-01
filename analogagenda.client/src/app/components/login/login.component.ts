import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountService } from '../../services/account.service';

@Component({
  selector: 'app-root',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private api = inject(AccountService);

  loading = false;
  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid) return;
    const { username, password } = this.form.value;
    this.loading = true;
    this.api.login(username!, password!)
      .subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }

  secret(){
    this.api.secret().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }

  whoAmI(){
    this.api.whoAmI().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }

  logout(){
    this.api.logout().subscribe({ next: () => this.loading = false, error: () => this.loading = false });
  }
}