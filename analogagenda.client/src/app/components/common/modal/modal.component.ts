import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css',
  standalone: false,
})
export class ModalComponent implements OnChanges {
  @Input() open = false;

  @Output() close = new EventEmitter<void>();

  /** True only if the last pointerdown on the overlay was on the backdrop (not bubbled from modal content). */
  private backdropPressStartedOnBackdrop = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open'] && !this.open) {
      this.backdropPressStartedOnBackdrop = false;
    }
  }

  onBackdropPointerDown(event: PointerEvent): void {
    this.backdropPressStartedOnBackdrop = event.target === event.currentTarget;
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target !== event.currentTarget || !this.backdropPressStartedOnBackdrop) {
      this.backdropPressStartedOnBackdrop = false;
      return;
    }
    this.backdropPressStartedOnBackdrop = false;
    this.close.emit();
  }
}
