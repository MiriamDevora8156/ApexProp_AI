import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NearbyLocation } from '../../../core/models/nearbyLocation';

@Component({
  selector: 'app-nearby-locations',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './nearby-locations.html'
})
export class NearbyLocationsComponent {
  @Input({ required: true }) nearbyLocations: NearbyLocation[] = [];

  openCategory = signal<string | null>(null);

  toggleCategory(label: string): void {
    this.openCategory.set(this.openCategory() === label ? null : label);
  }

  getLocationsByCategory() {
    const map = new Map<string, { name: string }[]>();
    for (const loc of this.nearbyLocations) {
      const label = this.getLocationLabel(loc.type);
      if (!map.has(label)) map.set(label, []);
      map.get(label)!.push({ name: loc.name });
    }
    return Array.from(map.entries()).map(([label, items]) => ({
      type: this.nearbyLocations.find(l => this.getLocationLabel(l.type) === label)?.type ?? '',
      label,
      icon: this.getLocationIcon(
        this.nearbyLocations.find(l => this.getLocationLabel(l.type) === label)?.type ?? ''
      ),
      count: items.length,
      items
    })).sort((a, b) => b.count - a.count);
  }

  getLocationIcon(type: string): string {
    const icons: Record<string, string> = {
      school: '🏫', kindergarten: '🎒', university: '🎓', college: '📚', library: '📖',
      bus_station: '🚌', train_station: '🚆', tram_stop: '🚊', parking: '🅿️', taxi: '🚕',
      park: '🌳', playground: '🛝', swimming_pool: '🏊', sports_centre: '⚽', cinema: '🎬', theatre: '🎭',
      hospital: '🏥', clinic: '🩺', pharmacy: '💊', dentist: '🦷', veterinary: '🐾',
      supermarket: '🛒', shop: '🏪', market: '🏬', mall: '🛍️', bakery: '🥖',
      restaurant: '🍽️', cafe: '☕', bar: '🍺', fast_food: '🍔'
    };
    return icons[type] ?? '📍';
  }

  getLocationLabel(type: string): string {
    const labels: Record<string, string> = {
      school: 'בית ספר', kindergarten: 'גן ילדים', university: 'אוניברסיטה',
      college: 'מכללה', library: 'ספרייה',
      bus_station: 'תחנת אוטובוס', train_station: 'תחנת רכבת', tram_stop: 'תחנת רכבל',
      parking: 'חניון', taxi: 'תחנת מונית',
      park: 'פארק', playground: 'גן שעשועים', swimming_pool: 'בריכה',
      sports_centre: 'מרכז ספורט', cinema: 'קולנוע', theatre: 'תיאטרון',
      hospital: 'בית חולים', clinic: 'קליניקה', pharmacy: 'בית מרקחת',
      dentist: 'רופא שיניים', veterinary: 'וטרינר',
      supermarket: 'סופרמרקט', shop: 'חנות', market: 'שוק',
      mall: 'קניון', bakery: 'מאפייה',
      restaurant: 'מסעדה', cafe: 'קפה', bar: 'בר', fast_food: 'מזון מהיר'
    };
    return labels[type] ?? type;
  }
}