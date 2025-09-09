import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountService, DevKitService } from '../../../services';
import { DevKitType, UsernameType } from '../../../enums';
import { DevKitDto, IdentityDto } from '../../../DTOs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-new-kit',
  templateUrl: './new-kit.component.html',
  styleUrl: './new-kit.component.css'
})
export class NewKitComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private dk = inject(DevKitService);
  private api = inject(AccountService);

  loading = false;
  errorMessage: string | null = null;

  form = this.fb.group({
    name: ['', Validators.required],
    url: ['', [Validators.required]],
    type: [DevKitType.C41, Validators.required],
    purchasedBy: ['', Validators.required],
    purchasedOn: [new Date().toISOString().split('T')[0], Validators.required],
    mixedOn: [new Date().toISOString().split('T')[0]],
    validForWeeks: [6, Validators.required],
    validForFilms: [8, Validators.required],
    filmsDeveloped: [0, Validators.required],
    image: [''],
    description: ['']
  });

  devKitOptions = Object.values(DevKitType);
  purchasedByOptions = Object.values(UsernameType);

  constructor() {
    this.api.whoAmI().subscribe({ next: (response: IdentityDto) => this.form.patchValue({ purchasedBy: response.username }) });
  }

  submit() {
    if (this.form.invalid) return;

    const formData = this.form.value as DevKitDto;
    this.loading = true;
    this.errorMessage = null;

    this.dk.addNewKit(formData).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/substances']);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = 'There was an error saving the new Kit.';
      }
    });
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();

      reader.readAsDataURL(file);
      reader.onload = () => (this.form.patchValue({ image: reader.result as string }));
    }
  }
}
