export interface UserRoleMapping {
  id: number;
  userUid: string;
  userId?: string | null;
  userName?: string | null;
  roleTag: string;
  contextId: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export type UserRequestType = 'AccountReactivation' | 'HospitalAdminApplication';
export type UserRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface UserRequest {
  id: number;
  userUid: string;
  userId?: string | null;
  userName?: string | null;
  requestType: UserRequestType;
  targetHospitalId: string | null;
  status: UserRequestStatus;
  userComment: string | null;
  adminComment: string | null;
  createdAt: string;
  resolvedAt: string | null;
  resolvedByUserUid: string | null;
}
