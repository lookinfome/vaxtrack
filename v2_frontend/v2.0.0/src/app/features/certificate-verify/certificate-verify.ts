import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { BookingService } from '../../core/services/booking.service';
import { CertificateService } from '../../core/services/certificate.service';
import { Certificate } from '../../core/models/booking.model';
import { FooterComponent } from '../../shared/components/footer/footer';

@Component({
  selector: 'app-certificate-verify',
  imports: [DatePipe, RouterLink, FooterComponent],
  templateUrl: './certificate-verify.html',
})
export class CertificateVerify implements OnInit {
  private route = inject(ActivatedRoute);
  private bookingService = inject(BookingService);
  private certificateService = inject(CertificateService);

  loading = signal(true);
  error = signal('');
  certificate = signal<Certificate | null>(null);
  downloading = signal(false);

  ngOnInit(): void {
    const bookingId = this.route.snapshot.paramMap.get('bookingId');
    if (!bookingId) {
      this.error.set('Invalid certificate link.');
      this.loading.set(false);
      return;
    }

    this.bookingService.getCertificate(bookingId).subscribe({
      next: (cert) => {
        this.certificate.set(cert);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('This certificate could not be found, or the vaccination is not yet fully completed.');
        this.loading.set(false);
      }
    });
  }

  download(): void {
    const cert = this.certificate();
    if (!cert) return;
    this.downloading.set(true);
    this.certificateService.downloadCertificate(cert).finally(() => this.downloading.set(false));
  }
}
