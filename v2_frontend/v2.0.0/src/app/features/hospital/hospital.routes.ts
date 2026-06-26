import { Routes } from '@angular/router';

export const hospitalRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./hospital-list/hospital-list').then(m => m.HospitalList)
  }
];
