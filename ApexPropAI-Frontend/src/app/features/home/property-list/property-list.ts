import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Property } from '../../../core/models/property';

type SortField = 'aiScore' | 'price' | 'areaSqm' | 'createdAt';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './property-list.html',
  styleUrls: ['../home.scss']
})
export class PropertyListComponent {
  @Input() properties: Property[] = [];
  @Input() loading: boolean = false;
  @Input() error: string | null = null;
  @Input() sortField: SortField = 'aiScore';
  @Input() hoveredPropertyId: number | null = null;
  @Input() scanResultCount: number | null = null;
  @Input() anomalyIds: Set<number> = new Set();

  @Output() sortChanged = new EventEmitter<SortField>();
  @Output() propertyHovered = new EventEmitter<number | null>();
  @Output() refreshRequested = new EventEmitter<void>();
  @Output() zoomOutRequested = new EventEmitter<void>();
  @Output() drawerClosed = new EventEmitter<void>();
  @Output() drawerSideToggled = new EventEmitter<void>();

  readonly sortOptions = [
    { key: 'aiScore'   as SortField, label: 'AI ↓',   icon: 'ph-bold ph-robot' },
    { key: 'price'     as SortField, label: 'מחיר ↑', icon: 'ph-bold ph-currency-ils' },
    { key: 'areaSqm'   as SortField, label: 'שטח ↓',  icon: 'ph-bold ph-ruler' },
    { key: 'createdAt' as SortField, label: 'חדש ↓',  icon: 'ph-bold ph-clock' },
  ];

  isAnomaly(id: number): boolean {
    return this.anomalyIds.has(id);
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#00f2ff';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
  }

  formatPrice(n: number): string {
    if (n >= 1_000_000) return `₪${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000)     return `₪${Math.round(n / 1_000)}K`;
    return `₪${n}`;
  }
}