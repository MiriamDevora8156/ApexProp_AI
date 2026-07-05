// src/app/core/services/compare.service.ts
import { Injectable, signal, computed } from '@angular/core';
import { Property } from '../models/property';

@Injectable({ providedIn: 'root' })
export class CompareService {
  private list = signal<Property[]>([]);
  
  compareList = this.list.asReadonly();
  count = computed(() => this.list().length);

  add(property: Property): void {
    if (this.list().length >= 4) return;
    if (this.list().some(p => p.id === property.id)) return;
    this.list.update(l => [...l, property]);
  }

  remove(id: number): void {
    this.list.update(l => l.filter(p => p.id !== id));
  }

  isInList(id: number): boolean {
    return this.list().some(p => p.id === id);
  }

  clear(): void {
    this.list.set([]);
  }

  // מחליף מצב - אם קיים מסיר, אם לא קיים מוסיף (נשתמש בזה בדף הבית)
  toggleProperty(property: Property) {
    if (this.isInCompare(property.id)) {
      this.remove(property.id);
    } else {
      this.add(property);
    }
  }

  // בודק אם הנכס כבר נבחר
  isInCompare(id: number) {
    return this.list().some(p => p.id === id);
  }
}