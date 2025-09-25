import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { BaseUpsertComponent } from '../../common/base-upsert/base-upsert.component';
import { FilmService, PhotoService } from '../../../services';
import { FilmType, UsernameType } from '../../../enums';
import { FilmDto, PhotoBulkUploadDto, PhotoUploadDto } from '../../../DTOs';
import { FileUploadHelper } from '../../../helpers/file-upload.helper';
import { DateHelper } from '../../../helpers/date.helper';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';

@Component({
  selector: 'app-upsert-film',
  templateUrl: './upsert-film.component.html',
  styleUrl: './upsert-film.component.css'
})
export class UpsertFilmComponent extends BaseUpsertComponent<FilmDto> {

  constructor(private filmService: FilmService, private photoService: PhotoService) {
    super();
  }

  // Component-specific properties
  filmTypeOptions = Object.values(FilmType);
  purchasedByOptions = Object.values(UsernameType);

  protected createForm(): FormGroup {
    return this.fb.group({
      name: ['', Validators.required],
      iso: [400, [Validators.required, Validators.min(1)]],
      type: [FilmType.ColorNegative, Validators.required],
      numberOfExposures: [36, [Validators.required, Validators.min(1)]],
      cost: [0, [Validators.required, Validators.min(0)]],
      purchasedBy: ['', Validators.required],
      purchasedOn: [DateHelper.getTodayForInput(), Validators.required],
      imageUrl: [''],
      imageBase64: [''],
      description: [''],
      developed: [false, Validators.required]
    });
  }

  protected getCreateObservable(item: FilmDto): Observable<any> {
    return this.filmService.add(item);
  }

  protected getUpdateObservable(rowKey: string, item: FilmDto): Observable<any> {
    return this.filmService.update(rowKey, item);
  }

  protected getDeleteObservable(rowKey: string): Observable<any> {
    return this.filmService.deleteById(rowKey);
  }

  protected getItemObservable(rowKey: string): Observable<FilmDto> {
    return this.filmService.getById(rowKey);
  }

  protected getBaseRoute(): string {
    return '/films';
  }

  protected getEntityName(): string {
    return 'Film';
  }

  // Film-specific functionality (photo uploads) - now using helper
  async onUploadPhotos(): Promise<void> {
    try {
      const files = await FileUploadHelper.selectFiles(true, 'image/*');
      if (!files) return; // User cancelled

      await this.processPhotoUploads(files);
    } catch (error) {
      this.loading = false;
      this.errorMessage = ErrorHandlingHelper.handleError(error, 'photo file selection');
    }
  }

  private async processPhotoUploads(files: FileList): Promise<void> {
    this.loading = true;
    this.errorMessage = null;
    
    try {
      // Validate files before processing
      const validation = FileUploadHelper.validateFiles(files);
      if (!validation.isValid) {
        this.errorMessage = validation.errors.join('; ');
        this.loading = false;
        return;
      }

      // Convert files to photo DTOs using helper
      const photos = await FileUploadHelper.filesToPhotoUploadDtos(
        files, 
        (imageBase64) => ({ imageBase64 } as PhotoUploadDto)
      );

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
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'photo upload');
        }
      });
    } catch (error) {
      this.loading = false;
      this.errorMessage = ErrorHandlingHelper.handleError(error, 'photo processing');
    }
  }

  onViewPhotos(): void {
    this.router.navigate(['/films', this.rowKey, 'photos']);
  }
}
