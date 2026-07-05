import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

type PropertyCategory = 'all' | 'apartments' | 'penthouse' | 'investment';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './search-bar.html',
  styleUrls: ['../home.scss']
})
export class SearchBarComponent {
  @Input() searchTerm: string = '';
  @Input() category: PropertyCategory = 'all';
  @Input() compareCount: number = 0;
  @Input() showAutocomplete: boolean = false;
  @Input() autocompleteResults: { label: string; lat: number; lng: number }[] = [];

  @Output() searchChanged = new EventEmitter<Event>();
  @Output() searchCleared = new EventEmitter<void>();
  @Output() autocompleteSelected = new EventEmitter<{ label: string; lat: number; lng: number }>();
  @Output() autocompleteHidden = new EventEmitter<void>();
  @Output() categoryChanged = new EventEmitter<PropertyCategory>();

  readonly categories = [
    { key: 'all'        as PropertyCategory, label: 'הכל',     icon: 'ph-bold ph-squares-four' },
    { key: 'apartments' as PropertyCategory, label: 'דירות',   icon: 'ph-bold ph-buildings' },
    { key: 'penthouse'  as PropertyCategory, label: 'פנטהאוז', icon: 'ph-bold ph-crown' },
    { key: 'investment' as PropertyCategory, label: 'להשקעה',  icon: 'ph-bold ph-currency-circle-dollar' },
  ];
}