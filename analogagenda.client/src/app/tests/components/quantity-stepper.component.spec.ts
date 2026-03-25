import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuantityStepperComponent } from '../../components/common';

describe('QuantityStepperComponent', () => {
  let component: QuantityStepperComponent;
  let fixture: ComponentFixture<QuantityStepperComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [QuantityStepperComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(QuantityStepperComponent);
    component = fixture.componentInstance;
    component.value = 3;
    component.min = 1;
    component.max = 10;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('applies theme class on host', () => {
    component.theme = 'glass';
    fixture.detectChanges();
    expect(fixture.nativeElement.className).toContain('quantity-stepper-root--glass');

    component.theme = 'solid';
    fixture.detectChanges();
    expect(fixture.nativeElement.className).toContain('quantity-stepper-root--solid');
  });

  it('disables decrement at min and increment at max', () => {
    component.value = 1;
    fixture.detectChanges();
    const btns = fixture.nativeElement.querySelectorAll('.stepper-btn');
    expect(btns[0].disabled).toBeTrue();
    expect(btns[1].disabled).toBeFalse();

    component.value = 10;
    fixture.detectChanges();
    expect(btns[0].disabled).toBeFalse();
    expect(btns[1].disabled).toBeTrue();
  });

  it('emits valueChange on increment and decrement', () => {
    const spy = jasmine.createSpy('valueChange');
    component.valueChange.subscribe(spy);

    component.increment();
    expect(spy).toHaveBeenCalledWith(4);

    component.value = 5;
    component.decrement();
    expect(spy).toHaveBeenCalledWith(4);
  });

  it('does not emit when at max or min', () => {
    const spy = jasmine.createSpy('valueChange');
    component.valueChange.subscribe(spy);

    component.value = 10;
    component.increment();
    expect(spy).not.toHaveBeenCalled();

    component.value = 1;
    component.decrement();
    expect(spy).not.toHaveBeenCalled();
  });

  it('honors disabled input — no emit and buttons disabled', () => {
    component.disabled = true;
    component.value = 5;
    fixture.detectChanges();

    const btns = fixture.nativeElement.querySelectorAll('.stepper-btn');
    expect(btns[0].disabled).toBeTrue();
    expect(btns[1].disabled).toBeTrue();
    expect(fixture.nativeElement.className).toContain('quantity-stepper-root--disabled');

    const spy = jasmine.createSpy('valueChange');
    component.valueChange.subscribe(spy);
    component.increment();
    component.decrement();
    expect(spy).not.toHaveBeenCalled();
  });
});
