export type UserStatus = 'Active' | 'Disabled' | 'PendingReactivation';
export type VaccinationStatus = 'NotVaccinated' | 'Pending' | 'PartiallyVaccinated' | 'Vaccinated' | 'Rejected';

export interface User {
  userId: string;
  userUid: string;
  userName: string;
  userBirthdate: string;
  userAge: number;
  userGender: string;
  email: string;
  userPhone: string;
  userAddress: string;
  userPinCode: string;
  profilePicturePath: string;
  userRole: boolean;
  isHospitalAdmin: boolean;
  vaccinationStatus: VaccinationStatus;
  status: UserStatus;
  statusComment: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedUsersResponse {
  items: User[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DeleteMyAccountRequest {
  password: string;
  reason?: string;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  userBirthdate: string;
  userGender: string;
  userPhone: string;
  userAddress: string;
  userPinCode: string;
  email: string;
  password: string;
}

export interface CreateUserResponse {
  userId: string;
  userName: string;
  email: string;
  userRole: boolean;
  createdAt: string;
}

export interface UpdateUserRequest {
  userId: string;
  firstName: string;
  lastName: string;
  userGender: string;
  userPhone: string;
  userAddress: string;
  userPinCode: string;
  profilePicturePath: string;
}

export interface UpdateUserResponse {
  userId: string;
  firstName: string;
  lastName: string;
  userGender: string;
  userPhone: string;
  userAddress: string;
  userPinCode: string;
  profilePicturePath: string;
  updatedAt: string | null;
}

export interface UpdateEmailRequest {
  userId: string;
  newEmail: string;
  currentPassword: string;
}

export interface UpdateEmailResponse {
  userId: string;
  newEmail: string;
  updatedAt: string | null;
}

export interface ChangePasswordRequest {
  userId: string;
  currentPassword: string;
  newPassword: string;
}

export interface ChangePasswordResponse {
  userId: string;
  updatedAt: string | null;
}
