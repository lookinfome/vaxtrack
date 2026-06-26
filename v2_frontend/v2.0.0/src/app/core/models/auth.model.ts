export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  userId: string;
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
