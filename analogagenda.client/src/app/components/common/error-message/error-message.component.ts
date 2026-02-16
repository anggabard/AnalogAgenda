import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-error-message',
  templateUrl: './error-message.component.html',
  styleUrl: './error-message.component.css',
  standalone: false,
})
export class ErrorMessageComponent {
  @Input() message: string | null = null;
}
