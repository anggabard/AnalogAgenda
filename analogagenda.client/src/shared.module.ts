import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';

@NgModule({
  exports: [
    ReactiveFormsModule, 
    FormsModule, 
    DragDropModule,
    CommonModule
  ]
})
export class SharedModule {}