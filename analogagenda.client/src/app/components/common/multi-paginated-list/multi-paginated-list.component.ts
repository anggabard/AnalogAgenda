import { Component, Input, Output, EventEmitter, ViewChild, TemplateRef } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResponseDto } from '../../../DTOs';

/**
 * Generic component for managing multiple paginated lists (like Films component with tabs)
 * This can handle any number of filtered lists with their own pagination state
 */
@Component({
  selector: 'app-multi-paginated-list',
  template: `
    <div class="multi-list-container">
      <!-- Tab navigation if multiple lists -->
      <div class="tabs" *ngIf="listConfigs.length > 1">
        <button 
          *ngFor="let config of listConfigs; let i = index"
          class="tab-button"
          [class.active]="activeTabIndex === i"
          (click)="setActiveTab(i)">
          {{config.tabLabel}}
        </button>
      </div>

      <!-- Active list content -->
      <div *ngIf="activeListConfig" class="list-content">
        <div class="list-header">
          <h3>{{activeListConfig.title}}</h3>
          <button class="new-item-btn" (click)="onNewItem()" *ngIf="showNewButton">
            {{newItemLabel}}
          </button>
        </div>

        <!-- Sub-lists within the active tab -->
        <div *ngFor="let subList of activeListConfig.subLists" class="sub-list">
          <h4 *ngIf="subList.title">{{subList.title}}</h4>
          
          <app-card-list
            [items]="subList.state.items"
            [cardTemplate]="cardTemplate"
            [hasMore]="subList.state.hasMore"
            [loading]="subList.state.loading"
            (loadMore)="loadMore(subList)"
            (itemClick)="onItemClick($event)">
          </app-card-list>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./multi-paginated-list.component.css']
})
export class MultiPaginatedListComponent<TDto> {
  @Input() listConfigs: ListConfig<TDto>[] = [];
  @Input() cardTemplate!: TemplateRef<any>;
  @Input() showNewButton: boolean = true;
  @Input() newItemLabel: string = 'New Item';
  
  @Output() newItemClick = new EventEmitter<void>();
  @Output() itemClick = new EventEmitter<TDto>();

  activeTabIndex = 0;

  ngOnInit(): void {
    // Load initial data for all lists
    this.listConfigs.forEach(config => {
      config.subLists.forEach(subList => {
        this.loadMore(subList);
      });
    });
  }

  get activeListConfig(): ListConfig<TDto> | undefined {
    return this.listConfigs[this.activeTabIndex];
  }

  setActiveTab(index: number): void {
    this.activeTabIndex = index;
  }

  loadMore(subList: SubListConfig<TDto>): void {
    if (subList.state.loading) return;
    
    subList.state.loading = true;
    subList.loadData(subList.state.currentPage, subList.state.pageSize).subscribe({
      next: (response: PagedResponseDto<TDto>) => {
        subList.state.items.push(...response.data);
        subList.state.hasMore = response.hasNextPage;
        subList.state.currentPage++;
        subList.state.loading = false;
      },
      error: (err) => {
        console.error('Error loading data:', err);
        subList.state.loading = false;
      }
    });
  }

  onNewItem(): void {
    this.newItemClick.emit();
  }

  onItemClick(item: TDto): void {
    this.itemClick.emit(item);
  }
}

/**
 * Configuration for a tab with multiple sub-lists
 */
export interface ListConfig<TDto> {
  tabLabel: string;
  title: string;
  subLists: SubListConfig<TDto>[];
}

/**
 * Configuration for each sub-list within a tab
 */
export interface SubListConfig<TDto> {
  title?: string;
  loadData: (page: number, pageSize: number) => Observable<PagedResponseDto<TDto>>;
  state: PaginatedListState<TDto>;
}

/**
 * State management for individual paginated lists
 */
export class PaginatedListState<TDto> {
  items: TDto[] = [];
  currentPage = 1;
  pageSize = 5;
  hasMore = false;
  loading = false;

  reset(): void {
    this.items = [];
    this.currentPage = 1;
    this.hasMore = false;
    this.loading = false;
  }
}
