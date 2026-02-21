import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { UpsertIdeaComponent } from '../../components/home/wacky-ideas-section/upsert-idea/upsert-idea.component';
import { IdeaService } from '../../services';
import { TestConfig } from '../test.config';
import { IdeaDto } from '../../DTOs';

describe('UpsertIdeaComponent', () => {
  let component: UpsertIdeaComponent;
  let fixture: ComponentFixture<UpsertIdeaComponent>;
  let mockIdeaService: jasmine.SpyObj<IdeaService>;

  beforeEach(async () => {
    const ideaServiceSpy = jasmine.createSpyObj('IdeaService', ['add', 'update', 'deleteById']);
    ideaServiceSpy.add.and.returnValue(of({ id: 'new1', title: 'New', description: '' }));
    ideaServiceSpy.update.and.returnValue(of(undefined));
    ideaServiceSpy.deleteById.and.returnValue(of(undefined));

    await TestConfig.configureTestBed({
      declarations: [UpsertIdeaComponent],
      providers: [
        { provide: IdeaService, useValue: ideaServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UpsertIdeaComponent);
    component = fixture.componentInstance;
    mockIdeaService = TestBed.inject(IdeaService) as jasmine.SpyObj<IdeaService>;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should be in create mode when idea input is null', () => {
    component.idea = null;
    fixture.detectChanges();
    expect(component.isEditMode).toBe(false);
  });

  it('should not show delete button in create mode', () => {
    component.idea = null;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const deleteBtn = compiled.querySelector('.btn-danger');
    expect(deleteBtn).toBeFalsy();
  });

  it('should be in edit mode and show delete button when idea is set', () => {
    component.idea = { id: 'abc', title: 'Test', description: 'Desc' };
    fixture.detectChanges();
    expect(component.isEditMode).toBe(true);
    const compiled = fixture.nativeElement as HTMLElement;
    const deleteBtn = compiled.querySelector('.btn-danger');
    expect(deleteBtn).toBeTruthy();
  });

  it('should prefill form in edit mode', () => {
    component.idea = { id: 'abc', title: 'My Idea', description: 'My desc' };
    fixture.detectChanges();
    expect(component.form.get('title')?.value).toBe('My Idea');
    expect(component.form.get('description')?.value).toBe('My desc');
  });

  it('should call add and emit saved when submitting in create mode', () => {
    spyOn(component.saved, 'emit');
    component.idea = null;
    fixture.detectChanges();
    component.form.patchValue({ title: 'New Idea', description: 'New desc' });

    component.submit();

    expect(mockIdeaService.add).toHaveBeenCalled();
    expect(component.saved.emit).toHaveBeenCalled();
  });

  it('should call update and emit saved when submitting in edit mode', () => {
    spyOn(component.saved, 'emit');
    component.idea = { id: 'abc', title: 'Old', description: 'Old desc' };
    fixture.detectChanges();
    component.form.patchValue({ title: 'Updated', description: 'Updated desc' });

    component.submit();

    expect(mockIdeaService.update).toHaveBeenCalledWith('abc', jasmine.any(Object));
    expect(component.saved.emit).toHaveBeenCalled();
  });

  it('should call deleteById and emit deleted in edit mode when delete is confirmed', () => {
    spyOn(component.deleted, 'emit');
    spyOn(window, 'confirm').and.returnValue(true);
    component.idea = { id: 'abc', title: 'To Delete', description: '' };
    fixture.detectChanges();

    component.onDelete();

    expect(mockIdeaService.deleteById).toHaveBeenCalledWith('abc');
    expect(component.deleted.emit).toHaveBeenCalledWith('abc');
  });
});
