import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent, HomeComponent, NotesComponent, SubstancesComponent, UpsertKitComponent, NoteTableComponent, FilmsComponent, UpsertFilmComponent, FilmPhotosComponent, SessionsComponent, UpsertSessionComponent, ChangePasswordComponent, NotesMergeComponent } from './components';
import { sessionGuard, loginGuard } from './guards';
import { MainLayoutComponent, AuthLayoutComponent } from './layouts';

const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [sessionGuard],
    children: [
      { path: '', redirectTo: '/home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent, canActivate: [sessionGuard] },
      { path: 'notes', component: NotesComponent, canActivate: [sessionGuard] },
      { path: 'substances', component: SubstancesComponent, canActivate: [sessionGuard] },
      { path: 'substances/new', component: UpsertKitComponent, canActivate: [sessionGuard] },
      { path: 'substances/:id', component: UpsertKitComponent, canActivate: [sessionGuard] },
      { path: 'films', component: FilmsComponent, canActivate: [sessionGuard] },
      { path: 'films/new', component: UpsertFilmComponent, canActivate: [sessionGuard] },
      { path: 'films/:id/photos', component: FilmPhotosComponent, canActivate: [sessionGuard] },
      { path: 'films/:id', component: UpsertFilmComponent, canActivate: [sessionGuard] },
      { path: 'sessions', component: SessionsComponent, canActivate: [sessionGuard] },
      { path: 'sessions/new', component: UpsertSessionComponent, canActivate: [sessionGuard] },
      { path: 'sessions/:id', component: UpsertSessionComponent, canActivate: [sessionGuard] },
      { path: 'notes/new', component: NoteTableComponent, canActivate: [sessionGuard] },
      { path: 'notes/merge/:compositeId', component: NotesMergeComponent, canActivate: [sessionGuard] },
      { path: 'notes/:id', component: NoteTableComponent, canActivate: [sessionGuard] },
      { path: 'change-password', component: ChangePasswordComponent, canActivate: [sessionGuard] },
    ]
  },
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginComponent, canActivate: [loginGuard] },
    ]
  },
  { path: '**', redirectTo: '/login', pathMatch: 'full' } 
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
