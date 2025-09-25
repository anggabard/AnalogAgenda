import { Injectable } from '@angular/core';
import { DevKitDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class DevKitService extends BasePaginatedService<DevKitDto> {
  constructor() { super('DevKit'); }

  // Note: Basic CRUD methods are inherited from BasePaginatedService
  // Use add(), getAll(), getPaged(), getById(), update(), deleteById() directly

  // DevKit-specific filtered pagination methods
  getAvailableDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.getFilteredPaged('available', page, pageSize);
  }

  getExpiredDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.getFilteredPaged('expired', page, pageSize);
  }
}
