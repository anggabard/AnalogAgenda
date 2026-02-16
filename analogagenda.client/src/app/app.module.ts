import { provideHttpClient } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { SharedModule } from '../shared.module';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent, LoginComponent, NavbarComponent, HomeComponent, NotesComponent, SubstancesComponent, UpsertKitComponent, NoteTableComponent, FilmsComponent, UpsertFilmComponent, FilmPhotosComponent, PhotosContentComponent, PhotosComponent, SessionsComponent, UpsertSessionComponent, ChangePasswordComponent, NotesMergeComponent } from './components';
import { FilmSearchComponent } from './components/films/film-search/film-search.component';
import { CardListComponent } from './components/common/card-list/card-list.component';
import { ListComponent } from './components/common/list/list.component';
import { TableListComponent } from './components/common/table-list/table-list.component';
import { ImagePreviewComponent } from './components/common/image-preview/image-preview.component';
import { TimeInputComponent } from './components/common/time-input/time-input.component';
import { WackyIdeasSectionComponent } from './components/home/wacky-ideas-section/wacky-ideas-section.component';
import { UpsertIdeaComponent } from './components/home/upsert-idea/upsert-idea.component';
import { MainLayoutComponent, AuthLayoutComponent} from './layouts';

@NgModule({
  declarations: [
    LoginComponent,
    NavbarComponent,
    HomeComponent,
    WackyIdeasSectionComponent,
    UpsertIdeaComponent,
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
    NotesMergeComponent
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
