import { Injectable } from '@angular/core';
import { DevKitDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class DevKitService extends BasePaginatedService<DevKitDto> {
  constructor() { super('DevKit'); }

  // Specific dev kit methods using base service patterns
  addNewKit(newKit: DevKitDto) { return this.add(newKit); }
  getAllDevKits(): Observable<DevKitDto[]> { return this.getAll(); }
  getDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> { 
    return this.getPaged(page, pageSize); 
  }
  getKit(rowKey: string): Observable<DevKitDto> { return this.getById(rowKey); }
  updateKit(rowKey: string, updateKit: DevKitDto) { return this.update(rowKey, updateKit); }
  deleteKit(rowKey: string) { return this.deleteById(rowKey); }

  // DevKit-specific filtered pagination methods
  getAvailableDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.getFilteredPaged('available', page, pageSize);
  }

  getExpiredDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.getFilteredPaged('expired', page, pageSize);
  }
}
