import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent, HomeComponent, NotesComponent, SubstancesComponent, NewKitComponent } from './components';
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
      { path: 'substances/kit', component: NewKitComponent, canActivate: [sessionGuard] },
      { path: 'substances/kit/:id', component: NewKitComponent, canActivate: [sessionGuard] },
    ]
  },
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginComponent, canActivate: [loginGuard] },
    ]
  },
  //{ path: '**', component: NotFoundComponent } 
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
