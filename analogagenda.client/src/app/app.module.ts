import { provideHttpClient } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { SharedModule } from '../shared.module';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent, LoginComponent, NavbarComponent, HomeComponent, IdeaResultsComponent, NotesComponent, SubstancesComponent, UpsertKitComponent, NoteTableComponent, FilmsComponent, UpsertFilmComponent, FilmPhotosComponent, PhotosContentComponent, PhotosComponent, SessionsComponent, UpsertSessionComponent, ChangePasswordComponent, NotesMergeComponent, CollectionsComponent, UpsertCollectionComponent, PublicCollectionPageComponent } from './components';
import { NotesTimerComponent } from './components/notes/notes-timer/notes-timer.component';
import { FilmSearchComponent } from './components/films/film-search/film-search.component';
import {
  CardListComponent,
  ListComponent,
  TableListComponent,
  ImagePreviewComponent,
  TimeInputComponent,
  ConfirmDeleteModalComponent,
  ModalComponent,
  ErrorMessageComponent,
  QuantityStepperComponent
} from './components/common';
import { WackyIdeasSectionComponent } from './components/home/wacky-ideas-section/wacky-ideas-section.component';
import { UpsertIdeaComponent } from './components/home/wacky-ideas-section/upsert-idea/upsert-idea.component';
import { FilmCheckSectionComponent } from './components/home/film-check-section/film-check-section.component';
import { FilmCheckUserComponent } from './components/home/film-check-section/film-check-user/film-check-user.component';
import { CurrentFilmSectionComponent } from './components/home/current-film-section/current-film-section.component';
import { PhotoOfTheDaySectionComponent } from './components/home/photo-of-the-day-section/photo-of-the-day-section.component';
import { SettingsSectionComponent } from './components/home/settings-section/settings-section.component';
import { MainLayoutComponent, AuthLayoutComponent} from './layouts';

@NgModule({
  declarations: [
    LoginComponent,
    NavbarComponent,
    HomeComponent,
    IdeaResultsComponent,
    WackyIdeasSectionComponent,
    UpsertIdeaComponent,
    FilmCheckSectionComponent,
    FilmCheckUserComponent,
    CurrentFilmSectionComponent,
    PhotoOfTheDaySectionComponent,
    SettingsSectionComponent,
    AppComponent,
    NotesComponent,
    SubstancesComponent,
    FilmsComponent,
    UpsertFilmComponent,
    FilmPhotosComponent,
    PhotosContentComponent,
    PhotosComponent,
    SessionsComponent,
    UpsertSessionComponent,
    MainLayoutComponent,
    AuthLayoutComponent,
    UpsertKitComponent,
    NoteTableComponent,
    ChangePasswordComponent,
    CardListComponent,
    ListComponent,
    TableListComponent,
    ImagePreviewComponent,
    TimeInputComponent,
    ConfirmDeleteModalComponent,
    ModalComponent,
    ErrorMessageComponent,
    QuantityStepperComponent,
    NotesMergeComponent,
    NotesTimerComponent,
    CollectionsComponent,
    UpsertCollectionComponent,
    PublicCollectionPageComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule, 
    SharedModule,
    FilmSearchComponent
  ],
  providers: [
    provideHttpClient()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
