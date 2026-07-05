export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  userId: string;
  userUid: string;
  userName: string;
  email: string;
  userRole: boolean;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetToken: string | null;
  expiresAt: string | null;
}

export interface ResetForgottenPasswordRequest {
  resetToken: string;
  newPassword: string;
}

export interface ResetForgottenPasswordResponse {
  userId: string;
  updatedAt: string;
}

export interface RequestAccountReactivationRequest {
  email: string;
  reason?: string;
}

// Distinguishes a disabled-account login failure from any other error — the login page
// shows the reason + a link to the public reactivation form only when this code is present.
export interface AccountDisabledError {
  code: 'ACCOUNT_DISABLED';
  message: string;
  reason: string | null;
}
