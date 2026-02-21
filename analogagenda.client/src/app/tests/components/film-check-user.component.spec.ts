import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FilmCheckUserComponent } from '../../components/home/film-check-section/film-check-user/film-check-user.component';
import { TestConfig } from '../test.config';
import { FilmType } from '../../enums';

describe('FilmCheckUserComponent', () => {
  let component: FilmCheckUserComponent;
  let fixture: ComponentFixture<FilmCheckUserComponent>;

  beforeEach(async () => {
    await TestConfig.configureTestBed({
      declarations: [FilmCheckUserComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmCheckUserComponent);
    component = fixture.componentInstance;
    component.userLabel = 'Angel';
    component.count = 3;
    component.countsByType = {
      [FilmType.ColorNegative]: 2,
      [FilmType.ColorPositive]: 0,
      [FilmType.BlackAndWhite]: 1
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display user label and count', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Angel');
    expect(compiled.textContent).toContain('3');
  });

  it('getCountForType should return count for given type', () => {
    expect(component.getCountForType(FilmType.ColorNegative)).toBe(2);
    expect(component.getCountForType(FilmType.ColorPositive)).toBe(0);
    expect(component.getCountForType(FilmType.BlackAndWhite)).toBe(1);
  });

  it('getCountForType should return 0 for missing type', () => {
    component.countsByType = {} as any;
    expect(component.getCountForType(FilmType.ColorNegative)).toBe(0);
  });

  it('should emit popoverShow on eye enter', () => {
    spyOn(component.popoverShow, 'emit');
    component.onEyeEnter();
    expect(component.showPopover).toBe(true);
    expect(component.popoverShow.emit).toHaveBeenCalled();
  });

  it('should emit popoverHide on eye leave', () => {
    component.showPopover = true;
    spyOn(component.popoverHide, 'emit');
    component.onEyeLeave();
    expect(component.showPopover).toBe(false);
    expect(component.popoverHide.emit).toHaveBeenCalled();
  });

  it('should show popover content when showPopover is true', () => {
    component.showPopover = true;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Color Negative');
    expect(compiled.textContent).toContain('Black and White');
  });
});
