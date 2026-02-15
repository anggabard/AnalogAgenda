import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';

@Component({
  selector: 'app-table-list',
  templateUrl: './table-list.component.html',
  styleUrl: './table-list.component.css',
  standalone: false
})
export class TableListComponent {
  @Input() items: any[] = [];
  @Input() rowTemplate!: TemplateRef<any>;
  @Input() columnHeaders: string[] = [];
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
