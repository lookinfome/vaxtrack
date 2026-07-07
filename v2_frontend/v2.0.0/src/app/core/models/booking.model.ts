export interface Booking {
  bookingId: string;
  userUid: string;

  dose1RequestedDateTime: string;
  dose1SlotNumber: number;
  dose1HospitalUid: string;
  isDose1Completed: boolean;
  dose1CompletedDateTime: string | null;
  isD1RequestCanceled: boolean;
  isD1RejectedByAdmin: boolean;

  dose2RequestedDateTime: string | null;
  dose2SlotNumber: number;
  dose2HospitalUid: string;
  isDose2Completed: boolean;
  dose2CompletedDateTime: string | null;
  isD2RequestCanceled: boolean;
  isD2RejectedByAdmin: boolean;

  isVaccinationCompleted: boolean;
  vaccinationCompletedDateTime: string | null;

  // server-computed display status — restricts what user-facing screens show
  dose1DisplayStatus: string;       // "Pending" | "Completed" | "Cancelled" | "Rejected"
  dose2DisplayStatus: string;       // same + "NotBooked"
  vaccinationDisplayStatus: string; // "NotVaccinated" | "Pending" | "PartiallyVaccinated" | "Vaccinated" | "Rejected"

  createdAt: string;
  modifiedAt: string;

  // Comma-joined email(s) of the active hospital-admin(s) for each dose's hospital —
  // empty string if none assigned.
  dose1HospitalAdminEmails: string;
  dose2HospitalAdminEmails: string;
}

export interface CreateBookingRequest {
  userUid: string;
  dose1RequestedDateTime: string;
  dose1HospitalUid: string;
}

export interface RebookDose1Request {
  bookingId: string;
  userUid: string;
  newHospitalUid: string;
  newRequestedDateTime: string;
}

export interface CreateBookingResponse {
  bookingId: string;
  userUid: string;
  dose1RequestedDateTime: string;
  dose1SlotNumber: number;
  dose1HospitalUid: string;
}

export interface UpdateBookingRequest {
  bookingId: string;
  userUid: string;
  dose1RequestedDateTime: string;
  dose1SlotNumber: number;
  dose1HospitalUid: string;
  isDose1Completed: boolean;
  dose1CompletedDateTime: string | null;
  isD1RequestCanceled: boolean;
  dose2RequestedDateTime: string | null;
  dose2SlotNumber: number;
  dose2HospitalUid: string;
  isDose2Completed: boolean;
  dose2CompletedDateTime: string | null;
  isD2RequestCanceled: boolean;
  isVaccinationCompleted: boolean;
  vaccinationCompletedDateTime: string | null;
}

export interface UpdateBookingResponse {
  bookingId: string;
  userUid: string;
  dose1RequestedDateTime: string;
  dose1SlotNumber: number;
  dose1HospitalUid: string;
  isDose1Completed: boolean;
  dose1CompletedDateTime: string | null;
  isD1RequestCanceled: boolean;
  dose2RequestedDateTime: string | null;
  dose2SlotNumber: number;
  dose2HospitalUid: string;
  isDose2Completed: boolean;
  dose2CompletedDateTime: string | null;
  isD2RequestCanceled: boolean;
  isVaccinationCompleted: boolean;
  vaccinationCompletedDateTime: string | null;
}

export interface EditBookingRequest {
  bookingId: string;
  userUid: string;
  doseNumber: number;
  newHospitalUid: string;
  newRequestedDateTime: string;
}

export interface BookDose2Request {
  bookingId: string;
  userUid: string;
  dose2HospitalUid: string;
  dose2RequestedDateTime: string;
}

export interface BookDose2Response {
  bookingId: string;
  userUid: string;
  dose2HospitalUid: string;
  dose2SlotNumber: number;
  dose2RequestedDateTime: string | null;
  isDose2Completed: boolean;
}

export interface BookingAuditLogEntry {
  bookingId: string;
  doseNumber: number;
  actionType: string;
  actorUserUid: string;
  actorRole: string;
  comment: string | null;
  createdAt: string;
}

export interface NextAvailableSlot {
  slotNumber: number;
  slotDateTime: string;
}

export interface Certificate {
  bookingId: string;
  beneficiaryName: string;
  beneficiaryAge: number;
  beneficiaryGender: string;
  dose1HospitalName: string;
  dose1CompletedDate: string | null;
  dose2HospitalName: string;
  dose2CompletedDate: string | null;
  vaccinationCompletedDate: string | null;
}
