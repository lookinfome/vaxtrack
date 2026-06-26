import { Routes } from '@angular/router';

export const bookingRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./my-bookings/my-bookings').then(m => m.MyBookings)
  }
];
