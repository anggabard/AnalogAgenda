import { Injectable } from '@angular/core';
import { SessionDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class SessionService extends BasePaginatedService<SessionDto> {
  constructor() { super('Session'); }

  // Note: Basic CRUD methods are inherited from BasePaginatedService
  // Use add(), getAll(), getPaged(), getById(), update(), deleteById() directly
}
