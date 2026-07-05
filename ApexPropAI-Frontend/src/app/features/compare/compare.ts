import { Component, signal, inject, OnInit, PLATFORM_ID, viewChild, ElementRef, afterNextRender, Injector, OnDestroy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CompareService } from '../../core/services/compare';
import { PropertyService } from '../../core/services/property';
import { Property } from '../../core/models/property';
import { Chart, registerables } from 'chart.js';
import { PropertyCardComponent } from '../../shared/property-card/property-card';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';

Chart.register(...registerables);

type InvestorProfile = {
    goal: 'return' | 'longterm' | 'living' | null;
    horizon: 'short' | 'medium' | 'home' | null;
    budget: 'low' | 'mid' | 'high' | null;
};

@Component({
    selector: 'app-compare',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule, PropertyCardComponent],
    templateUrl: './compare.html',
    styleUrls: ['./compare.scss']
})
export class CompareComponent implements OnInit, OnDestroy {
    compareService = inject(CompareService);
    private propertyService = inject(PropertyService);
    private route = inject(ActivatedRoute);
    private platformId = inject(PLATFORM_ID);
    private injector = inject(Injector);
    private http = inject(HttpClient);
    private destroy$ = new Subject<void>();

    compareList = signal<Property[]>([]);
    aiVerdict = signal<string | null>(null);
    loadingVerdict = signal(false);
    showAllFields = signal(false);
    copied = signal(false);
    suggestedProperties = signal<Property[]>([]);

    loadingSuggestions = signal(false);

    profile = signal<InvestorProfile>({ goal: null, horizon: null, budget: null });
    profileDone = signal(false);

    radarCanvas = viewChild<ElementRef<HTMLCanvasElement>>('radarChart');
    radarChart: Chart | null = null;

    constructor() {
        afterNextRender(() => {
            if (isPlatformBrowser(this.platformId)) {
                this.buildRadar();
            }
        }, { injector: this.injector });
    }

    ngOnInit(): void {
        // טען מה-URL אם יש ?ids=1,2,3
        const idsParam = this.route.snapshot.queryParamMap.get('ids');
        if (idsParam) {
            const ids = idsParam.split(',').map(Number).filter(Boolean);
            ids.forEach(id => {
                this.propertyService.getPropertyById(id)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe(res => {
                        if (res.success && res.data) {
                            this.compareService.add(res.data);
                            this.compareList.set(this.compareService.compareList());
                            this.buildRadar();
                        }
                    });
            });
        } else {
            this.compareList.set(this.compareService.compareList());
        }
    }

    setProfile(key: keyof InvestorProfile, value: string): void {
        this.profile.update(p => ({ ...p, [key]: value }));
        const p = this.profile();
        if (p.goal && p.horizon && p.budget) {
            this.profileDone.set(true);
            this.buildRadar();
            this.fetchVerdict();
            this.loadSuggestedProperties();
        }
    }

    getWeights(): { economic: number; environmental: number; rooms: number } {
        const goal = this.profile().goal;
        if (goal === 'return') return { economic: 0.6, environmental: 0.2, rooms: 0.2 };
        if (goal === 'living') return { economic: 0.2, environmental: 0.5, rooms: 0.3 };
        return { economic: 0.4, environmental: 0.4, rooms: 0.2 };
    }

    getWeightedScore(p: Property): number {
        const w = this.getWeights();
        const economic = Math.min(100, (30000 / (p.price / p.areaSqm)) * 100);
        const env = p.nearbyLocations?.length ? Math.min(100, p.nearbyLocations.length * 8) : 50;
        const rooms = Math.min(100, p.rooms * 20);
        return Math.round(economic * w.economic + env * w.environmental + rooms * w.rooms);
    }

    buildRadar(): void {
        if (!isPlatformBrowser(this.platformId)) return;
        const canvas = this.radarCanvas();
        if (!canvas || this.compareList().length < 2) return;

        if (this.radarChart) { this.radarChart.destroy(); this.radarChart = null; }

        const colors = ['#00f2ff', '#8c54ff', '#ffd700', '#ff6b6b'];
        const labels = ['מחיר/מ"ר', 'ציון AI', 'שטח', 'חדרים', 'מוסדות'];

        const datasets = this.compareList().map((p, i) => ({
            label: p.title,
            data: [
                Math.min(100, (30000 / (p.price / p.areaSqm)) * 100),
                p.aiScore,
                Math.min(100, (p.areaSqm / 200) * 100),
                Math.min(100, p.rooms * 15),
                Math.min(100, (p.nearbyLocations?.length ?? 0) * 8)
            ],
            borderColor: colors[i],
            backgroundColor: colors[i] + '22',
            pointBackgroundColor: colors[i],
            borderWidth: 2
        }));

        setTimeout(() => {
            const ctx = canvas.nativeElement.getContext('2d');
            if (!ctx) return;
            this.radarChart = new Chart(ctx, {
                type: 'radar',
                data: { labels, datasets },
                options: {
                    responsive: true,
                    scales: {
                        r: {
                            min: 0, max: 100,
                            ticks: { color: '#666', stepSize: 20 },
                            grid: { color: 'rgba(255,255,255,0.05)' },
                            pointLabels: { color: '#aaa', font: { size: 13 } }
                        }
                    },
                    plugins: { legend: { labels: { color: '#ccc' } } }
                }
            });
        }, 100);
    }

    fetchVerdict(): void {
        if (this.compareList().length < 2) return;
        this.loadingVerdict.set(true);

        const profile = this.profile();
        const body = {
            propertyIds: this.compareList().map(p => p.id),
            goal: profile.goal,
            horizon: profile.horizon,
            budget: profile.budget
        };

        this.http.post<{ data: string }>('https://localhost:7215/api/ai/compare-verdict', body)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.aiVerdict.set(data.data ?? 'לא הצלחתי לקבל המלצה.');
                    this.loadingVerdict.set(false);
                },
                error: (err) => {
                    console.warn('⚠️ AI Verdict נכשל, משתמש ב-fallback:', err);
                    this.aiVerdict.set(this.buildLocalVerdict());
                    this.loadingVerdict.set(false);
                }
            });
    }

    getBest(field: keyof Property): number {
        const list = this.compareList();
        if (!list.length) return -1;
        let bestIdx = 0;
        list.forEach((p, i) => {
            const val = p[field] as number;
            const bestVal = list[bestIdx][field] as number;
            if (field === 'price') { if (val < bestVal) bestIdx = i; }
            else { if (val > bestVal) bestIdx = i; }
        });
        return bestIdx;
    }

    removeFromCompare(id: number): void {
        this.compareService.remove(id);
        this.compareList.set(this.compareService.compareList());
        this.buildRadar();
    }

    addToCompare(property: Property): void {
        this.compareService.add(property);
        this.compareList.set([...this.compareService.compareList()]);
        setTimeout(() => this.buildRadar(), 100);
    }

    shareUrl(): void {
        const ids = this.compareList().map(p => p.id).join(',');
        const url = `${window.location.origin}/compare?ids=${ids}`;
        navigator.clipboard.writeText(url).then(() => {
            this.copied.set(true);
            setTimeout(() => this.copied.set(false), 2000);
        });
    }

    getScoreColor(score: number): string {
        if (score >= 80) return '#00f2ff';
        if (score >= 60) return '#ffd700';
        if (score >= 40) return '#ff8c00';
        return '#ff6b6b';
    }

    // ── Fallback Verdict מקומי ────────────────────────────────────────────────
    private buildLocalVerdict(): string {
        const list = this.compareList();
        const goal = this.profile().goal;
        const goalLabel = goal === 'return' ? 'תשואה' : goal === 'living' ? 'איכות חיים' : 'ערך לטווח ארוך';

        const scored = list
            .map(p => ({ title: p.title, score: this.getWeightedScore(p), price: p.price }))
            .sort((a, b) => b.score - a.score);

        const best = scored[0];
        const second = scored[1];
        const gap = best.score - second.score;
        const gapText = gap > 15 ? 'מנצח בפער משמעותי' : gap > 5 ? 'עדיף במקצת' : 'עדיף בקושי';
        const cheaper = best.price < second.price ? ' בנוסף, הוא זול יותר — יתרון כפול.' : '';

        return `על בסיס פרופיל "${goalLabel}" — ${best.title} ${gapText} (ציון ${best.score} לעומת ${second.score}).${cheaper}`;
    }

    // ── נכסים מומלצים דומים ───────────────────────────────────────────────────
    loadSuggestedProperties(): void {
        const list = this.compareList();
        if (!list.length) return;

        this.loadingSuggestions.set(true);
        const goal = this.profile().goal;

        // משתמש ב-getProperties כ-fallback בטוח במקום getByArea
        this.propertyService.getProperties()
        .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (res) => {
                    if (!res.success || !res.data) {
                        this.loadingSuggestions.set(false);
                        return;
                    }
                    const existingIds = new Set(list.map(p => p.id));
                    const sorted = res.data
                        .filter(p => !existingIds.has(p.id))
                        .sort((a, b) => {
                            if (goal === 'return') return (b.estimatedValue ?? 0) - (a.estimatedValue ?? 0);
                            if (goal === 'living') return b.areaSqm - a.areaSqm;
                            return b.aiScore - a.aiScore;
                        })
                        .slice(0, 3);
                    this.suggestedProperties.set(sorted);
                    this.loadingSuggestions.set(false);
                },
                error: () => this.loadingSuggestions.set(false)
            });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        if (this.radarChart) { this.radarChart.destroy(); }
    }
}