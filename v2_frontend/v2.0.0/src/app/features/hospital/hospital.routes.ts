import { Routes } from '@angular/router';
import { hospitalAdminOrAdminGuard } from '../../shared/guards/hospital-admin.guard';

// Regular users select a hospital directly from the booking flow's own hospital dropdown
// (see MyBookings) — they never need this standalone management view.
export const hospitalRoutes: Routes = [
  {
    path: '',
    canActivate: [hospitalAdminOrAdminGuard],
    loadComponent: () => import('./hospital-list/hospital-list').then(m => m.HospitalList)
  }
];
