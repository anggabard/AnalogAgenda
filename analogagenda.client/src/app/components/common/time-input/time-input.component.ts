import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TimeHelper } from '../../../helpers/time.helper';

@Component({
  selector: 'app-time-input',
  templateUrl: './time-input.component.html',
  styleUrl: './time-input.component.css',
  standalone: false,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TimeInputComponent),
      multi: true
    }
  ]
})
export class TimeInputComponent implements ControlValueAccessor {
  @Input() placeholder: string = '0:00';
  @Input() disabled: boolean = false;
  @Input() cssClass: string = '';
  @Input() incrementSeconds: number = 15; // Default 15 seconds increment
  @Input() showValidation: boolean = false;
  @Input() isValid: boolean = true;

  @Output() timeChange = new EventEmitter<number>();

  private _value: number = 0;
  private onChange = (value: number) => {};
  public onTouched = () => {};

  get value(): number {
    return this._value;
  }

  set value(val: number) {
    this._value = val;
    this.onChange(val);
    this.timeChange.emit(val);
  }

  get displayValue(): string {
    return TimeHelper.decimalMinutesToMinSec(this._value);
  }

  onInputChange(event: any): void {
    const newValue = TimeHelper.minSecToDecimalMinutes(event.target.value);
    this.value = newValue;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.incrementTime();
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.decrementTime();
    }
  }

  private incrementTime(): void {
    const deltaMinutes = this.incrementSeconds / 60;
    const newValue = this._value + deltaMinutes;
    this.value = Math.max(0, +newValue.toFixed(4));
  }

  private decrementTime(): void {
    const deltaMinutes = this.incrementSeconds / 60;
    const newValue = this._value - deltaMinutes;
    this.value = Math.max(0, +newValue.toFixed(4));
  }

  // ControlValueAccessor implementation
  writeValue(value: number): void {
    this._value = value || 0;
  }

  registerOnChange(fn: (value: number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
