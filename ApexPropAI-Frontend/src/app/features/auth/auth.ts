import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms'; // הוספנו את NgForm
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './auth.html',
  styleUrls: ['./auth.scss']
})
export class AuthComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLogin = signal(true);
  loading = signal(false);
  error = signal<string | string[] | null>(null);

  protected isErrorList(): boolean {
    return Array.isArray(this.error());
  }
  protected getSingleError(): string {
    return this.error() as string;
  }
  protected getErrorList(): string[] {
    return this.error() as string[];
  }
  // שימו לב: מחקנו את משתני האימייל והסיסמה! הטופס ינהל אותם.

  private navigateAfterAuth(): void {
    const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/home';
    this.router.navigateByUrl(returnUrl);
  }

  // הפונקציה המרכזית שמקבלת את האובייקט של הטופס מה-HTML
  onSubmit(form: NgForm): void {
    this.error.set(null);

    // אבטחה נוספת: אם ה-HTML לא תקין, אל תמשיך
    if (form.invalid) return;

    // form.value מחזיק את כל השדות שהגדרנו ב-HTML
    const val = form.value;
    this.loading.set(true);

    if (this.isLogin()) {
      this.authService.login({ email: val.email, password: val.password }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          const backendResponse = err?.error;
          if (backendResponse?.errorCode === 'VALIDATION_ERROR' && Array.isArray(backendResponse.errors)) {
            this.error.set(backendResponse.errors); // שומר את כל רשימת השגיאות
          } else {
            this.error.set(backendResponse?.message || 'שגיאה בהרשמה');
          }
          this.loading.set(false);
        }
      });
    } else {
      // בדיקת סיסמאות ידנית (רק בהרשמה)
      if (val.password !== val.confirmPassword) {
        this.error.set('הסיסמאות אינן תואמות');
        this.loading.set(false);
        return;
      }
      this.authService.register({ fullName: val.fullName, email: val.email, password: val.password, confirmPassword: val.confirmPassword }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => { this.error.set(err?.error?.message || 'שגיאה בהרשמה'); this.loading.set(false); }
      });
    }
  }

  demoLogin(): void {
    this.loading.set(true);
    // שנה את פרטי ה-demo לפי מה שהשרת שלך מצפה
    this.authService.login({ email: 'demo@demo.com', password: 'password' }).subscribe({
      next: () => this.navigateAfterAuth(),
      error: (err) => {
        this.error.set('שגיאה בכניסת דוגמה');
        this.loading.set(false);
      }
    });
  }
}