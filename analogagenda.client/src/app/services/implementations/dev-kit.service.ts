import { Injectable } from '@angular/core';
import { BaseService } from '../base.service';
import { DevKitDto } from '../../DTOs';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DevKitService extends BaseService {

  constructor() { super('DevKit'); }

  addNewKit(newKit: DevKitDto) {
    return this.post('', newKit);
  }

  getAllDevKits(): Observable<DevKitDto[]>{
    return this.get<DevKitDto[]>('');
  }
}
