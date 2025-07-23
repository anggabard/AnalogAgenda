import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { SharedModule } from '../shared.module';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent, LoginComponent, NavbarComponent, HomeComponent, NotesComponent, SubstancesComponent } from './components';
import { MainLayoutComponent, AuthLayoutComponent} from './layouts';
import { NewKitComponent } from './components/substances/new-kit/new-kit.component';

@NgModule({
  declarations: [
    LoginComponent,
    NavbarComponent,
    HomeComponent,
    AppComponent,
    NotesComponent,
    SubstancesComponent,
    MainLayoutComponent,
    AuthLayoutComponent,
    NewKitComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule, SharedModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
