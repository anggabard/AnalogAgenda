import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { BaseUpsertComponent } from '../../common/base-upsert/base-upsert.component';
import { DevKitService } from '../../../services';
import { DevKitType, UsernameType } from '../../../enums';
import { DevKitDto } from '../../../DTOs';
import { DateHelper } from '../../../helpers/date.helper';

@Component({
    selector: 'app-upsert-kit',
    templateUrl: './upsert-kit.component.html',
    styleUrl: './upsert-kit.component.css',
    standalone: false
})
export class UpsertKitComponent extends BaseUpsertComponent<DevKitDto> implements OnInit {

  constructor(private devKitService: DevKitService) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
  }

  // Component-specific properties
  devKitOptions = Object.values(DevKitType);
  purchasedByOptions = Object.values(UsernameType);

  protected createForm(): FormGroup {
    return this.fb.group({
      name: ['', Validators.required],
      url: ['', [Validators.required]],
      type: [DevKitType.C41, Validators.required],
      purchasedBy: ['', Validators.required],
      purchasedOn: [DateHelper.getTodayForInput(), Validators.required],
      mixedOn: [DateHelper.getTodayForInput()],
      validForWeeks: [6, Validators.required],
      validForFilms: [8, Validators.required],
      filmsDeveloped: [0, Validators.required],
      imageUrl: [''],
      imageBase64: [''],
      description: [''],
      expired: [false, Validators.required]
    });
  }

  protected getCreateObservable(item: DevKitDto): Observable<any> {
    return this.devKitService.add(item);
  }

  protected getUpdateObservable(rowKey: string, item: DevKitDto): Observable<any> {
    return this.devKitService.update(rowKey, item);
  }

  protected getDeleteObservable(rowKey: string): Observable<any> {
    return this.devKitService.deleteById(rowKey);
  }

  protected getItemObservable(rowKey: string): Observable<DevKitDto> {
    return this.devKitService.getById(rowKey);
  }

  protected getBaseRoute(): string {
    return '/substances';
  }

  protected getEntityName(): string {
    return 'Kit';
  }
}
