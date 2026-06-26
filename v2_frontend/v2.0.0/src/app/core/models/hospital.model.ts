export interface Hospital {
  hospitalId: string;
  hospitalName: string;
  hospitalAddress: string;
  hospitalPinCode: string;
  hospitalPhoneNumber: string;
  hospitalEmail: string;
  totalSlots: number;
  slotsAvailable: number;
  registeredDate: string;
  updatedDate: string;
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
