import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css',
  standalone: false,
})
export class ModalComponent {
  @Input() open = false;

  @Output() close = new EventEmitter<void>();

  onOverlayClick(): void {
    this.close.emit();
  }
}
