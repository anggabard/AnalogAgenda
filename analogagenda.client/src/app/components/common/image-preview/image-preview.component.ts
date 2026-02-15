import { Component, ElementRef, Input, ViewChild } from '@angular/core';

@Component({
  selector: 'app-image-preview',
  templateUrl: './image-preview.component.html',
  styleUrl: './image-preview.component.css',
  standalone: false
})
export class ImagePreviewComponent {
  @Input() imageUrl: string | null | undefined = '';
  @Input() alt: string = '';
  /** When 'left', popover shows to the left of the icon (e.g. for last column to avoid clipping). */
  @Input() popoverPosition: 'left' | 'right' = 'right';

  @ViewChild('trigger') triggerRef!: ElementRef<HTMLElement>;

  showModal = false;

  openPreview(): void {
    this.showModal = true;
  }

  closePreview(): void {
    this.showModal = false;
  }

  /** Fixed position so popover is always out of flow and doesn't stretch table rows */
  popoverStyle: { [key: string]: string } = {
    position: 'fixed',
    left: '-9999px',
    top: '0',
  };

  onTriggerEnter(): void {
    if (!this.triggerRef?.nativeElement) return;
    const rect = this.triggerRef.nativeElement.getBoundingClientRect();
    const gap = 8;
    const top = rect.top + rect.height / 2;
    if (this.popoverPosition === 'left') {
      this.popoverStyle = {
        position: 'fixed',
        left: `${rect.left - gap}px`,
        top: `${top}px`,
        transform: 'translate(-100%, -50%)',
      };
    } else {
      this.popoverStyle = {
        position: 'fixed',
        left: `${rect.right + gap}px`,
        top: `${top}px`,
        transform: 'translateY(-50%)',
      };
    }
  }
}
