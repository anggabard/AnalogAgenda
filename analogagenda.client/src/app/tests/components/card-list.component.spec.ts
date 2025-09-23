import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, TemplateRef, ViewChild } from '@angular/core';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { FilmDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

@Component({
  template: `
    <app-card-list 
      [items]="items" 
      [cardTemplate]="cardTemplate"
      [hasMore]="hasMore" 
      [loading]="loading"
      (loadMore)="onLoadMore()"
      (itemClick)="onItemClick($event)">
    </app-card-list>
    
    <ng-template #cardTemplate let-item>
      <div class="test-card" (click)="onItemClick(item)">
        <span>{{item.name}}</span>
      </div>
    </ng-template>
  `
})
class TestHostComponent {
  @ViewChild('cardTemplate') cardTemplate!: TemplateRef<any>;
  
  items: FilmDto[] = [];
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

describe('CardListComponent', () => {
  let component: CardListComponent;
  let hostComponent: TestHostComponent;
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CardListComponent, TestHostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    
    // Get the CardListComponent instance
    const cardListDebugElement = fixture.debugElement.query(
      (debugElement) => debugElement.componentInstance instanceof CardListComponent
    );
    component = cardListDebugElement.componentInstance;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(hostComponent).toBeTruthy();
  });

  it('should display items using the provided template', () => {
    // Arrange
    hostComponent.items = [
      createMockFilm('1', 'Test Film 1'),
      createMockFilm('2', 'Test Film 2')
    ];

    // Act
    fixture.detectChanges();

    // Assert
    const cardElements = fixture.nativeElement.querySelectorAll('.test-card');
    expect(cardElements.length).toBe(2);
    expect(cardElements[0].textContent.trim()).toBe('Test Film 1');
    expect(cardElements[1].textContent.trim()).toBe('Test Film 2');
  });

  it('should show load more button when hasMore is true and not loading', () => {
    // Arrange
    hostComponent.hasMore = true;
    hostComponent.loading = false;

    // Act
    fixture.detectChanges();

    // Assert
    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    expect(loadMoreButton).toBeTruthy();
    expect(loadMoreButton.textContent.trim()).toBe('›');
  });

  it('should hide load more button when hasMore is false', () => {
    // Arrange
    hostComponent.hasMore = false;
    hostComponent.loading = false;

    // Act
    fixture.detectChanges();

    // Assert
    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    expect(loadMoreButton).toBeFalsy();
  });

  it('should show loading spinner when loading is true', () => {
    // Arrange
    hostComponent.hasMore = true;
    hostComponent.loading = true;

    // Act
    fixture.detectChanges();

    // Assert
    const loadingSpinner = fixture.nativeElement.querySelector('.loading-spinner-card');
    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    
    expect(loadingSpinner).toBeTruthy();
    expect(loadMoreButton).toBeFalsy(); // Button should be hidden when loading
  });

  it('should emit loadMore event when load more button is clicked', () => {
    // Arrange
    hostComponent.hasMore = true;
    hostComponent.loading = false;
    fixture.detectChanges();

    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');

    // Act
    loadMoreButton.click();

    // Assert
    expect(hostComponent.loadMoreCalled).toBe(true);
  });

  it('should emit itemClick event when card is clicked', () => {
    // Arrange
    const testFilm = createMockFilm('1', 'Test Film');
    hostComponent.items = [testFilm];
    fixture.detectChanges();

    const cardElement = fixture.nativeElement.querySelector('.test-card');

    // Act
    cardElement.click();

    // Assert
    expect(hostComponent.clickedItem).toEqual(testFilm);
  });

  it('should handle empty items array', () => {
    // Arrange
    hostComponent.items = [];

    // Act
    fixture.detectChanges();

    // Assert
    const cardElements = fixture.nativeElement.querySelectorAll('.test-card');
    expect(cardElements.length).toBe(0);
  });

  it('should apply proper CSS classes to container', () => {
    // Act
    fixture.detectChanges();

    // Assert
    const container = fixture.nativeElement.querySelector('.card-list');
    expect(container).toBeTruthy();
  });

  it('should have correct loading spinner structure', () => {
    // Arrange
    hostComponent.loading = true;

    // Act
    fixture.detectChanges();

    // Assert
    const spinner = fixture.nativeElement.querySelector('.loading-spinner-card .spinner');
    const spinnerBg = fixture.nativeElement.querySelector('.spinner-bg');
    const spinnerPath = fixture.nativeElement.querySelector('.spinner-path');
    
    expect(spinner).toBeTruthy();
    expect(spinnerBg).toBeTruthy();
    expect(spinnerPath).toBeTruthy();
  });

  it('should handle multiple rapid load more clicks gracefully', () => {
    // Arrange
    hostComponent.hasMore = true;
    hostComponent.loading = false;
    fixture.detectChanges();

    const loadMoreButton = fixture.nativeElement.querySelector('.load-more-btn');
    let loadMoreCallCount = 0;
    hostComponent.onLoadMore = () => { loadMoreCallCount++; };

    // Act
    loadMoreButton.click();
    loadMoreButton.click();
    loadMoreButton.click();

    // Assert
    expect(loadMoreCallCount).toBe(3); // Should handle all clicks
  });

  // Helper function to create mock films
  function createMockFilm(rowKey: string, name: string): FilmDto {
    return {
      rowKey,
      name,
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      description: 'Test film description',
      developed: false,
      imageUrl: 'test-image-url',
      imageBase64: ''
    };
  }
});
