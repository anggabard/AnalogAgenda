import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, TemplateRef, ViewChild } from '@angular/core';
import { TableListComponent } from '../../components/common/table-list/table-list.component';
import { FilmDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

@Component({
  template: `
    <app-table-list
      [items]="items"
      [rowTemplate]="rowTemplate"
      [columnHeaders]="columnHeaders"
      [hasMore]="hasMore"
      [loading]="loading"
      (loadMore)="onLoadMore()"
      (itemClick)="onItemClick($event)">
    </app-table-list>

    <ng-template #rowTemplate let-item>
      <td class="test-cell">{{ item.name }}</td>
    </ng-template>
  `,
  standalone: false
})
class TestHostComponent {
  @ViewChild('rowTemplate') rowTemplate!: TemplateRef<any>;

  items: FilmDto[] = [];
  columnHeaders: string[] = ['Name'];
  hasMore = false;
  loading = false;
  loadMoreCalled = false;
  clickedItem: any = null;

  onLoadMore(): void {
    this.loadMoreCalled = true;
  }

  onItemClick(item: any): void {
    this.clickedItem = item;
  }
}

describe('TableListComponent', () => {
  let component: TableListComponent;
  let hostComponent: TestHostComponent;
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TableListComponent, TestHostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;

    const tableListDebugElement = fixture.debugElement.query(
      (debugElement) => debugElement.componentInstance instanceof TableListComponent
    );
    component = tableListDebugElement.componentInstance;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(hostComponent).toBeTruthy();
  });

  it('should display table with thead from columnHeaders', () => {
    hostComponent.columnHeaders = ['Name', 'Type'];
    fixture.detectChanges();

    const table = fixture.nativeElement.querySelector('table.table-list');
    expect(table).toBeTruthy();
    const headers = fixture.nativeElement.querySelectorAll('thead th');
    expect(headers.length).toBe(2);
    expect(headers[0].textContent?.trim()).toBe('Name');
    expect(headers[1].textContent?.trim()).toBe('Type');
  });

  it('should display items as rows using the provided row template', () => {
    hostComponent.items = [
      createMockFilm('1', 'Test Film 1'),
      createMockFilm('2', 'Test Film 2')
    ];
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr.table-list-row');
    expect(rows.length).toBe(2);
    const cells = fixture.nativeElement.querySelectorAll('.test-cell');
    expect(cells.length).toBe(2);
    expect(cells[0].textContent?.trim()).toBe('Test Film 1');
    expect(cells[1].textContent?.trim()).toBe('Test Film 2');
  });

  it('should show load more button when hasMore is true and not loading', () => {
    hostComponent.hasMore = true;
    hostComponent.loading = false;
    fixture.detectChanges();

    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    expect(loadMoreButton).toBeTruthy();
    expect(loadMoreButton.textContent?.trim()).toBe('Load more');
  });

  it('should hide load more button when hasMore is false', () => {
    hostComponent.hasMore = false;
    hostComponent.loading = false;
    fixture.detectChanges();

    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    expect(loadMoreButton).toBeFalsy();
  });

  it('should show loading spinner when loading is true', () => {
    hostComponent.hasMore = true;
    hostComponent.loading = true;
    fixture.detectChanges();

    const loadingSpinner = fixture.nativeElement.querySelector('.loading-spinner-card');
    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    expect(loadingSpinner).toBeTruthy();
    expect(loadMoreButton).toBeFalsy();
  });

  it('should emit loadMore event when load more button is clicked', () => {
    hostComponent.hasMore = true;
    hostComponent.loading = false;
    fixture.detectChanges();

    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    loadMoreButton.click();

    expect(hostComponent.loadMoreCalled).toBe(true);
  });

  it('should emit itemClick event when row is clicked', () => {
    const testFilm = createMockFilm('1', 'Test Film');
    hostComponent.items = [testFilm];
    fixture.detectChanges();

    const row = fixture.nativeElement.querySelector('tbody tr.table-list-row');
    row.click();

    expect(hostComponent.clickedItem).toEqual(testFilm);
  });

  it('should handle empty items array', () => {
    hostComponent.items = [];
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr.table-list-row');
    expect(rows.length).toBe(0);
  });

  it('should apply table-list class to wrapper', () => {
    fixture.detectChanges();
    const wrapper = fixture.nativeElement.querySelector('.table-list-wrapper');
    expect(wrapper).toBeTruthy();
  });

  it('should have correct loading spinner structure', () => {
    hostComponent.loading = true;
    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.loading-spinner-card .spinner');
    const spinnerBg = fixture.nativeElement.querySelector('.spinner-bg');
    const spinnerPath = fixture.nativeElement.querySelector('.spinner-path');
    expect(spinner).toBeTruthy();
    expect(spinnerBg).toBeTruthy();
    expect(spinnerPath).toBeTruthy();
  });

  function createMockFilm(id: string, name: string): FilmDto {
    return {
      id,
      name,
      brand: name,
      iso: '400',
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      description: 'Test film description',
      developed: false,
      imageUrl: 'test-image-url',
    };
  }
});
