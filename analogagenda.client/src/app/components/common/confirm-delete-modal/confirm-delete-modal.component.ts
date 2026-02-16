import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-delete-modal',
  templateUrl: './confirm-delete-modal.component.html',
  styleUrl: './confirm-delete-modal.component.css',
  standalone: false,
})
export class ConfirmDeleteModalComponent {
  @Input() open = false;
  @Input() title = 'Confirm Delete';
  @Input() message = '';
  @Input() confirmLabel = 'Delete';
  @Input() cancelLabel = 'Cancel';

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onOverlayClick(): void {
    this.cancel.emit();
  }

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
