import { Component, OnInit, inject, Output, EventEmitter } from '@angular/core';
import { IdeaDto } from '../../../DTOs';
import { IdeaService } from '../../../services';

@Component({
    selector: 'app-wacky-ideas-section',
    templateUrl: './wacky-ideas-section.component.html',
    styleUrl: './wacky-ideas-section.component.css',
    standalone: false
})
export class WackyIdeasSectionComponent implements OnInit {
    private ideaService = inject(IdeaService);

    ideas: IdeaDto[] = [];

    @Output() addIdea = new EventEmitter<void>();
    @Output() editIdea = new EventEmitter<IdeaDto>();

    ngOnInit(): void {
        this.loadIdeas();
    }

    loadIdeas(): void {
        this.ideaService.getAll().subscribe({
            next: (ideas) => {
                this.ideas = ideas;
            },
            error: (error) => {
                console.error('Error loading ideas:', error);
                this.ideas = [];
            }
        });
    }

    onAddIdea(): void {
        this.addIdea.emit();
    }

    onEditIdea(idea: IdeaDto): void {
        this.editIdea.emit(idea);
    }
}
