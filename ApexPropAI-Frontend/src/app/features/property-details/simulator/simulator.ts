import { Component, Input, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Property } from '../../../core/models/property';
import { AIAnalysisResult } from '../../../core/models/ai-analysis';
import { NearbyLocation } from '../../../core/models/nearbyLocation';

@Component({
  selector: 'app-simulator',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './simulator.html'
})
export class SimulatorComponent implements OnInit {
  @Input({ required: true }) property!: Property;
  @Input() aiAnalysis?: AIAnalysisResult;
  @Input() nearbyLocations: NearbyLocation[] = [];

  simPrice = signal<number>(0);
  simRooms = signal<number>(0);
  simArea = signal<number>(0);
  simScore = signal<number | null>(null);

  ngOnInit(): void {
    this.simPrice.set(this.property.price);
    this.simRooms.set(this.property.rooms);
    this.simArea.set(this.property.areaSqm);
  }

  calculateSimScore(): void {
    const price = this.simPrice();
    const area = this.simArea();
    const rooms = this.simRooms();
    const nearbyCount = this.nearbyLocations.length;

    if (!area || area <= 0) return;

    const pricePerSqm = price / area;
    const avgPricePerSqm = 30000;
    const priceRatio = pricePerSqm / avgPricePerSqm;

    let economicScore = 50;
    if (priceRatio < 0.7) economicScore += 30;
    else if (priceRatio < 0.85) economicScore += 20;
    else if (priceRatio < 1.0) economicScore += 10;
    else if (priceRatio < 1.2) economicScore -= 10;
    else economicScore -= 20;

    if (area > 150) economicScore += 5;
    else if (area < 80) economicScore -= 5;
    if (rooms >= 3 && rooms <= 4) economicScore += 10;
    economicScore = Math.max(0, Math.min(100, economicScore));

    const envScore = Math.min(100, nearbyCount * 8);
    const finalScore = (economicScore * 0.5) + (envScore * 0.5);
    this.simScore.set(Math.round(Math.max(0, Math.min(100, finalScore))));
  }

  getSimScoreDiff(): number {
    const real = this.aiAnalysis?.aiScore ?? 0;
    const sim = this.simScore() ?? real;
    return Math.round(sim - real);
  }

  getSimScoreColor(): string {
    const score = this.simScore() ?? 0;
    if (score >= 80) return '#00f2ff';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
  }
}