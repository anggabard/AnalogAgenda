import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UsedDevKitThumbnailDto } from '../../DTOs';
import { BaseService } from '../base.service';

@Injectable({
  providedIn: 'root'
})
export class UsedDevKitThumbnailService extends BaseService {
  constructor() { 
    super('UsedDevKitThumbnail'); 
  }

  searchByDevKitName(devKitName: string): Observable<UsedDevKitThumbnailDto[]> {
    return this.get<UsedDevKitThumbnailDto[]>(`/search?devKitName=${encodeURIComponent(devKitName)}`);
  }

  uploadThumbnail(devKitName: string, imageBase64: string): Observable<UsedDevKitThumbnailDto> {
    const dto: UsedDevKitThumbnailDto = {
      rowKey: '',
      devKitName: devKitName,
      imageId: '',
      imageUrl: '',
      imageBase64: imageBase64
    };
    return this.post<UsedDevKitThumbnailDto>('', dto);
  }
}
