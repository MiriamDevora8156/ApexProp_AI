import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Property } from '../models/property';
import { ApiResponse } from '../models/api-response';

@Injectable({ providedIn: 'root' })
export class SavedPropertiesService {
  private http = inject(HttpClient);
  private readonly API_URL = 'https://localhost:7215/api/users/saved-properties';

  // State management באמצעות Signals בדיוק כמו בשאר הפרויקט
  savedList = signal<Property[]>([]);
  loading = signal<boolean>(false);

  loadSavedProperties(): void {
    this.loading.set(true);
    this.http.get<ApiResponse<Property[]>>(this.API_URL).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.savedList.set(res.data);
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading saved properties:', err);
        this.loading.set(false);
      }
    });
  }

  saveProperty(propertyId: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.API_URL}/${propertyId}`, {}).pipe(
      tap(() => this.loadSavedProperties()) // רענון הרשימה אוטומטית מול השרת
    );
  }

  removeProperty(propertyId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.API_URL}/${propertyId}`).pipe(
      tap(() => this.loadSavedProperties()) 
    );
  }

  isSaved(propertyId: number): boolean {
    return this.savedList().some(p => p.id === propertyId);
  }

  toggleSave(propertyId: number): Observable<ApiResponse<string>> {
    if (this.isSaved(propertyId)) {
      return this.removeProperty(propertyId);
    } else {
      return this.saveProperty(propertyId);
    }
  }
}