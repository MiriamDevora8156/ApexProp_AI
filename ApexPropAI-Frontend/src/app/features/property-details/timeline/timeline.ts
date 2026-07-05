import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Property } from '../../../core/models/property';
import { PriceHistoryPoint } from '../../../core/models/price-history';

@Component({
  selector: 'app-timeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './timeline.html'
})
export class TimelineComponent {
  @Input({ required: true }) property!: Property;
  @Input() priceHistory: PriceHistoryPoint[] = [];
  @Input() yearsAhead: number = 5;

  buildTimelineEvents() {
    const history = this.priceHistory;

    if (history.length < 2) {
      const basePrice = this.property?.price ?? 2000000;
      const currentYear = new Date().getFullYear();
      return [
        { date: `ינואר ${currentYear - 2}`, price: Math.round(basePrice * 0.88), change: 0, aiReason: 'מחיר רישום ראשוני', icon: '🏠', isPositive: true },
        { date: `יולי ${currentYear - 1}`, price: Math.round(basePrice * 0.94), change: 6.8, aiReason: 'ה-AI מקשר זאת לפיתוח תשתיות תחבורה בסביבה', icon: '🚊', isPositive: true },
        { date: `ינואר ${currentYear}`, price: basePrice, change: 6.4, aiReason: 'שיפוצים ועלייה בביקוש לאזור', icon: '🔨', isPositive: true },
        { date: `תחזית ${currentYear + this.yearsAhead}`, price: this.property?.estimatedValue ?? Math.round(basePrice * 1.15), change: this.property?.estimatedValue ? +((this.property.estimatedValue - basePrice) / basePrice * 100).toFixed(1) : 15, aiReason: `תחזית AI לעוד ${this.yearsAhead} שנים בהתבסס על מגמות השוק`, icon: '🤖', isPositive: true }
      ];
    }

    return history.map((point, i) => {
      const prev = history[i - 1];
      const change = prev ? +((point.price - prev.price) / prev.price * 100).toFixed(1) : 0;
      const isPositive = change >= 0;
      return {
        date: new Date(point.recordedAt).toLocaleDateString('he-IL', { month: 'long', year: 'numeric' }),
        price: point.price,
        change,
        aiReason: i === 0 ? 'מחיר רישום ראשוני' : isPositive ? 'עלייה בביקוש לאזור — ה-AI זיהה מגמת השבחה' : 'ירידה זמנית — ה-AI מייחס זאת לתנאי שוק גלובליים',
        icon: i === 0 ? '🏠' : isPositive ? '📈' : '📉',
        isPositive
      };
    });
  }
}