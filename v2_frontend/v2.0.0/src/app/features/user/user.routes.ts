import { Routes } from '@angular/router';

export const userRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./profile/profile').then(m => m.Profile)
  }
];
