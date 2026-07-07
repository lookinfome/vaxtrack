import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Booking,
  CreateBookingRequest, CreateBookingResponse,
  UpdateBookingRequest, UpdateBookingResponse,
  EditBookingRequest,
  BookDose2Request, BookDose2Response,
  RebookDose1Request,
  BookingAuditLogEntry,
  NextAvailableSlot,
  Certificate
} from '../models/booking.model';

const BASE = '/api/vaxtrack/v1/booking';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private http = inject(HttpClient);

  createBooking(request: CreateBookingRequest): Observable<CreateBookingResponse> {
    return this.http.post<CreateBookingResponse>(`${BASE}/createBooking`, request);
  }

  updateBooking(request: UpdateBookingRequest): Observable<UpdateBookingResponse> {
    return this.http.put<UpdateBookingResponse>(`${BASE}/updateBooking`, request);
  }

  bookDose2(request: BookDose2Request): Observable<BookDose2Response> {
    return this.http.put<BookDose2Response>(`${BASE}/bookDose2`, request);
  }

  editBooking(request: EditBookingRequest): Observable<Booking> {
    return this.http.put<Booking>(`${BASE}/editBooking`, request);
  }

  rebookDose1(request: RebookDose1Request): Observable<Booking> {
    return this.http.put<Booking>(`${BASE}/rebookDose1`, request);
  }

  approveBooking(bookingId: string, comment?: string): Observable<Booking> {
    return this.http.put<Booking>(`${BASE}/approveBookings/${bookingId}`, { comment });
  }

  cancelBooking(bookingId: string, comment?: string): Observable<Booking> {
    return this.http.put<Booking>(`${BASE}/cancelBookings/${bookingId}`, { comment });
  }

  rejectBooking(bookingId: string, comment?: string): Observable<Booking> {
    return this.http.put<Booking>(`${BASE}/rejectBookings/${bookingId}`, { comment });
  }

  getBookingAuditTrail(bookingId: string): Observable<BookingAuditLogEntry[]> {
    return this.http.get<BookingAuditLogEntry[]>(`${BASE}/getBookingAuditTrail/${bookingId}`);
  }

  getActionableBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${BASE}/getActionableBookings`);
  }

  getNextAvailableSlot(hospitalId: string, date: string): Observable<NextAvailableSlot> {
    return this.http.get<NextAvailableSlot>(`${BASE}/getNextAvailableSlot/${hospitalId}/${date}`);
  }

  getBookingById(bookingId: string): Observable<Booking> {
    return this.http.get<Booking>(`${BASE}/getBookingByBookingId/${bookingId}`);
  }

  getBookingsByUserId(userId: string): Observable<Booking> {
    return this.http.get<Booking>(`${BASE}/getBookingsByUserId/${userId}`);
  }

  getBookingsByHospitalId(hospitalId: string): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${BASE}/getBookingsByHospitalId/${hospitalId}`);
  }

  getAllBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${BASE}/getAllBookings`);
  }

  isBookingExists(bookingId: string): Observable<boolean> {
    return this.http.get<boolean>(`${BASE}/isBookingExists/${bookingId}`);
  }

  deleteBooking(bookingId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/deleteBooking/${bookingId}`);
  }

  // Public — no auth required, backs both the owner's PDF download and the public verify page.
  getCertificate(bookingId: string): Observable<Certificate> {
    return this.http.get<Certificate>(`${BASE}/getCertificate/${bookingId}`);
  }
}
