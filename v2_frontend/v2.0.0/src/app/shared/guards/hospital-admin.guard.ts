import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { RoleMappingService } from '../../core/services/role-mapping.service';

// Platform admins always pass. Everyone else must hold an active hospital-admin role
// mapping (checked live against the server — this isn't encoded in the JWT). A plain
// user hitting this guard is redirected away instead of landing on an empty-state page.
export const hospitalAdminOrAdminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const roleMappingService = inject(RoleMappingService);
  const router = inject(Router);

  if (authService.isAdmin()) return of(true);

  const userUid = authService.currentUser()?.userUid ?? '';
  if (!userUid) {
    router.navigate(['/user']);
    return of(false);
  }

  return roleMappingService.isHospitalAdmin(userUid).pipe(
    map(isHospitalAdmin => isHospitalAdmin || router.createUrlTree(['/user']))
  );
};
