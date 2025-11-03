import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TimeInputComponent } from '../../components/common/time-input/time-input.component';

describe('TimeInputComponent', () => {
  let component: TimeInputComponent;
  let fixture: ComponentFixture<TimeInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TimeInputComponent],
      imports: [FormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(TimeInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Value Management', () => {
    it('should initialize with value 0', () => {
      expect(component.value).toBe(0);
    });

    it('should update value when set', () => {
      component.value = 1.5;
      expect(component.value).toBe(1.5);
    });

    it('should emit timeChange when value changes', (done) => {
      component.timeChange.subscribe((value: number) => {
        expect(value).toBe(2.5);
        done();
      });
      component.value = 2.5;
    });
  });

  describe('Display Value', () => {
    it('should display value in min:sec format', () => {
      component.value = 1.5;
      expect(component.displayValue).toBe('1:30');
    });

    it('should display 0:00 for zero value', () => {
      component.value = 0;
      expect(component.displayValue).toBe('0:00');
    });

    it('should display 3:45 for 3.75 minutes', () => {
      component.value = 3.75;
      expect(component.displayValue).toBe('3:45');
    });
  });

  describe('Input Change', () => {
    it('should convert min:sec input to decimal minutes', () => {
      const event = { target: { value: '2:30' } };
      component.onInputChange(event);
      expect(component.value).toBe(2.5);
    });

    it('should handle 0:15 input', () => {
      const event = { target: { value: '0:15' } };
      component.onInputChange(event);
      expect(component.value).toBe(0.25);
    });

    it('should handle invalid input gracefully', () => {
      const event = { target: { value: 'invalid' } };
      component.onInputChange(event);
      expect(component.value).toBe(0);
    });
  });

  describe('Arrow Key Increment/Decrement', () => {
    it('should increment by 15 seconds (0.25 min) on ArrowUp', () => {
      component.value = 1.0;
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      spyOn(event, 'preventDefault');
      
      component.onKeyDown(event);
      
      expect(component.value).toBe(1.25);
      expect(event.preventDefault).toHaveBeenCalled();
    });

    it('should decrement by 15 seconds (0.25 min) on ArrowDown', () => {
      component.value = 1.0;
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      spyOn(event, 'preventDefault');
      
      component.onKeyDown(event);
      
      expect(component.value).toBe(0.75);
      expect(event.preventDefault).toHaveBeenCalled();
    });

    it('should not go below 0 when decrementing', () => {
      component.value = 0.1;
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      
      component.onKeyDown(event);
      
      expect(component.value).toBe(0);
    });

    it('should use custom increment seconds', () => {
      component.incrementSeconds = 30; // 30 seconds = 0.5 minutes
      component.value = 1.0;
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      
      component.onKeyDown(event);
      
      expect(component.value).toBe(1.5);
    });

    it('should not prevent default for other keys', () => {
      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      spyOn(event, 'preventDefault');
      
      component.onKeyDown(event);
      
      expect(event.preventDefault).not.toHaveBeenCalled();
    });
  });

  describe('ControlValueAccessor Implementation', () => {
    it('should write value', () => {
      component.writeValue(3.5);
      expect(component.value).toBe(3.5);
    });

    it('should handle null value', () => {
      component.writeValue(null as any);
      expect(component.value).toBe(0);
    });

    it('should handle undefined value', () => {
      component.writeValue(undefined as any);
      expect(component.value).toBe(0);
    });

    it('should register onChange callback', () => {
      const fn = jasmine.createSpy('onChange');
      component.registerOnChange(fn);
      component.value = 2.0;
      expect(fn).toHaveBeenCalledWith(2.0);
    });

    it('should register onTouched callback', () => {
      const fn = jasmine.createSpy('onTouched');
      component.registerOnTouched(fn);
      expect(component.onTouched).toBe(fn);
    });

    it('should set disabled state', () => {
      component.setDisabledState(true);
      expect(component.disabled).toBe(true);
      
      component.setDisabledState(false);
      expect(component.disabled).toBe(false);
    });
  });

  describe('Input Properties', () => {
    it('should accept placeholder input', () => {
      component.placeholder = '1:00';
      expect(component.placeholder).toBe('1:00');
    });

    it('should accept disabled input', () => {
      component.disabled = true;
      expect(component.disabled).toBe(true);
    });

    it('should accept cssClass input', () => {
      component.cssClass = 'custom-class';
      expect(component.cssClass).toBe('custom-class');
    });

    it('should accept showValidation input', () => {
      component.showValidation = true;
      expect(component.showValidation).toBe(true);
    });

    it('should accept isValid input', () => {
      component.isValid = false;
      expect(component.isValid).toBe(false);
    });
  });
});

