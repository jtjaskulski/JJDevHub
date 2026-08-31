import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { describeApiError } from '../../auth/api-error';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected readonly error = signal<string | null>(null);
  protected readonly pending = signal(false);

  protected submit(): void {
    this.pending.set(true);
    this.error.set(null);
    this.auth.login(this.email, this.password).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (err) => {
        this.pending.set(false);
        this.error.set(describeApiError(err));
      },
    });
  }
}
