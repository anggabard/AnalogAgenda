import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountService, DevKitService } from '../../../services';
import { DevKitType, UsernameType } from '../../../enums';
import { DevKitDto, IdentityDto } from '../../../DTOs';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-upsert-kit',
  templateUrl: './upsert-kit.component.html',
  styleUrl: './upsert-kit.component.css'
})
export class UpsertKitComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private dk = inject(DevKitService);
  private api = inject(AccountService);

  rowKey: string | null;
  isInsert: boolean = false;
  originalName: string = '';
  isDeleteModalOpen: boolean = false;
  errorMessage: string | null = null;
  loading = false;

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
    imageUrl: [''],
    imageBase64: [''],
    description: [''],
    expired: [false, Validators.required]
  });

  devKitOptions = Object.values(DevKitType);
  purchasedByOptions = Object.values(UsernameType);

  constructor() {
    this.rowKey = this.route.snapshot.paramMap.get('id');
    this.isInsert = this.rowKey == null;

    if (this.isInsert) {
      this.api.whoAmI().subscribe({ next: (response: IdentityDto) => this.form.patchValue({ purchasedBy: response.username }) });
    } else {
      this.dk.getKit(this.rowKey!).subscribe({
        next: (response: DevKitDto) => {
          this.form.patchValue(response);
          this.originalName = response.name;
        }
      });
    }
  }

  submit() {
    if (this.form.invalid) return;

    const formData = this.form.value as DevKitDto;
    this.loading = true;
    this.errorMessage = null;

    if (this.isInsert) {
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
    } else {

      this.dk.updateKit(this.rowKey!, formData).subscribe({
        next: () => {
          this.loading = false;
          this.router.navigate(['/substances']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = 'There was an error updating the Kit.';
        }
      });
    }

  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();

      reader.readAsDataURL(file);
      reader.onload = () => (this.form.patchValue({ imageBase64: reader.result as string }));
    }
  }

  onDelete() {
    this.dk.deleteKit(this.rowKey!).subscribe({
      next: () => {
        this.router.navigate(['/substances']);
      },
      error: (err) => {
        this.errorMessage = 'There was an error updating the Kit.';
      }
    });
  }
}
