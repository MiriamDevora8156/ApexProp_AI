import { Component, inject, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth';
import { CompareService } from '../../core/services/compare';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.scss']
})
export class NavbarComponent {
  readonly authService  = inject(AuthService);
  readonly compareService = inject(CompareService);
  private  router       = inject(Router);

  dropdownOpen = signal(false);

  // ── סגירה בלחיצה מחוץ ל-Dropdown ────────────────────────────
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.navbar-avatar-wrapper')) {
      this.dropdownOpen.set(false);
    }
  }

  // ── סגירה ב-Escape ────────────────────────────────────────────
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dropdownOpen.set(false);
  }

  // ── ניווט מקלדת בתוך ה-Dropdown (Arrow Keys) ─────────────────
  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.dropdownOpen()) return;

    const items = Array.from(
      document.querySelectorAll<HTMLElement>('[role="menu"] [role="menuitem"]')
    );
    if (!items.length) return;

    const current = document.activeElement as HTMLElement;
    const idx = items.indexOf(current);

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const next = items[(idx + 1) % items.length];
      next?.focus();
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      const prev = items[(idx - 1 + items.length) % items.length];
      prev?.focus();
    }
  }

  toggleDropdown(): void {
    this.dropdownOpen.update(v => !v);
  }

  getInitial(): string {
    return this.authService.currentUser()?.fullName?.charAt(0).toUpperCase() ?? '?';
  }

  logout(): void {
    this.dropdownOpen.set(false);
    this.authService.logout();
    this.router.navigate(['/']);
  }
}