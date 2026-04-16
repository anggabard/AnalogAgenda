import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ModalComponent } from '../../components/common/modal/modal.component';

describe('ModalComponent', () => {
  let fixture: ComponentFixture<ModalComponent>;
  let component: ModalComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('emits close when pointerdown and click both occur on the backdrop', () => {
    const spy = jasmine.createSpy('close');
    component.close.subscribe(spy);
    component.open = true;
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.modal-overlay') as HTMLElement;
    expect(overlay).toBeTruthy();

    overlay.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(spy).toHaveBeenCalledTimes(1);
  });

  it('does not emit close when pointerdown started inside modal content and click finishes on overlay', () => {
    const spy = jasmine.createSpy('close');
    component.close.subscribe(spy);
    component.open = true;
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.modal-overlay') as HTMLElement;
    const content = fixture.nativeElement.querySelector('.modal-content') as HTMLElement;
    expect(overlay && content).toBeTruthy();

    content.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(spy).not.toHaveBeenCalled();
  });

  it('does not emit close when only click fires on backdrop without prior pointerdown on backdrop', () => {
    const spy = jasmine.createSpy('close');
    component.close.subscribe(spy);
    component.open = true;
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.modal-overlay') as HTMLElement;
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(spy).not.toHaveBeenCalled();
  });
});
