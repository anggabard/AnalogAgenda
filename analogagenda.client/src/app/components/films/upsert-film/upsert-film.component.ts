import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountService, FilmService, PhotoService } from '../../../services';
import { FilmType, UsernameType } from '../../../enums';
import { FilmDto, IdentityDto, PhotoBulkUploadDto, PhotoUploadDto } from '../../../DTOs';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-upsert-film',
  templateUrl: './upsert-film.component.html',
  styleUrl: './upsert-film.component.css'
})
export class UpsertFilmComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private filmService = inject(FilmService);
  private photoService = inject(PhotoService);
  private api = inject(AccountService);

  rowKey: string | null;
  isInsert: boolean = false;
  originalName: string = '';
  isDeleteModalOpen: boolean = false;
  errorMessage: string | null = null;
  loading = false;

  form = this.fb.group({
    name: ['', Validators.required],
    iso: [400, [Validators.required, Validators.min(1)]],
    type: [FilmType.ColorNegative, Validators.required],
    numberOfExposures: [36, [Validators.required, Validators.min(1)]],
    cost: [0, [Validators.required, Validators.min(0)]],
    purchasedBy: ['', Validators.required],
    purchasedOn: [new Date().toISOString().split('T')[0], Validators.required],
    imageUrl: [''],
    imageBase64: [''],
    description: [''],
    developed: [false, Validators.required]
  });

  filmTypeOptions = Object.values(FilmType);
  purchasedByOptions = Object.values(UsernameType);

  constructor() {
    this.rowKey = this.route.snapshot.paramMap.get('id');
    this.isInsert = this.rowKey == null;

    if (this.isInsert) {
      this.api.whoAmI().subscribe({ next: (response: IdentityDto) => this.form.patchValue({ purchasedBy: response.username }) });
    } else {
      this.filmService.getFilm(this.rowKey!).subscribe({
        next: (response: FilmDto) => {
          this.form.patchValue(response);
          this.originalName = response.name;
        }
      });
    }
  }

  submit() {
    if (this.form.invalid) return;

    const formData = this.form.value as FilmDto;
    this.loading = true;
    this.errorMessage = null;

    if (this.isInsert) {
      this.filmService.addNewFilm(formData).subscribe({
        next: () => {
          this.loading = false;
          this.router.navigate(['/films']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = 'There was an error saving the new Film.';
        }
      });
    } else {
      this.filmService.updateFilm(this.rowKey!, formData).subscribe({
        next: () => {
          this.loading = false;
          this.router.navigate(['/films']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = 'There was an error updating the Film.';
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
    this.filmService.deleteFilm(this.rowKey!).subscribe({
      next: () => {
        this.router.navigate(['/films']);
      },
      error: (err) => {
        this.errorMessage = 'There was an error deleting the Film.';
      }
    });
  }

  onUploadPhotos() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.multiple = true;
    fileInput.accept = 'image/*';
    
    fileInput.onchange = (event: any) => {
      const files = event.target.files;
      if (files && files.length > 0) {
        this.processPhotoUploads(files);
      }
    };
    
    fileInput.click();
  }

  private processPhotoUploads(files: FileList) {
    this.loading = true;
    this.errorMessage = null;
    
    const photos: PhotoUploadDto[] = [];
    let processedCount = 0;
    
    Array.from(files).forEach((file, index) => {
      const reader = new FileReader();
      reader.onload = () => {
        photos.push({
          imageBase64: reader.result as string
        });
        
        processedCount++;
        if (processedCount === files.length) {
          // All files processed, upload them
          const uploadDto: PhotoBulkUploadDto = {
            filmRowId: this.rowKey!,
            photos: photos
          };
          
          this.photoService.uploadPhotos(uploadDto).subscribe({
            next: () => {
              this.loading = false;
              // Navigate to the film photos page
              this.router.navigate(['/films', this.rowKey, 'photos']);
            },
            error: (err) => {
              this.loading = false;
              this.errorMessage = 'There was an error uploading the photos.';
            }
          });
        }
      };
      
      reader.readAsDataURL(file);
    });
  }

  onViewPhotos() {
    this.router.navigate(['/films', this.rowKey, 'photos']);
  }
}
