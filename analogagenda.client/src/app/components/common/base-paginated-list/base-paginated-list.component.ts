import { Component, ViewChild, TemplateRef, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { PagedResponseDto } from '../../../DTOs';

/**
 * Abstract base component for paginated list views with common patterns
 */
@Component({
    template: '' // Abstract component, no template
    ,
    standalone: false
})
export abstract class BasePaginatedListComponent<TDto> {
  protected router = inject(Router);
  
  // Common pagination state
  currentPage = 1;
  pageSize = 5;
  hasMore = false;
  loading = false;
  items: TDto[] = [];

  @ViewChild('cardTemplate') cardTemplate!: TemplateRef<any>;

  ngOnInit(): void {
    this.loadItems();
  }

  /**
   * Load items from the service - to be implemented by concrete classes
   */
  protected abstract getItemsObservable(page: number, pageSize: number): Observable<PagedResponseDto<TDto>>;

  /**
   * Get the base route for navigation - to be implemented by concrete classes
   */
  protected abstract getBaseRoute(): string;

  /**
   * Get the id from an item - to be implemented by concrete classes
   */
  protected abstract getId(item: TDto): string;

  /**
   * Load items with pagination
   */
  loadItems(): void {
    if (this.loading) return;
    
    this.loading = true;
    this.getItemsObservable(this.currentPage, this.pageSize).subscribe({
      next: (response: PagedResponseDto<TDto>) => {
        this.items.push(...response.data);
        this.hasMore = response.hasNextPage;
        this.currentPage++;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading items:', err);
        this.loading = false;
      }
    });
  }

  /**
   * Load more items (infinite scroll pattern)
   */
  loadMoreItems(): void {
    this.loadItems();
  }

  /**
   * Navigate to create new item
   */
  onNewItemClick(): void {
    this.router.navigate([`${this.getBaseRoute()}/new`]);
  }

  /**
   * Navigate to item detail
   */
  onItemSelected(item: TDto): void {
    const id = this.getId(item);
    this.router.navigate([`${this.getBaseRoute()}/${id}`]);
  }
}
