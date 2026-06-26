import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';
import { adminGuard } from './shared/guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: 'hospital',
    canActivate: [authGuard],
    loadChildren: () => import('./features/hospital/hospital.routes').then(m => m.hospitalRoutes)
  },
  {
    path: 'booking',
    canActivate: [authGuard],
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.bookingRoutes)
  },
  {
    path: 'user',
    canActivate: [authGuard],
    loadChildren: () => import('./features/user/user.routes').then(m => m.userRoutes)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
  },
  { path: '**', redirectTo: 'auth/login' }
];
