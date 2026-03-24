import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IdeaDto } from '../../../../DTOs';
import { IdeaService } from '../../../../services';

@Component({
    selector: 'app-upsert-idea',
    templateUrl: './upsert-idea.component.html',
    styleUrl: './upsert-idea.component.css',
    standalone: false
})
export class UpsertIdeaComponent implements OnInit, OnChanges {
    private fb = inject(FormBuilder);
    private ideaService = inject(IdeaService);
    private router = inject(Router);

    @Input() idea: IdeaDto | null = null;

    @Output() saved = new EventEmitter<IdeaDto>();
    @Output() deleted = new EventEmitter<string>();

    form!: FormGroup;
    errorMessage: string | null = null;
    isSubmitting = false;
    isDeleteModalOpen = false;

    get isEditMode(): boolean {
        return this.idea != null && !!this.idea.id;
    }

    get deleteConfirmMessage(): string {
        const title = (this.idea?.title ?? '').trim();
        return title
            ? `Are you sure you want to delete the idea "${title}"? This cannot be undone.`
            : 'Are you sure you want to delete this idea? This cannot be undone.';
    }

    ngOnInit(): void {
        this.form = this.fb.group({
            title: ['', Validators.required],
            description: [''],
            outcome: ['']
        });
        this.patchFormFromIdea();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['idea'] && this.form) {
            this.patchFormFromIdea();
        }
    }

    private patchFormFromIdea(): void {
        if (this.idea) {
            this.form?.patchValue({
                title: this.idea.title,
                description: this.idea.description ?? '',
                outcome: this.idea.outcome ?? ''
            });
        } else if (this.form) {
            this.form.reset({ title: '', description: '', outcome: '' });
        }
    }

    onViewResults(): void {
        if (this.idea?.id) {
            this.router.navigate(['/idea', this.idea.id]);
        }
    }

    submit(): void {
        if (!this.form.valid || this.isSubmitting) return;

        this.errorMessage = null;
        this.isSubmitting = true;

        const dto: IdeaDto = {
            id: this.idea?.id ?? '',
            title: this.form.value.title,
            description: this.form.value.description ?? '',
            outcome: (this.form.value.outcome as string) ?? ''
        };

        const request = this.isEditMode
            ? this.ideaService.update(dto.id, dto)
            : this.ideaService.add(dto);

        request.subscribe({
            next: (result) => {
                this.isSubmitting = false;
                const savedDto: IdeaDto = this.isEditMode ? { ...dto } : (result as IdeaDto);
                this.saved.emit(savedDto);
            },
            error: (err) => {
                this.isSubmitting = false;
                this.errorMessage = err?.error?.message ?? err?.message ?? 'Failed to save idea.';
            }
        });
    }

    confirmDelete(): void {
        if (!this.isEditMode || !this.idea?.id || this.isSubmitting) return;

        this.isDeleteModalOpen = false;
        this.errorMessage = null;
        this.isSubmitting = true;

        const ideaId = this.idea.id;

        this.ideaService.deleteById(ideaId).subscribe({
            next: () => {
                this.isSubmitting = false;
                this.deleted.emit(ideaId);
            },
            error: (err) => {
                this.isSubmitting = false;
                this.errorMessage = err?.error?.message ?? err?.message ?? 'Failed to delete idea.';
            }
        });
    }
}
