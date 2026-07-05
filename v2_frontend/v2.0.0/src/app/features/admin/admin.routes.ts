import { Routes } from '@angular/router';
import { adminGuard } from '../../shared/guards/admin.guard';
import { hospitalAdminOrAdminGuard } from '../../shared/guards/hospital-admin.guard';

export const adminRoutes: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard)
  },
  {
    // adminGuard doesn't apply here — hospital-admins (who are NOT platform admins) must be
    // able to reach this screen too. hospitalAdminOrAdminGuard checks for either a platform-admin
    // JWT or an active hospital-admin role mapping (fetched live, since that scoped role isn't
    // encoded in the JWT), redirecting anyone else back to /user instead of showing an empty list.
    path: 'bookings',
    canActivate: [hospitalAdminOrAdminGuard],
    loadComponent: () => import('./bookings-management/bookings-management').then(m => m.BookingsManagement)
  },
  {
    // Platform-admin only — viewing/managing the full user base is not a hospital-admin capability.
    path: 'users',
    canActivate: [adminGuard],
    loadComponent: () => import('./users-management/users-management').then(m => m.UsersManagement)
  }
];
