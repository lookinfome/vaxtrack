import { Component, OnDestroy, signal } from '@angular/core';
import { FooterComponent } from '../../shared/components/footer/footer';

interface JourneyEntry {
  version: string;
  tag: string;
  theme: string;
  dot: string;
  highlights: string[];
  forDevelopers: string[];
}

const SLIDE_COUNT = 4;
const AUTO_ADVANCE_MS = 6000;

const JOURNEY: JourneyEntry[] = [
  {
    version: 'v1.0.0',
    tag: 'Beta',
    theme: 'Where it started — a COVID-era booking portal',
    dot: 'bg-slate-300',
    highlights: [
      'Users could register, log in, and book an available vaccination slot.',
      'Admins had a dashboard showing total users, pending approvals, and open slots.',
      'A booking moved from "not vaccinated" to "vaccinated" once an admin approved it.',
    ],
    forDevelopers: [
      'ASP.NET MVC application with Razor views, backed by SQLite (vaxTrackDB.db).',
      'Single-tier user/admin model; approvals and slot counts handled directly in controllers.',
    ],
  },
  {
    version: 'v1.1.0',
    tag: 'Remastered',
    theme: 'A structural rewrite before adding anything new',
    dot: 'bg-slate-400',
    highlights: [
      'Restructured the application architecture and updated the UI throughout.',
      'Redesigned the admin page with charts for a clearer, at-a-glance view.',
    ],
    forDevelopers: [
      'Introduced a DTO layer (absent in v1.0.0) so controllers stopped passing EF models directly to views.',
      'Normalized and restructured model relationships that had no normalization before.',
      'Replaced a raw password column with ASP.NET Identity\'s IdentityUser.',
    ],
  },
  {
    version: 'v1.2.0',
    tag: 'Iteration',
    theme: 'Making the day-to-day smoother',
    dot: 'bg-teal-300',
    highlights: [
      'Shipped a proper profile-edit module and a second-generation slot-booking flow.',
      'Added contextual welcome, update, and alert messages across login, registration, profile edits, and booking.',
      'Users could finally upload a profile picture.',
    ],
    forDevelopers: [
      'Condition-based messaging layered onto the existing MVC controllers rather than a rewrite.',
    ],
  },
  {
    version: 'v1.3.0',
    tag: 'Remastered — Final',
    theme: 'A full UI pass, and an honest list of what\'s next',
    dot: 'bg-teal-500',
    highlights: [
      'Added password reset and a Support module so users could raise a ticket and follow up on it.',
      'Every major page — home, sign in, sign up, profile, booking, admin — got a UI pass, including a v2 admin page with separated tabs.',
      'The team closed the version by writing down, in plain language, what still needed work: ticket file uploads, an SLA/OLA tracker, a real notification module, and general code cleanup.',
    ],
    forDevelopers: [
      'Upgraded to Bootstrap 5.3 and fixed the styling/font regressions that came with it.',
      'Consolidated several standalone pages (login, register, profile edit, slot booking) into modal/inline views on the home and profile pages.',
      'Support ticketing shipped without file uploads or an SLA/OLA tracker, and the notification module was named as a known gap — both carried forward as the starting brief for v2.',
    ],
  },
  {
    version: 'v2.0.0',
    tag: 'Current',
    theme: 'A ground-up rebuild: three-tier roles, live notifications, audited by design',
    dot: 'bg-emerald-500',
    highlights: [
      'A three-tier role model — normal user, hospital-admin scoped to one hospital, and platform admin — enforced on every request, not just in the UI.',
      'Hospital and user lifecycle management: disable, reactivate, and a two-party unregister flow, each with a mandatory reason.',
      'In-app notifications fire on every approval, rejection, and status change — the gap v1.3.0 flagged is now real.',
      'Vaccination certificates that are downloadable and publicly verifiable by link, without exposing personal details.',
      'A full audit trail on every state-changing action against a booking, hospital, or user.',
    ],
    forDevelopers: [
      'Backend rewritten as an ASP.NET Core Web API on .NET 10, three-layer architecture (Controllers → Services → Repositories) over EF Core 10 / SQL Server.',
      'JWT bearer auth (HMAC-SHA256) with server-side revocation on logout — tokens are stateless but still killable.',
      'Frontend rewritten from scratch in Angular 20 (standalone components) with TailwindCSS.',
      'A dedicated booking audit-log table records actor, action, and reason for edit/rebook/reject/cancel/approve.',
      'Self-service flows (forgot/reset password, disabled-account reactivation, hospital-admin applications) each route into an admin approval queue instead of a support ticket.',
    ],
  },
];

@Component({
  selector: 'app-about',
  imports: [FooterComponent],
  templateUrl: './about.html',
})
export class About implements OnDestroy {
  readonly journey = JOURNEY;

  currentSlide = signal(0);
  showDevDetails = signal(false);

  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    this.startAutoAdvance();
  }

  ngOnDestroy(): void {
    this.stopAutoAdvance();
  }

  goToSlide(i: number): void {
    this.currentSlide.set(((i % SLIDE_COUNT) + SLIDE_COUNT) % SLIDE_COUNT);
  }

  nextSlide(): void {
    this.goToSlide(this.currentSlide() + 1);
  }

  prevSlide(): void {
    this.goToSlide(this.currentSlide() - 1);
  }

  toggleDevDetails(): void {
    this.showDevDetails.update(v => !v);
  }

  pauseAutoAdvance(): void {
    this.stopAutoAdvance();
  }

  resumeAutoAdvance(): void {
    this.startAutoAdvance();
  }

  private startAutoAdvance(): void {
    if (this.timer) return;
    this.timer = setInterval(() => this.nextSlide(), AUTO_ADVANCE_MS);
  }

  private stopAutoAdvance(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
