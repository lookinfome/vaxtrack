import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-authorize-unregister-modal',
  imports: [FormsModule],
  templateUrl: './authorize-unregister-modal.html',
})
export class AuthorizeUnregisterModalComponent {
  hospitalName = input.required<string>();
  loading = input(false);
  error = input('');

  confirmed = output<{ password: string; comment?: string }>();
  dismissed = output<void>();

  password = signal('');
  comment = signal('');

  confirm(): void {
    if (!this.password()) return;
    this.confirmed.emit({ password: this.password(), comment: this.comment().trim() || undefined });
  }

  dismiss(): void {
    this.dismissed.emit();
  }
}
