import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { ActivatedRoute } from '@angular/router';
import { Component } from '@angular/core';
import { of } from 'rxjs';
import { PublicCollectionPageComponent } from '../../components/collections/public-collection-page/public-collection-page.component';
import { PublicCollectionService } from '../../services/implementations/public-collection.service';
import { ErrorMessageComponent } from '../../components/common/error-message/error-message.component';
import { PublicCollectionPageDto } from '../../DTOs';

@Component({ selector: 'app-photos-content', template: '', standalone: false })
class PhotosContentStubComponent {}

describe('PublicCollectionPageComponent', () => {
  let fixture: ComponentFixture<PublicCollectionPageComponent>;
  let component: PublicCollectionPageComponent;
  let mockService: jasmine.SpyObj<PublicCollectionService>;

  const unlockedPage: PublicCollectionPageDto = {
    requiresPassword: false,
    name: 'Coll',
    photos: [],
    comments: [],
    location: '',
    description: null,
  };

  beforeEach(async () => {
    mockService = jasmine.createSpyObj('PublicCollectionService', [
      'getPage',
      'verify',
      'postComment',
      'downloadAll',
      'downloadSelected',
      'downloadPhoto',
    ]);
    mockService.getPage.and.returnValue(of({ ...unlockedPage }));

    await TestBed.configureTestingModule({
      imports: [RouterTestingModule.withRoutes([]), FormsModule],
      declarations: [PublicCollectionPageComponent, ErrorMessageComponent, PhotosContentStubComponent],
      providers: [
        { provide: PublicCollectionService, useValue: mockService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'id' ? 'col1' : null),
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublicCollectionPageComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('featuredPreviewUrl uses preview path like collections cards', () => {
    expect(component.featuredPreviewUrl('')).toBe('');
    expect(component.featuredPreviewUrl('https://x.blob/photos/abc-guid')).toBe(
      'https://x.blob/photos/preview/abc-guid'
    );
    expect(component.featuredPreviewUrl('https://x.blob/photos/preview/abc-guid')).toBe(
      'https://x.blob/photos/preview/abc-guid'
    );
  });

  it('normalizes undefined photos and comments from API to empty arrays', async () => {
    mockService.getPage.and.returnValue(
      of({
        requiresPassword: false,
        name: 'Coll',
        photos: undefined as unknown as [],
        comments: undefined as unknown as [],
      } as PublicCollectionPageDto)
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.page?.photos).toEqual([]);
    expect(component.page?.comments).toEqual([]);
  });

  it('appends new comment to page when postComment succeeds', async () => {
    mockService.postComment.and.returnValue(
      of({
        id: 'c1',
        authorName: 'Visitor',
        body: 'Hello',
        createdAt: '2026-01-01T12:00:00.000Z',
      })
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.page?.comments?.length).toBe(0);

    component.commentAuthor = 'Visitor';
    component.commentBody = 'Hello';
    component.submitComment();

    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockService.postComment).toHaveBeenCalled();
    expect(component.page?.comments?.length).toBe(1);
    expect(component.page?.comments?.[0].body).toBe('Hello');
  });
});
