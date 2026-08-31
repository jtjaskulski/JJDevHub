import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly title = 'JJDevHub';
  protected readonly isAuthenticated = this.auth.isAuthenticated;

  protected logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
