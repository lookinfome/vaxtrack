import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';

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
    // adminGuard is applied per-child in admin.routes.ts (not here) — the 'bookings' child is
    // reachable by any authenticated user (real authorization is enforced server-side, scoped to
    // the caller's hospital-admin assignments), while 'dashboard' stays platform-admin-only.
    path: 'admin',
    canActivate: [authGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
  },
  {
    path: 'support',
    canActivate: [authGuard],
    loadComponent: () => import('./features/support/support').then(m => m.Support)
  },
  {
    // Public — general-audience page describing the product and its version history, no login required.
    path: 'about',
    loadComponent: () => import('./features/about/about').then(m => m.About)
  },
  {
    // Public — a disabled account can never log in, so this must be reachable without a session.
    path: 'account-reactivation',
    loadComponent: () => import('./features/account-reactivation/account-reactivation').then(m => m.AccountReactivation)
  },
  {
    // Public — anyone holding the certificate link/QR code can verify it, no login required
    // (mirrors the real-world CoWIN QR-verification flow).
    path: 'certificate/:bookingId',
    loadComponent: () => import('./features/certificate-verify/certificate-verify').then(m => m.CertificateVerify)
  },
  { path: '**', redirectTo: 'auth/login' }
];
