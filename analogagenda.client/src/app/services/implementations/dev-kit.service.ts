import { Injectable } from '@angular/core';
import {
  DevKitDto,
  PagedResponseDto,
  DevKitSessionAssignmentRowDto,
  DevKitFilmAssignmentRowDto,
  IdListDto
} from '../../DTOs';
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

  getSessionAssignment(devKitId: string, showAll: boolean): Observable<DevKitSessionAssignmentRowDto[]> {
    const q = showAll ? '?showAll=true' : '?showAll=false';
    return this.get<DevKitSessionAssignmentRowDto[]>(`${devKitId}/assignment/sessions${q}`);
  }

  putSessionAssignment(devKitId: string, ids: string[]): Observable<DevKitSessionAssignmentRowDto[]> {
    const body: IdListDto = { ids };
    return this.put<DevKitSessionAssignmentRowDto[]>(`${devKitId}/assignment/sessions`, body);
  }

  getFilmAssignment(devKitId: string, showAll: boolean): Observable<DevKitFilmAssignmentRowDto[]> {
    const q = showAll ? '?showAll=true' : '?showAll=false';
    return this.get<DevKitFilmAssignmentRowDto[]>(`${devKitId}/assignment/films${q}`);
  }

  putFilmAssignment(devKitId: string, ids: string[]): Observable<DevKitFilmAssignmentRowDto[]> {
    const body: IdListDto = { ids };
    return this.put<DevKitFilmAssignmentRowDto[]>(`${devKitId}/assignment/films`, body);
  }
}
