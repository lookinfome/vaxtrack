export type HospitalStatus = 'Active' | 'Disabled' | 'PendingReactivation' | 'PendingUnregistration' | 'Unregistered';

export interface Hospital {
  hospitalId: string;
  hospitalUid: string;
  hospitalName: string;
  hospitalAddress: string;
  hospitalPinCode: string;
  hospitalPhoneNumber: string;
  hospitalEmail: string;
  totalSlots: number;
  slotsAvailable: number;
  status: HospitalStatus;
  statusComment: string | null;
  hospitalAdminCount: number;
  registeredDate: string;
  updatedDate: string;
}

export interface HospitalAuditLogEntry {
  hospitalId: string;
  actionType: string;
  actorUserUid: string;
  actorRole: string;
  comment: string | null;
  createdAt: string;
}

export interface CreateHospitalRequest {
  hospitalName: string;
}

export interface CreateHospitalResponse {
  hospitalId: string;
  hospitalName: string;
  hospitalAddress: string;
  hospitalPinCode: string;
  hospitalPhoneNumber: string;
  hospitalEmail: string;
  totalSlots: number;
  slotsAvailable: number;
  registeredDate: string;
}

export interface UpdateHospitalRequest {
  hospitalId: string;
  hospitalAddress: string;
  hospitalPinCode: string;
  hospitalPhoneNumber: string;
  hospitalEmail: string;
}

export interface UpdateHospitalResponse {
  hospitalId: string;
  hospitalAddress: string;
  hospitalPinCode: string;
  hospitalPhoneNumber: string;
  hospitalEmail: string;
}
