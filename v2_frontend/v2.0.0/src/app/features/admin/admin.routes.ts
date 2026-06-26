import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard)
  }
];
