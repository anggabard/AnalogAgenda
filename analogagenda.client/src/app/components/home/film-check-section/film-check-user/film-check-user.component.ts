import { Component, Input, Output, EventEmitter } from '@angular/core';
import { FilmType } from '../../../../enums';

export interface FilmCheckCountsByType {
  [FilmType.ColorNegative]: number;
  [FilmType.ColorPositive]: number;
  [FilmType.BlackAndWhite]: number;
}

@Component({
  selector: 'tr[app-film-check-user]',
  templateUrl: './film-check-user.component.html',
  styleUrl: './film-check-user.component.css',
  standalone: false
})
export class FilmCheckUserComponent {
  @Input() userLabel = '';
  @Input() count = 0;
  @Input() countsByType: FilmCheckCountsByType = {
    [FilmType.ColorNegative]: 0,
    [FilmType.ColorPositive]: 0,
    [FilmType.BlackAndWhite]: 0
  };

  @Output() popoverShow = new EventEmitter<void>();
  @Output() popoverHide = new EventEmitter<void>();

  readonly filmTypes = [
    { type: FilmType.ColorNegative, label: FilmType.ColorNegative },
    { type: FilmType.ColorPositive, label: FilmType.ColorPositive },
    { type: FilmType.BlackAndWhite, label: FilmType.BlackAndWhite }
  ];

  showPopover = false;

  getCountForType(type: FilmType): number {
    return this.countsByType[type] ?? 0;
  }

  onEyeEnter(): void {
    this.showPopover = true;
    this.popoverShow.emit();
  }

  onEyeLeave(): void {
    this.showPopover = false;
    this.popoverHide.emit();
  }
}
