import { Component, Input, Output, EventEmitter, TemplateRef, OnInit, inject } from '@angular/core';
import { UserSettingsService } from '../../../services';
import { UserSettingsDto } from '../../../DTOs';

@Component({
  selector: 'app-list',
  templateUrl: './list.component.html',
  styleUrl: './list.component.css',
  standalone: false
})
export class ListComponent implements OnInit {
  private userSettingsService = inject(UserSettingsService);

  @Input() items: any[] = [];
  @Input() cardTemplate!: TemplateRef<any>;
  @Input() rowTemplate?: TemplateRef<any>;
  @Input() columnHeaders: string[] = [];
  @Input() hasMore: boolean = false;
  @Input() loading: boolean = false;
  @Input() listClass: string = '';

  @Output() loadMore = new EventEmitter<void>();
  @Output() itemClick = new EventEmitter<any>();

  tableView = false;
  settingsLoaded = false;

  ngOnInit(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings: UserSettingsDto) => {
        this.tableView = settings.tableView ?? false;
        this.settingsLoaded = true;
      },
      error: () => {
        this.tableView = false;
        this.settingsLoaded = true;
      }
    });
  }

  get useTableView(): boolean {
    return this.settingsLoaded && this.tableView && !!this.rowTemplate;
  }

  onLoadMore(): void {
    this.loadMore.emit();
  }

  onItemClick(item: any): void {
    this.itemClick.emit(item);
  }
}
