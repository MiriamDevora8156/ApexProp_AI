/// <reference types="@types/google.maps" />
import {
  Component, OnInit, OnDestroy, inject, signal, computed,
  viewChild, ElementRef, afterNextRender, Injector, PLATFORM_ID
} from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { PropertyService } from '../../core/services/property';
import { Property } from '../../core/models/property';
import { Chart, registerables } from 'chart.js';
import { Subject } from 'rxjs';
import { takeUntil, retry } from 'rxjs/operators';
import { AIAnalysisResult } from '../../core/models/ai-analysis';
import { NearbyLocation } from '../../core/models/nearbyLocation';
import { PriceHistoryPoint } from '../../core/models/price-history';
import { CompareService } from '../../core/services/compare';
import { SavedPropertiesService } from '../../core/services/saved-properties';
import { AuthService } from '../../core/services/auth';
import { MatTabsModule, MatTabChangeEvent } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SimulatorComponent } from './simulator/simulator';
import { NearbyLocationsComponent } from './nearby-locations/nearby-locations';
import { TimelineComponent } from './timeline/timeline';

Chart.register(...registerables);

type ActiveTab = 'overview' | 'deepdive' | 'future';

@Component({
  selector: 'app-property-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    SimulatorComponent,
    NearbyLocationsComponent,
    TimelineComponent
  ],
  templateUrl: './property-details.html',
  styleUrls: ['./property-details.scss']
})
export class PropertyDetailsComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly propertyService = inject(PropertyService);
  private readonly injector = inject(Injector);
  private readonly platformId = inject(PLATFORM_ID);

  compareService = inject(CompareService);
  savedService = inject(SavedPropertiesService);
  authService = inject(AuthService); // נדרש ל-Freemium

  private destroy$ = new Subject<void>();

  property = signal<Property | null>(null);
  aiAnalysis = signal<AIAnalysisResult | undefined>(undefined);
  nearbyProperties = signal<Property[]>([]);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);
  yearsAhead = signal<number>(5);
  nearbyLocations = signal<NearbyLocation[]>([]);
  priceHistory = signal<PriceHistoryPoint[]>([]);

  animatedScore = signal<number>(0);

  activeTab = signal<ActiveTab>('overview');

  isMarketAnomaly = computed(() => {
    const p = this.property();
    if (!p?.estimatedValue || !p.price) return false;
    return (p.estimatedValue - p.price) / p.price > 0.10;
  });

  anomalyGap = computed(() => {
    const p = this.property();
    if (!p?.estimatedValue || !p.price) return 0;
    return Math.round(p.estimatedValue - p.price);
  });

  priceChartCanvas = viewChild<ElementRef<HTMLCanvasElement>>('priceChart');
  radarChartCanvas = viewChild<ElementRef<HTMLCanvasElement>>('radarChart');
  mapContainer = viewChild<ElementRef<HTMLDivElement>>('mapContainer');

  chart: Chart | null = null;
  radarChart: Chart | null = null;
  map: any = null;

  constructor() {
    afterNextRender(() => {
      if (isPlatformBrowser(this.platformId)) {
        this.initChart();
        this.initMap();
      }
    }, { injector: this.injector });
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const id = params.get('id');
        if (id) this.loadPropertyDetails(+id);
      });
  }

  loadPropertyDetails(id: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.resetChartAndMap();

    this.propertyService.getPropertyById(id)
      .pipe(retry({ count: 2, delay: 1000 }), takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.property.set(res.data);
            this.nearbyLocations.set(res.data.nearbyLocations ?? []);

            this.loadPriceHistory(id);
            this.loadAIAnalysis(id);
            this.loadPricePrediction(id, this.yearsAhead());
            this.loadNearbyProperties(res.data);

            if (isPlatformBrowser(this.platformId)) {
              setTimeout(() => { this.initChart(); this.initMap(); }, 200);
            }
            this.loading.set(false);
          }
        },
        error: (err) => {
          console.error('❌ שגיאה בטעינת נכס:', err);
          this.error.set('לא הצלחתי לטעון את פרטי הנכס.');
          this.loading.set(false);
        }
      });
  }

  loadAIAnalysis(id: number): void {
    this.propertyService.analyzeProperty(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.aiAnalysis.set(res.data);
            this.animateScore(res.data.aiScore);

            this.propertyService.getPropertyById(id)
              .pipe(takeUntil(this.destroy$))
              .subscribe({
                next: (propRes) => {
                  if (propRes.success && propRes.data) {
                    this.property.set(propRes.data);
                    this.nearbyLocations.set(propRes.data.nearbyLocations ?? []);
                    this.addLocationMarkersToMap();
                    setTimeout(() => this.initRadarChart(), 300);
                  }
                }
              });
          }
        },
        error: (err) => console.error('❌ שגיאה בטעינת ה-AI:', err)
      });
  }

  loadPricePrediction(propertyId: number, yearsAhead: number): void {
    this.propertyService.predictPrice(propertyId, yearsAhead)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            const currentProperty = this.property();
            if (currentProperty) {
              this.property.set({ ...currentProperty, estimatedValue: res.data.predictedPrice });
              if (this.chart) { this.chart.destroy(); this.chart = null; }
              this.initChart();
            }
          }
        },
        error: (err) => console.error('⚠️ שגיאה בתחזית:', err)
      });
  }

  loadNearbyProperties(property: Property): void {
    this.propertyService.getByArea(property.latitude, property.longitude, 2)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (properties) => {
          const filtered = properties.filter(p => p.id !== property.id).slice(0, 6);
          this.nearbyProperties.set(filtered);
        },
        error: (err) => console.warn('⚠️ לא נמצאו נכסים סמוכים:', err)
      });
  }

  loadPriceHistory(id: number): void {
    this.propertyService.getPriceHistory(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.priceHistory.set(res.data);
            this.initChart();
          }
        },
        error: (err) => console.warn('⚠️ אין היסטוריית מחיר:', err)
      });
  }

  onYearsChange(event: any): void {
    const value = parseInt(event?.target?.value ?? event, 10);
    if (!isNaN(value) && value > 0 && value <= 50) {
      this.yearsAhead.set(value);
      if (this.property()) this.loadPricePrediction(this.property()!.id, value);
    }
  }

  // הפונקציה התעדכנה כדי לעבוד עם הטאבים של Angular Material
  onTabChange(event: MatTabChangeEvent): void {
    const tabs: ActiveTab[] = ['overview', 'deepdive', 'future'];
    this.activeTab.set(tabs[event.index]);

    if (this.activeTab() === 'deepdive' && isPlatformBrowser(this.platformId)) {
      setTimeout(() => this.initRadarChart(), 100);
    }
  }

  toggleSave(): void {
    const p = this.property();
    if (!p) return;
    this.savedService.toggleSave(p.id).subscribe();
  }

  toggleCompare(): void {
    const p = this.property();
    if (!p) return;
    if (this.compareService.isInList(p.id)) {
      this.compareService.remove(p.id);
    } else {
      this.compareService.add(p);
    }
  }

  animateScore(targetScore: number): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.animatedScore.set(0);
    const duration = 1500;
    const steps = 60;
    const increment = targetScore / steps;
    let current = 0;
    let step = 0;

    try {
      const ctx = new window.AudioContext();
      const oscillator = ctx.createOscillator();
      const gain = ctx.createGain();
      oscillator.connect(gain);
      gain.connect(ctx.destination);
      oscillator.frequency.setValueAtTime(200, ctx.currentTime);
      oscillator.frequency.linearRampToValueAtTime(600, ctx.currentTime + 1.5);
      gain.gain.setValueAtTime(0.05, ctx.currentTime);
      gain.gain.linearRampToValueAtTime(0, ctx.currentTime + 1.5);
      oscillator.start();
      oscillator.stop(ctx.currentTime + 1.5);
    } catch (e) { }

    const interval = setInterval(() => {
      step++;
      current = Math.min(current + increment, targetScore);
      this.animatedScore.set(Math.round(current));
      if (step >= steps) clearInterval(interval);
    }, duration / steps);
  }

  getScorePercentile(): number {
    const myScore = this.aiAnalysis()?.aiScore ?? 0;
    const nearby = this.nearbyProperties();
    if (!nearby.length) return 0;
    const betterThan = nearby.filter(p => p.aiScore < myScore).length;
    return Math.round((betterThan / nearby.length) * 100);
  }

  initRadarChart(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const canvas = this.radarChartCanvas();
    if (!canvas) return;

    if (this.radarChart) { this.radarChart.destroy(); this.radarChart = null; }

    try {
      const ctx = canvas.nativeElement.getContext('2d');
      if (!ctx) return;

      const p = this.property();
      const ai = this.aiAnalysis();
      if (!p) return;

      const locationScore = Math.min(100, this.nearbyLocations().length * 8);
      const avgPricePerSqm = 30000;
      const actualPricePerSqm = p.price / p.areaSqm;
      const priceScore = Math.max(0, Math.min(100, 100 - ((actualPricePerSqm / avgPricePerSqm - 0.5) * 100)));
      const potentialScore = p.estimatedValue
        ? Math.min(100, ((p.estimatedValue - p.price) / p.price) * 200)
        : 50;
      const aiScore = ai?.aiScore ?? (p.aiScore ?? 50);
      const areaScore = Math.min(100, (p.areaSqm / 200) * 100);

      this.radarChart = new Chart(ctx, {
        type: 'radar',
        data: {
          labels: ['📍 מיקום', '💰 מחיר', '📈 פוטנציאל', '🤖 AI Score', '📏 שטח'],
          datasets: [{
            label: p.title,
            data: [locationScore, priceScore, potentialScore, aiScore, areaScore],
            backgroundColor: 'rgba(0, 242, 255, 0.15)',
            borderColor: '#00f2ff',
            borderWidth: 2,
            pointBackgroundColor: '#00f2ff',
            pointBorderColor: '#fff',
            pointRadius: 5,
            pointHoverRadius: 7
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            r: {
              min: 0,
              max: 100,
              ticks: { display: false },
              grid: { color: 'rgba(255, 255, 255, 0.08)' },
              angleLines: { color: 'rgba(255, 255, 255, 0.08)' },
              pointLabels: { color: '#aaa', font: { size: 13 } }
            }
          }
        }
      });
    } catch (e) {
      console.error('❌ שגיאה ביצירת גרף רדאר:', e);
    }
  }

  async initMap(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const mapContainer = this.mapContainer();
    const property = this.property();
    if (!mapContainer || !property || this.map) return;

    try {
      const L = await import('leaflet');
      this.map = L.map(mapContainer.nativeElement).setView(
        [property.latitude, property.longitude], 15
      );
      L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap'
      }).addTo(this.map);

      L.marker([property.latitude, property.longitude])
        .addTo(this.map)
        .bindPopup(`<b>${property.title}</b>`)
        .openPopup();

      this.addLocationMarkersToMap();
    } catch (e) {
      console.warn('⚠️ לא הצלחתי לטעון מפה:', e);
    }
  }

  private async addLocationMarkersToMap(): Promise<void> {
    if (!this.map || !isPlatformBrowser(this.platformId)) return;
    const L = await import('leaflet');

    const categoryColors: Record<string, string> = {
      school: '#3b82f6', kindergarten: '#3b82f6', university: '#3b82f6',
      bus_station: '#f59e0b', train_station: '#f59e0b',
      hospital: '#ef4444', clinic: '#ef4444', pharmacy: '#ef4444',
      park: '#22c55e', playground: '#22c55e',
      supermarket: '#a855f7', shop: '#a855f7', mall: '#a855f7',
      restaurant: '#f97316', cafe: '#f97316'
    };

    for (const loc of this.nearbyLocations()) {
      const color = categoryColors[loc.type] ?? '#94a3b8';
      const icon = L.divIcon({
        html: `<div style="background:${color};width:12px;height:12px;border-radius:50%;border:2px solid white;box-shadow:0 0 6px ${color}"></div>`,
        className: '',
        iconSize: [12, 12]
      });
      L.marker([loc.latitude, loc.longitude], { icon })
        .addTo(this.map)
        .bindPopup(`<b>${loc.name}</b><br>${this.getLocationLabel(loc.type)}`);
    }
  }

  initChart(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const canvas = this.priceChartCanvas();
    if (!canvas) return;

    if (this.chart) { this.chart.destroy(); this.chart = null; }

    try {
      const ctx = canvas.nativeElement.getContext('2d');
      if (!ctx) return;

      const gradient = ctx.createLinearGradient(0, 0, 0, 400);
      gradient.addColorStop(0, 'rgba(0, 242, 255, 0.3)');
      gradient.addColorStop(1, 'rgba(0, 242, 255, 0)');

      const basePrice = this.property()?.price || 2000000;
      const history = this.priceHistory();

      const labels = history.length > 1
        ? history.map(h => new Date(h.recordedAt).toLocaleDateString('he-IL'))
        : [`היום (${new Date().getFullYear()})`, `בעוד ${this.yearsAhead()} שנים`];

      const data = history.length > 1
        ? history.map(h => h.price)
        : [basePrice, this.property()?.estimatedValue ?? basePrice * 1.05 * this.yearsAhead()];

      this.chart = new Chart(ctx, {
        type: 'line',
        data: {
          labels,
          datasets: [{
            label: 'היסטוריית מחיר',
            data,
            borderColor: '#00f2ff',
            backgroundColor: gradient,
            fill: true,
            tension: 0.4,
            borderWidth: 3,
            pointBackgroundColor: '#00f2ff',
            pointBorderColor: '#fff',
            pointRadius: 6,
            pointHoverRadius: 8
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            y: {
              beginAtZero: false,
              ticks: {
                color: '#aaa',
                callback: (value) => typeof value === 'number'
                  ? '₪' + (value / 1000000).toFixed(1) + 'M' : value
              },
              grid: { color: 'rgba(255,255,255,0.05)' }
            },
            x: { ticks: { color: '#aaa' }, grid: { color: 'rgba(255,255,255,0.05)' } }
          }
        }
      });
    } catch (e) {
      console.error('❌ שגיאה ביצירת גרף:', e);
    }
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#00f2ff';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
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

  getPriceGrowthPercent(current: number, predicted: number): string {
    if (!current || !predicted) return '0.0';
    return ((predicted - current) / current * 100).toFixed(1);
  }

  private resetChartAndMap(): void {
    if (this.chart) { this.chart.destroy(); this.chart = null; }
    if (this.radarChart) { this.radarChart.destroy(); this.radarChart = null; }
    if (this.map) { this.map.remove(); this.map = null; }
  }

  ngOnDestroy(): void {
    this.resetChartAndMap();
    this.destroy$.next();
    this.destroy$.complete();
  }

  radarSummary = computed(() => {
    const p = this.property();
    const ai = this.aiAnalysis();
    if (!p || !ai) return 'מנתח נתונים...';

    const locationScore = Math.min(100, this.nearbyLocations().length * 8);
    const pricePerSqm = p.price / p.areaSqm;
    const isCheap = pricePerSqm < 25000;
    const hasPotential = p.estimatedValue && p.estimatedValue > p.price * 1.1;

    if (hasPotential && isCheap) return "💎 אנומליה נדירה: הנכס מתומחר מתחת לשוק עם פוטנציאל השבחה משמעותי.";
    if (locationScore > 80) return "📍 לוקיישן מנצח: הנכס ממוקם באזור רווי תשתיות ומוסדות חינוך.";
    if (ai.aiScore > 85) return "🏆 ציון פרימיום: ה-AI מזהה יציבות גבוהה וסיכון נמוך במיוחד.";
    return "⚡ נכס יציב: מתאים למגורים או להשקעה סולידית לטווח ארוך.";
  });

  exportToPDF() {
    console.log('Generating Intelligence Report for:', this.property()?.title);
    alert('מייצר דו"ח מודיעין AI... הדו"ח יישלח למייל שלך בעוד רגע.');
  }
}