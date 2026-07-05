import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Property, PropertyPage } from '../models/property';
import { tap, shareReplay, map } from 'rxjs/operators';
import { AIAnalysisResult, PricePrediction } from '../models/ai-analysis';
import { PriceHistoryPoint } from '../models/price-history';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private readonly http = inject(HttpClient);
  private readonly API_URL = 'https://localhost:7215/api';

  private propertiesCache$ = new BehaviorSubject<Property[] | null>(null);
  public propertiesCache = this.propertiesCache$.asObservable();

  getProperties(): Observable<{ success: boolean; data: Property[] }> {
    return this.http.get<{ success: boolean; data: Property[] }>(
      `${this.API_URL}/properties`
    ).pipe(
      /**
       * ❌ מה היה שגוי:
       *   shareReplay(1) — ב-RxJS 6+, ברירת המחדל של refCount היא false.
       *   זה אומר שה-subscription נשאר פעיל לנצח גם כשאין מנויים.
       *   אם המשתמש נווט לדף אחר, הבקשה ממשיכה "לחיות" בזיכרון.
       *
       * ✅ מה תוקן:
       *   shareReplay({ bufferSize: 1, refCount: true })
       *   refCount: true = כשאין יותר מנויים, ה-subscription נסגר.
       *   זה מונע memory leak ובקשות HTTP מיותרות.
       */
      shareReplay({ bufferSize: 1, refCount: true }),
      tap(res => {
        this.propertiesCache$.next(res.data);
      })
    );
  }

  getPropertyById(id: number): Observable<{ success: boolean; data: Property }> {
    return this.http.get<{ success: boolean; data: Property }>(
      `${this.API_URL}/properties/${id}`
    );
  }

  search(filters: {
    searchTerm?: string;
    minPrice?: number;
    maxPrice?: number;
    minRooms?: number;
    maxRooms?: number;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<{ success: boolean; data: PropertyPage }> {
    let params = new HttpParams();
    if (filters.searchTerm) params = params.set('searchTerm', filters.searchTerm);
    if (filters.minPrice)   params = params.set('minPrice', filters.minPrice.toString());
    if (filters.maxPrice)   params = params.set('maxPrice', filters.maxPrice.toString());
    if (filters.minRooms)   params = params.set('minRooms', filters.minRooms.toString());
    if (filters.maxRooms)   params = params.set('maxRooms', filters.maxRooms.toString());
    params = params.set('pageNumber', (filters.pageNumber || 1).toString());
    params = params.set('pageSize', (filters.pageSize || 20).toString());

    return this.http.get<{ success: boolean; data: PropertyPage }>(
      `${this.API_URL}/properties/search`, { params }
    );
  }

  getByArea(lat: number, lng: number, radiusKm = 5): Observable<Property[]> {
  const params = new HttpParams()
    .set('lat', lat.toString())
    .set('lng', lng.toString())
    .set('radiusKm', radiusKm.toString());

  // ה-API מחזיר { success, data } — לא Property[] ישיר
  return this.http
    .get<{ success: boolean; data: Property[] }>(
      `${this.API_URL}/properties/area`, { params }
    )
    .pipe(map(res => res.data ?? []));
}

  getTopByScore(count = 10): Observable<Property[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<Property[]>(`${this.API_URL}/properties/top`, { params });
  }

  createProperty(property: Partial<Property>): Observable<{ success: boolean; data: Property }> {
    return this.http.post<{ success: boolean; data: Property }>(
      `${this.API_URL}/properties`, property
    ).pipe(
      tap(res => {
        const current = this.propertiesCache$.value || [];
        this.propertiesCache$.next([...current, res.data]);
      })
    );
  }

  analyzeProperty(id: number): Observable<{ success: boolean; data: AIAnalysisResult }> {
    return this.http.post<{ success: boolean; data: AIAnalysisResult }>(
      `${this.API_URL}/ai/analyze/${id}`, {}
    );
  }

  predictPrice(id: number, yearsAhead = 5): Observable<{ success: boolean; data: PricePrediction }> {
    const params = new HttpParams().set('yearsAhead', yearsAhead.toString());
    return this.http.get<{ success: boolean; data: PricePrediction }>(
      `${this.API_URL}/ai/predict-price/${id}`, { params }
    );
  }

  // Utility methods
  getPricePerSqm(price: number, areaSqm: number): number {
    return areaSqm > 0 ? Math.round(price / areaSqm) : 0;
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('he-IL', {
      style: 'currency',
      currency: 'ILS',
      maximumFractionDigits: 0
    }).format(price);
  }

  getPropertyImage(property: Property): string {
    return property.images?.length > 0
      ? property.images[0]
      : 'assets/images/placeholder-house.jpg';
  }

  getPriceHistory(id: number): Observable<{ success: boolean; data: PriceHistoryPoint[] }> {
  return this.http.get<{ success: boolean; data: PriceHistoryPoint[] }>(
    `${this.API_URL}/properties/${id}/price-history`
  );
}
}
