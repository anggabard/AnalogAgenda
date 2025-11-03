import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AccountService } from '../../../services';
import { IdentityDto } from '../../../DTOs';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';

/**
 * Abstract base component for CRUD (Create/Update/Delete) forms with common patterns
 */
@Component({
    template: '' // Abstract component, no template
    ,
    standalone: false
})
export abstract class BaseUpsertComponent<TDto> implements OnInit {
  protected fb = inject(FormBuilder);
  protected router = inject(Router);
  protected route = inject(ActivatedRoute);
  protected accountService = inject(AccountService);

  // Common state
  id: string | null;
  isInsert: boolean = false;
  originalName: string = '';
  isDeleteModalOpen: boolean = false;
  errorMessage: string | null = null;
  loading = false;
  form!: FormGroup;

  constructor() {
    this.id = this.route.snapshot.paramMap.get('id');
    this.isInsert = this.id == null;
    
    // Initialize form first
    this.form = this.createForm();
  }

  ngOnInit(): void {
    if (this.isInsert) {
      this.initializeForCreate();
    } else {
      this.initializeForEdit();
    }
  }

  /**
   * Create the reactive form - to be implemented by concrete classes
   */
  protected abstract createForm(): FormGroup;

  /**
   * Get the service method for creating items - to be implemented by concrete classes
   */
  protected abstract getCreateObservable(item: TDto): Observable<any>;

  /**
   * Get the service method for updating items - to be implemented by concrete classes
   */
  protected abstract getUpdateObservable(id: string, item: TDto): Observable<any>;

  /**
   * Get the service method for deleting items - to be implemented by concrete classes
   */
  protected abstract getDeleteObservable(id: string): Observable<any>;

  /**
   * Get the service method for fetching single item - to be implemented by concrete classes
   */
  protected abstract getItemObservable(id: string): Observable<TDto>;

  /**
   * Get the base route for navigation - to be implemented by concrete classes
   */
  protected abstract getBaseRoute(): string;

  /**
   * Get the entity name for error messages - to be implemented by concrete classes
   */
  protected abstract getEntityName(): string;

  /**
   * Initialize form for creating new item
   */
  private initializeForCreate(): void {
    this.accountService.whoAmI().subscribe({
      next: (identity: IdentityDto) => {
        // Set current user as purchasedBy if the form has that field
        if (this.form.get('purchasedBy')) {
          this.form.patchValue({ purchasedBy: identity.username });
        }
      }
    });
  }

  /**
   * Initialize form for editing existing item
   */
  private initializeForEdit(): void {
    this.getItemObservable(this.id!).subscribe({
      next: (item: any) => {
        this.form.patchValue(item);
        this.originalName = item.name || '';
      }
    });
  }

  /**
   * Submit form (create or update)
   */
  submit(): void {
    if (this.form.invalid) return;

    const formData = this.form.value as TDto;
    this.loading = true;
    this.errorMessage = null;

    const operation$ = this.isInsert 
      ? this.getCreateObservable(formData)
      : this.getUpdateObservable(this.id!, formData);

    const actionName = this.isInsert ? 'saving' : 'updating';

    operation$.subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate([this.getBaseRoute()]);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, `${actionName} ${this.getEntityName()}`);
      }
    });
  }

  /**
   * Handle image selection
   */
  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();

      reader.readAsDataURL(file);
      reader.onload = () => {
        this.form.patchValue({ imageBase64: reader.result as string });
      };
    }
  }

  /**
   * Delete item
   */
  onDelete(): void {
    if (this.isInsert) return;

    this.getDeleteObservable(this.id!).subscribe({
      next: () => {
        this.router.navigate([this.getBaseRoute()]);
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, `deleting ${this.getEntityName()}`);
      }
    });
  }

  /**
   * Toggle delete confirmation modal
   */
  toggleDeleteModal(): void {
    this.isDeleteModalOpen = !this.isDeleteModalOpen;
  }
}
