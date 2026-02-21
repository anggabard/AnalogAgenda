import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
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

    @Input() idea: IdeaDto | null = null;

    @Output() saved = new EventEmitter<IdeaDto>();
    @Output() deleted = new EventEmitter<string>();

    form!: FormGroup;
    errorMessage: string | null = null;
    isSubmitting = false;

    get isEditMode(): boolean {
        return this.idea != null && !!this.idea.id;
    }

    ngOnInit(): void {
        this.form = this.fb.group({
            title: ['', Validators.required],
            description: ['']
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
                description: this.idea.description ?? ''
            });
        } else if (this.form) {
            this.form.reset({ title: '', description: '' });
        }
    }

    submit(): void {
        if (!this.form.valid || this.isSubmitting) return;

        this.errorMessage = null;
        this.isSubmitting = true;

        const dto: IdeaDto = {
            id: this.idea?.id ?? '',
            title: this.form.value.title,
            description: this.form.value.description ?? ''
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

    onDelete(): void {
        if (!this.isEditMode || !this.idea?.id || this.isSubmitting) return;
        if (!confirm('Are you sure you want to delete this idea?')) return;

        this.errorMessage = null;
        this.isSubmitting = true;

        this.ideaService.deleteById(this.idea.id).subscribe({
            next: () => {
                this.isSubmitting = false;
                this.deleted.emit(this.idea!.id);
            },
            error: (err) => {
                this.isSubmitting = false;
                this.errorMessage = err?.error?.message ?? err?.message ?? 'Failed to delete idea.';
            }
        });
    }
}
