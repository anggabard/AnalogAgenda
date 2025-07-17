import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent, HomeComponent } from './components';
import { BookNotesComponent } from './components/book-notes/book-notes.component';
import { InventoryComponent } from './components/inventory/inventory.component';
import { sessionGuard } from './guards/session.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';

const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [sessionGuard],
    children: [
      { path: '', redirectTo: '/home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent, canActivate: [sessionGuard] },
      { path: 'notes', component: BookNotesComponent, canActivate: [sessionGuard] },
      { path: 'inventory', component: InventoryComponent, canActivate: [sessionGuard] },
    ]
  },
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginComponent },
    ]
  },
  //{ path: '**', component: NotFoundComponent } 
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
