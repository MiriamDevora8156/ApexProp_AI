import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SavedPropertiesService } from '../../core/services/saved-properties';
import { PropertyCardComponent } from '../../shared/property-card/property-card';

@Component({
    selector: 'app-saved',
    standalone: true,
    imports: [CommonModule, RouterModule, PropertyCardComponent],
    templateUrl: './saved.html',
    styleUrls: ['./saved.scss']
})
export class SavedComponent implements OnInit {
    savedService = inject(SavedPropertiesService);

    ngOnInit(): void {
        // טעינת הנתונים בעת כניסה לדף
        this.savedService.loadSavedProperties();
    }

    removeProperty(id: number): void {
        this.savedService.removeProperty(id).subscribe();
    }

}