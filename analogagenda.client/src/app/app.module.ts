import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { LoginComponent } from './components/login/login.component';
import { SharedModule } from '../shared.module';

@NgModule({
  declarations: [
    LoginComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule, SharedModule
  ],
  providers: [],
  bootstrap: [LoginComponent]
})
export class AppModule { }
