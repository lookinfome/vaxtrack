import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-confirm-action-modal',
  imports: [FormsModule],
  templateUrl: './confirm-action-modal.html',
})
export class ConfirmActionModalComponent {
  title = input.required<string>();
  message = input.required<string>();
  confirmLabel = input('Confirm');
  cancelLabel = input('Cancel');
  showCommentBox = input(false);
  commentRequired = input(false);
  accentColor = input<'red' | 'teal' | 'emerald' | 'amber'>('red');

  confirmed = output<string | undefined>();
  dismissed = output<void>();

  comment = signal('');

  get commentInvalid(): boolean {
    return this.commentRequired() && !this.comment().trim();
  }

  confirm(): void {
    if (this.commentInvalid) return;
    this.confirmed.emit(this.showCommentBox() ? this.comment().trim() || undefined : undefined);
  }

  dismiss(): void {
    this.dismissed.emit();
  }
}
