export interface Booking {
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

  createdAt: string;
  modifiedAt: string;
}

export interface CreateBookingRequest {
  userUid: string;
  dose1RequestedDateTime: string;
  dose1SlotNumber: number;
  dose1HospitalUid: string;
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

export interface BookDose2Request {
  bookingId: string;
  userUid: string;
  dose2HospitalUid: string;
  dose2SlotNumber: number;
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
