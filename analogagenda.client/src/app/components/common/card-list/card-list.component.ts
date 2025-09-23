import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';

@Component({
  selector: 'app-card-list',
  templateUrl: './card-list.component.html',
  styleUrl: './card-list.component.css'
})
export class CardListComponent {
  @Input() items: any[] = [];
  @Input() cardTemplate!: TemplateRef<any>;
  @Input() hasMore: boolean = false;
  @Input() loading: boolean = false;
  @Input() listClass: string = '';

  @Output() loadMore = new EventEmitter<void>();
  @Output() itemClick = new EventEmitter<any>();

  onLoadMore(): void {
    this.loadMore.emit();
  }

  onItemClick(item: any): void {
    this.itemClick.emit(item);
  }
}
