import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Property } from '../../../core/models/property';

@Component({
  selector: 'app-mobile-sheet',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './mobile-sheet.html',
  styleUrls: ['../home.scss']
})
export class MobileSheetComponent {
  @Input() drawerOpen: boolean = false;
  @Input() properties: Property[] = [];

  getScoreColor(score: number): string {
    if (score >= 80) return '#00f2ff';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
  }
}