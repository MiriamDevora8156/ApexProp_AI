import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Property } from '../../../core/models/property';

interface MarketStats {
  sparklineData: number[];
  heatIndex: number;
  recentTransactions: { address: string; price: number; date: Date; type: 'purchase' | 'lease' }[];
  avgPrice: number;
  priceChange: string;
  areaDescription: string;
}

interface GlobalStats {
  totalProperties: number;
  avgPrice: number;
  avgScore: number;
  maxScore: number;
  minPrice: number;
  maxPrice: number;
}

@Component({
  selector: 'app-market-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './market-dashboard.html',
  styleUrls: ['../home.scss']
})
export class MarketDashboardComponent {
  @Input() marketStats: MarketStats | null = null;
  @Input() defaultStats: MarketStats | null = null;
  @Input() globalStats: GlobalStats | null = null;
  @Input() featuredProperties: Property[] = [];
  @Input() scanning: boolean = false;

  @Output() drawerClosed        = new EventEmitter<void>();
  @Output() scanClicked         = new EventEmitter<void>();
  @Output() propertiesRequested = new EventEmitter<void>();
  @Output() cardScrollRequested = new EventEmitter<number>();

  formatPrice(n: number): string {
    if (n >= 1_000_000) return `₪${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000)     return `₪${Math.round(n / 1_000)}K`;
    return `₪${n}`;
  }

  getMinValue(arr: number[]): number { return Math.min(...arr); }
  getMaxValue(arr: number[]): number { return Math.max(...arr); }
}