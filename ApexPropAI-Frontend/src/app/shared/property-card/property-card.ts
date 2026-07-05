import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Property } from '../../core/models/property';
import { CompareService } from '../../core/services/compare'; 

@Component({
  selector: 'app-property-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './property-card.html',
  host: { class: 'contents' }
})
export class PropertyCardComponent {
  // מזריקים את השירות של ההשוואה
  compareService = inject(CompareService);

  // הנתונים שהכרטיס מקבל
  @Input({ required: true }) property!: Property;
  @Input() mode: 'saved' | 'compare' = 'saved'; // ברירת המחדל היא 'saved'

  // האירועים שהכרטיס צועק החוצה
  @Output() onToggleSaved = new EventEmitter<number>();
  @Output() onAddCompare = new EventEmitter<Property>();

  getScoreColor(score: number): string {
    if (score >= 80) return '#60A5FA';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
  }
}