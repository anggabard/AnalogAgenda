import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';

@NgModule({
  exports: [ReactiveFormsModule, FormsModule]
})
export class SharedModule {}