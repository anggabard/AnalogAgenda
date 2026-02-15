import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { WackyIdeasSectionComponent } from '../../components/home/wacky-ideas-section/wacky-ideas-section.component';
import { IdeaService } from '../../services';
import { TestConfig } from '../test.config';
import { IdeaDto } from '../../DTOs';

describe('WackyIdeasSectionComponent', () => {
  let component: WackyIdeasSectionComponent;
  let fixture: ComponentFixture<WackyIdeasSectionComponent>;
  let mockIdeaService: jasmine.SpyObj<IdeaService>;

  const mockIdeas: IdeaDto[] = [
    { id: 'abc', title: 'Idea One', description: 'Desc one' },
    { id: 'def', title: 'Idea Two', description: 'Desc two' }
  ];

  beforeEach(async () => {
    const ideaServiceSpy = jasmine.createSpyObj('IdeaService', ['getAll']);
    ideaServiceSpy.getAll.and.returnValue(of(mockIdeas));

    await TestConfig.configureTestBed({
      declarations: [WackyIdeasSectionComponent],
      providers: [
        { provide: IdeaService, useValue: ideaServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WackyIdeasSectionComponent);
    component = fixture.componentInstance;
    mockIdeaService = TestBed.inject(IdeaService) as jasmine.SpyObj<IdeaService>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load ideas on init', () => {
    expect(mockIdeaService.getAll).toHaveBeenCalled();
    expect(component.ideas).toEqual(mockIdeas);
  });

  it('should display idea list', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Idea One');
    expect(compiled.textContent).toContain('Idea Two');
  });

  it('should emit addIdea when Add Idea button is clicked', () => {
    spyOn(component.addIdea, 'emit');
    const compiled = fixture.nativeElement as HTMLElement;
    const addButton = compiled.querySelector('button');
    expect(addButton?.textContent?.trim()).toBe('Add Idea');
    addButton?.click();
    expect(component.addIdea.emit).toHaveBeenCalled();
  });

  it('should emit editIdea with idea when a row is clicked', () => {
    spyOn(component.editIdea, 'emit');
    const firstRow = fixture.nativeElement.querySelector('.idea-row');
    firstRow?.click();
    expect(component.editIdea.emit).toHaveBeenCalledWith(mockIdeas[0]);
  });

  it('should refresh ideas when loadIdeas is called', () => {
    const newIdeas: IdeaDto[] = [{ id: 'xyz', title: 'New Idea', description: '' }];
    mockIdeaService.getAll.and.returnValue(of(newIdeas));

    component.loadIdeas();

    expect(mockIdeaService.getAll).toHaveBeenCalledTimes(2);
    expect(component.ideas).toEqual(newIdeas);
  });
});
