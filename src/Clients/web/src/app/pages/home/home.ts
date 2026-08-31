import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  private readonly auth = inject(AuthService);

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly apiStatus = signal<'checking' | 'ok' | 'down'>('checking');
  protected readonly email = signal<string | null>(null);
  protected readonly userId = signal<string | null>(null);
  protected readonly meError = signal<string | null>(null);

  ngOnInit(): void {
    this.auth.health().subscribe({
      next: () => this.apiStatus.set('ok'),
      error: () => this.apiStatus.set('down'),
    });

    if (this.auth.isAuthenticated()) {
      this.auth.me().subscribe({
        next: (me) => {
          this.email.set(me.email ?? null);
          this.userId.set(me.id);
        },
        error: () => this.meError.set('Could not load the current user.'),
      });
    }
  }
}
