import { Injectable } from '@angular/core';
import jsPDF from 'jspdf';
import QRCode from 'qrcode';
import { Certificate } from '../models/booking.model';

const TEAL = '#0d9488';
const SLATE_DARK = '#1e293b';
const SLATE = '#475569';
const SLATE_LIGHT = '#94a3b8';

function formatDate(iso: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' });
}

// Builds the verify-link URL for a given booking — the same link used by the Share icon and
// embedded as the certificate's QR code, so scanning the PDF lands on the same public page.
export function buildVerifyUrl(bookingId: string): string {
  return `${window.location.origin}/certificate/${encodeURIComponent(bookingId)}`;
}

@Injectable({ providedIn: 'root' })
export class CertificateService {

  // Renders a certificate PDF styled after a real vaccination certificate (beneficiary details,
  // dose-by-dose vaccination details, a scannable verification QR code) and triggers a download.
  async downloadCertificate(certificate: Certificate): Promise<void> {
    const verifyUrl = buildVerifyUrl(certificate.bookingId);
    const qrDataUrl = await QRCode.toDataURL(verifyUrl, { margin: 1, width: 220 });

    const doc = new jsPDF({ unit: 'pt', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const marginX = 48;
    let y = 56;

    // Header
    doc.setFillColor(TEAL);
    doc.circle(marginX + 14, y, 14, 'F');
    doc.setTextColor('#ffffff');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.text('V', marginX + 14, y + 5, { align: 'center' });

    doc.setTextColor(SLATE_DARK);
    doc.setFontSize(16);
    doc.text('VaxTrack', marginX + 36, y + 5);

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(SLATE_LIGHT);
    doc.text('Secure vaccination slot booking', marginX + 36, y + 18);

    y += 44;
    doc.setDrawColor(TEAL);
    doc.setLineWidth(2);
    doc.line(marginX, y, pageWidth - marginX, y);

    y += 34;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.setTextColor(SLATE_DARK);
    doc.text('Certificate of COVID-19 Vaccination', marginX, y);

    y += 20;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(TEAL);
    doc.text('Fully Vaccinated', marginX, y);

    // Beneficiary Details
    y += 34;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(SLATE_DARK);
    doc.text('Beneficiary Details', marginX, y);
    y += 8;
    doc.setDrawColor('#e2e8f0');
    doc.setLineWidth(1);
    doc.line(marginX, y, pageWidth - marginX, y);

    const row = (label: string, value: string) => {
      y += 22;
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(9);
      doc.setTextColor(SLATE_LIGHT);
      doc.text(label.toUpperCase(), marginX, y);
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(11);
      doc.setTextColor(SLATE_DARK);
      doc.text(value || '—', marginX, y + 14);
      y += 14;
    };

    row('Beneficiary Name', certificate.beneficiaryName);
    row('Age / Gender', `${certificate.beneficiaryAge} years · ${certificate.beneficiaryGender || '—'}`);
    row('Booking Reference ID', certificate.bookingId);

    // Vaccination Details
    y += 26;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(SLATE_DARK);
    doc.text('Vaccination Details', marginX, y);
    y += 8;
    doc.line(marginX, y, pageWidth - marginX, y);

    row('Dose 1 — Vaccination Centre', certificate.dose1HospitalName);
    row('Dose 1 — Date Administered', formatDate(certificate.dose1CompletedDate));
    if (certificate.dose2HospitalName) {
      row('Dose 2 — Vaccination Centre', certificate.dose2HospitalName);
      row('Dose 2 — Date Administered', formatDate(certificate.dose2CompletedDate));
    }
    row('Vaccination Completed On', formatDate(certificate.vaccinationCompletedDate));

    // Footer: QR + verification note
    y += 30;
    doc.setDrawColor(TEAL);
    doc.setLineWidth(2);
    doc.line(marginX, y, pageWidth - marginX, y);
    y += 26;

    const qrSize = 90;
    doc.addImage(qrDataUrl, 'PNG', marginX, y, qrSize, qrSize);

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(SLATE_DARK);
    doc.text('Scan to verify this certificate', marginX + qrSize + 16, y + 20);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(SLATE);
    doc.text(verifyUrl, marginX + qrSize + 16, y + 36, { maxWidth: pageWidth - marginX * 2 - qrSize - 16 });
    doc.setFontSize(8);
    doc.setTextColor(SLATE_LIGHT);
    doc.text(
      'This certificate is issued electronically by VaxTrack and can be verified at any time using the link or QR code above.',
      marginX + qrSize + 16, y + 56, { maxWidth: pageWidth - marginX * 2 - qrSize - 16 }
    );

    doc.save(`VaxTrack-Certificate-${certificate.bookingId}.pdf`);
  }
}
