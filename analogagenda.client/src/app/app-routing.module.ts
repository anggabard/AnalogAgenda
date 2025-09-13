import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent, HomeComponent, NotesComponent, SubstancesComponent, UpsertKitComponent, NoteTableComponent } from './components';
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
      { path: 'notes/new', component: NoteTableComponent, canActivate: [sessionGuard] },
      { path: 'notes/:id', component: NoteTableComponent, canActivate: [sessionGuard] },
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
