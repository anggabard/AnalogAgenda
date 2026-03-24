import { Component, EventEmitter, HostBinding, Input, Output } from '@angular/core';

@Component({
  selector: 'app-quantity-stepper',
  templateUrl: './quantity-stepper.component.html',
  styleUrl: './quantity-stepper.component.css',
  standalone: false
})
export class QuantityStepperComponent {
  @Input() value = 1;
  @Input() min = 1;
  @Input() max = 100;
  @Input() disabled = false;
  /** solid: readable on light backgrounds; glass: translucent for dark toolbars */
  @Input() theme: 'solid' | 'glass' = 'solid';

  @Output() valueChange = new EventEmitter<number>();

  @HostBinding('class')
  get hostClasses(): string {
    return `quantity-stepper-root quantity-stepper-root--${this.theme}`;
  }

  decrement(): void {
    if (this.disabled || this.value <= this.min) return;
    this.valueChange.emit(this.value - 1);
  }

  increment(): void {
    if (this.disabled || this.value >= this.max) return;
    this.valueChange.emit(this.value + 1);
  }
}
