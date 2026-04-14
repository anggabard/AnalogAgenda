import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  Output,
  EventEmitter,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { attachHorizontalWheelScroll } from '../../../helpers/horizontal-scroll-wheel.helper';

@Component({
  selector: 'app-card-list',
  templateUrl: './card-list.component.html',
  styleUrl: './card-list.component.css',
  standalone: false,
})
export class CardListComponent implements AfterViewInit, OnDestroy {
  @ViewChild('scrollHost', { read: ElementRef }) scrollHost?: ElementRef<HTMLElement>;

  @Input() items: any[] = [];
  @Input() cardTemplate!: TemplateRef<any>;
  @Input() hasMore: boolean = false;
  @Input() loading: boolean = false;
  @Input() listClass: string = '';

  @Output() loadMore = new EventEmitter<void>();
  @Output() itemClick = new EventEmitter<any>();

  private detachWheel?: () => void;

  ngAfterViewInit(): void {
    const el = this.scrollHost?.nativeElement;
    if (el) {
      this.detachWheel = attachHorizontalWheelScroll(el);
    }
  }

  ngOnDestroy(): void {
    this.detachWheel?.();
    this.detachWheel = undefined;
  }

  onLoadMore(): void {
    this.loadMore.emit();
  }

  onItemClick(item: any): void {
    this.itemClick.emit(item);
  }
}
