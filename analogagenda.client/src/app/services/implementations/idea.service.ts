import { Injectable } from '@angular/core';
import { IdeaDto } from '../../DTOs';
import { Observable } from 'rxjs';
import { BasePaginatedService } from '../base-paginated.service';

@Injectable({
  providedIn: 'root'
})
export class IdeaService extends BasePaginatedService<IdeaDto> {
  constructor() { super('Idea'); }
}
