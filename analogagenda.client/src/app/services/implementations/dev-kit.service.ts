import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { DevKitDto, PagedResponseDto } from '../../DTOs';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DevKitService extends BaseService {
  constructor() { super('DevKit'); }

  addNewKit(newKit: DevKitDto) {
    return this.post('', newKit);
  }

  getAllDevKits(): Observable<DevKitDto[]> {
    return this.get<DevKitDto[]>('?page=0'); // page=0 for backward compatibility to get all kits
  }

  getDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.get<PagedResponseDto<DevKitDto>>(`?page=${page}&pageSize=${pageSize}`);
  }

  getAvailableDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.get<PagedResponseDto<DevKitDto>>(`available?page=${page}&pageSize=${pageSize}`);
  }

  getExpiredDevKitsPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<DevKitDto>> {
    return this.get<PagedResponseDto<DevKitDto>>(`expired?page=${page}&pageSize=${pageSize}`);
  }

  getKit(rowKey: string): Observable<DevKitDto> {
    return this.get<DevKitDto>(rowKey)
  }

  updateKit(rowKey: string , updateKit: DevKitDto) {
    return this.put(rowKey, updateKit);
  }

  deleteKit(rowKey: string){
    return this.delete(rowKey);
  }
}
