import {
  Component, OnInit, OnDestroy,
  inject, signal, computed, effect,
  PLATFORM_ID, afterNextRender, Injector
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PropertyService } from '../../core/services/property';
import { CompareService } from '../../core/services/compare';
import { Property } from '../../core/models/property';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SearchBarComponent } from './search-bar/search-bar';
import { PropertyListComponent } from './property-list/property-list';
import { CompareFloaterComponent }  from './compare-floater/compare-floater';
import { MobileSheetComponent }     from './mobile-sheet/mobile-sheet';
import { RightToolbarComponent }    from './right-toolbar/right-toolbar';
import { MarketDashboardComponent } from './market-dashboard/market-dashboard';
import { Transaction } from '../../core/models/Transaction';
import { MarketStats } from '../../core/models/marketStats';

type MobileView    = 'list' | 'map';
type HeatmapLayer  = 'demand' | 'opportunity' | 'growth';
type PropertyCategory = 'all' | 'apartments' | 'penthouse' | 'investment';
type SortField     = 'aiScore' | 'price' | 'areaSqm' | 'createdAt';
type MapStyle      = 'dark' | 'satellite' | 'street';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SearchBarComponent, PropertyListComponent, CompareFloaterComponent, MobileSheetComponent, RightToolbarComponent, MarketDashboardComponent],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly propertyService = inject(PropertyService);
  public  readonly compareService  = inject(CompareService);
  private readonly platformId      = inject(PLATFORM_ID);
  private readonly injector        = inject(Injector);
  private readonly destroy$        = new Subject<void>();

  // ── Math helper for templates ────────────────────────────────
  readonly Math = Math;

  // ── Leaflet internals ────────────────────────────────────────
  private L: any;
  private mapInstance: any   = null;
  private clusterGroup: any  = null;
  private heatLayer: any     = null;
  private tileLayerInst: any = null;
  public  drawnPolygon: any  = null;   // public → template checks
  private drawPoints: [number, number][] = [];
  private pinMarkers = new Map<number, any>(); // id → marker ref

  // ── UI config arrays (static) ────────────────────────────────
  readonly heatmapOptions = [
    { key: 'demand'      as HeatmapLayer, label: 'ביקוש',       icon: 'ph-bold ph-drop' },
    { key: 'opportunity' as HeatmapLayer, label: 'הזדמנויות',   icon: 'ph-bold ph-diamond' },
    { key: 'growth'      as HeatmapLayer, label: 'עליית מחיר',  icon: 'ph-bold ph-trend-up' },
  ];

  readonly mapStyles = [
    { key: 'dark'      as MapStyle, label: 'לילה',     icon: 'ph-bold ph-moon' },
    { key: 'satellite' as MapStyle, label: 'לווין',    icon: 'ph-bold ph-globe' },
    { key: 'street'    as MapStyle, label: 'רחובות',   icon: 'ph-bold ph-map-trifold' },
  ];

  // ── Core signals ─────────────────────────────────────────────
  public readonly properties        = signal<Property[]>([]);
  public readonly loading           = signal<boolean>(true);
  public readonly error             = signal<string | null>(null);
  public readonly searchTerm        = signal<string>('');
  public readonly autocompleteResults = signal<{ label: string; lat: number; lng: number }[]>([]);
  public readonly showAutocomplete  = signal<boolean>(false);

  // ── Map / UI signals ─────────────────────────────────────────
  public readonly drawerOpen        = signal<boolean>(false);
  public readonly hoveredPropertyId = signal<number | null>(null);
  public readonly mapTileStyle      = signal<MapStyle>('dark');
  public readonly scanning          = signal<boolean>(false);
  public readonly scanResultCount   = signal<number | null>(null);
  public readonly heatmapActive     = signal<boolean>(false);
  public readonly heatmapLayer      = signal<HeatmapLayer>('demand');
  public readonly showHeatmapMenu   = signal<boolean>(false);
  public readonly category          = signal<PropertyCategory>('all');
  public readonly sortField         = signal<SortField>('aiScore');
  public readonly drawModeActive    = signal<boolean>(false);
  public readonly mobileView        = signal<MobileView>('map');
  
  // ── Halo animation signals ───────────────────────────────────
  public readonly haloActive        = signal<boolean>(false);
  public readonly haloColor         = signal<string>('rgba(0, 242, 255, 0.4)');

  // ── Drawer flip signals ───────────────────────────────────────
  public readonly drawerShowingProperties = signal<boolean>(false);
  public readonly marketStats       = signal<MarketStats | null>(null);
  public readonly defaultStats      = signal<MarketStats | null>(null);
  public readonly showingDefaultState = signal<boolean>(true);
  public readonly selectedPolygonPoints = signal<[number, number][] | null>(null);

  // ── Computed ─────────────────────────────────────────────────
  public readonly filteredProperties = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const cat  = this.category();
    const sort = this.sortField();
    let list   = this.properties();

    if (term) list = list.filter(p =>
      p.title.toLowerCase().includes(term) || p.address.toLowerCase().includes(term)
    );
    if (cat !== 'all') list = list.filter(p => this.matchCategory(p, cat));

    return [...list].sort((a, b) => {
      if (sort === 'price')     return a.price - b.price;
      if (sort === 'areaSqm')   return b.areaSqm - a.areaSqm;
      if (sort === 'createdAt') return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      return b.aiScore - a.aiScore;
    });
  });

  public readonly anomalyPropertyIds = computed(() => {
    const set = new Set<number>();
    for (const p of this.properties())
      if (p.estimatedValue && p.estimatedValue > p.price) set.add(p.id);
    return set;
  });

  public readonly globalStats = computed(() => {
    const all = this.properties();
    if (!all.length) return null;
    const avgPrice = Math.round(all.reduce((sum, p) => sum + p.price, 0) / all.length);
    const avgScore = all.reduce((sum, p) => sum + p.aiScore, 0) / all.length;
    return {
      totalProperties: all.length,
      avgPrice,
      avgScore: Math.round(avgScore),
      maxScore: Math.max(...all.map(p => p.aiScore)),
      minPrice: Math.min(...all.map(p => p.price)),
      maxPrice: Math.max(...all.map(p => p.price))
    };
  });

  public readonly featuredProperties = computed(() => {
    const all = this.properties();
    return all
      .sort((a, b) => b.aiScore - a.aiScore)
      .slice(0, 5);
  });

  public readonly hudStats = computed(() => {
    const list = this.filteredProperties();
    if (!list.length) return null;
    return {
      count: list.length,
      avgPrice: Math.round(list.reduce((s, p) => s + p.price, 0) / list.length)
    };
  });

  public readonly polygonFilteredProperties = computed(() => {
    const polygonPoints = this.selectedPolygonPoints();
    let list = this.filteredProperties();

    if (!polygonPoints) return list;

    return list.filter((p: Property) => {
      if (!p.latitude || !p.longitude) return false;
      const latlng = this.L.latLng(p.latitude, p.longitude);
      const polygon = this.L.polygon(polygonPoints);
      return polygon.getBounds().contains(latlng);
    });
  });

  // ── Lifecycle ─────────────────────────────────────────────────
  constructor() {
    afterNextRender(() => {
      if (isPlatformBrowser(this.platformId)) this.initMap();
    }, { injector: this.injector });

    // Update halo color when hoveredPropertyId changes
    effect(() => {
      const hoveredId = this.hoveredPropertyId();
      if (hoveredId !== null) {
        const prop = this.properties().find(p => p.id === hoveredId);
        if (prop) {
          this.haloColor.set(this.getHaloColor(prop.aiScore));
        }
      } else {
        this.haloColor.set(this.getHaloColor(null));
      }
    });

    // Update market stats when filtered properties change or drawer opens
    effect(() => {
      const drawer = this.drawerOpen();
      const filtered = this.filteredProperties();
      const isDefault = this.showingDefaultState();
      
      if (!drawer && isDefault) {
        // Generate default stats immediately when drawer opens for the first time
        setTimeout(() => this.generateDefaultStats(), 50);
      } else if (drawer && !isDefault) {
        // Generate filtered stats when showing scan results
        setTimeout(() => this.generateMarketStats(), 0);
      }
    });

    // Also generate default stats when properties load
    effect(() => {
      this.properties(); // Track properties change
      this.drawerOpen(); // Track drawer state too
      if (this.showingDefaultState()) {
        this.generateDefaultStats();
      }
    });

    // Reset drawer side when drawer closes
    effect(() => {
      if (!this.drawerOpen()) {
        this.drawerShowingProperties.set(false);
        this.showingDefaultState.set(true);
        this.marketStats.set(null);
      }
    });
  }

  ngOnInit(): void { 
    this.loadProperties();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.mapInstance?.remove();
  }

  // ── Data ──────────────────────────────────────────────────────
  public loadProperties(): void {
    this.loading.set(true);
    this.error.set(null);
    this.propertyService.getProperties().pipe(takeUntil(this.destroy$)).subscribe({
      next: r => {
        if (r.success && r.data) {
          this.properties.set(r.data.map(p => ({
            ...p,
            estimatedValue: p.estimatedValue ?? Math.round(p.price * 1.12)
          })));
          this.scanResultCount.set(r.data.length);
          setTimeout(() => this.renderPins(), 0);
          // Open drawer after properties load to show default dashboard
          setTimeout(() => this.drawerOpen.set(true), 100);
        } else {
          this.error.set('השרת החזיר תגובה לא תקינה.');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('חיבור לשרת נכשל.');
        this.loading.set(false);
      }
    });
  }

  public refreshResults(): void { this.loadProperties(); }

  // ── Map init ──────────────────────────────────────────────────
  private async initMap(): Promise<void> {
    try {
      this.L = await import('leaflet' as any);
      // leaflet.markercluster — dynamic import
      await import('leaflet.markercluster' as any);

      const el = document.getElementById('apex-map');
      if (!el) return;

      this.mapInstance = this.L.map('apex-map', {
        center: [32.0853, 34.7818],
        zoom: 12,
        zoomControl: false,
        attributionControl: false,
      });

      this.L.control.zoom({ position: 'bottomleft' }).addTo(this.mapInstance);
      this.applyTile();
      this.renderPins();

      // Draw mode — click to add polygon points
      this.mapInstance.on('click', (e: any) => {
        if (!this.drawModeActive()) return;
        this.drawPoints.push([e.latlng.lat, e.latlng.lng]);
        this.renderDrawing();
      });

      this.mapInstance.on('dblclick', (e: any) => {
        if (!this.drawModeActive()) return;
        e.originalEvent.preventDefault();
        this.finalizePolygon();
      });

    } catch (err) {
      console.error('Map init failed:', err);
    }
  }

  // ── Tile layers ───────────────────────────────────────────────
  private readonly TILES: Record<MapStyle, string> = {
    dark:      'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
    satellite: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
    street:    'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
  };

  private applyTile(): void {
    if (!this.mapInstance || !this.L) return;
    this.tileLayerInst?.remove();
    this.tileLayerInst = this.L.tileLayer(this.TILES[this.mapTileStyle()], { maxZoom: 19 })
      .addTo(this.mapInstance);
    // Re-render pins so they stay visible on all tile styles
    setTimeout(() => this.renderPins(), 100);
  }

  public setMapStyle(s: MapStyle): void {
    this.mapTileStyle.set(s);
    this.applyTile();
  }

  // ── Scan ──────────────────────────────────────────────────────
  public async scanArea(): Promise<void> {
    if (!this.mapInstance || this.scanning()) return;
    this.scanning.set(true);
    this.showingDefaultState.set(false);
    await new Promise(r => setTimeout(r, 700));

    const bounds  = this.mapInstance.getBounds();
    const visible = this.properties().filter(p =>
      bounds.contains(this.L.latLng(p.latitude, p.longitude))
    );
    this.scanResultCount.set(visible.length);
    if (visible.length > 0) this.drawerOpen.set(true);
    this.renderPins();

    // Activate halo pulse on scan completion
    this.activateHaloPulse();

    if (visible.length > 1) {
      this.mapInstance.fitBounds(
        visible.map((p: Property) => [p.latitude, p.longitude] as [number, number]),
        { padding: [50, 50], animate: true }
      );
    }
    this.scanning.set(false);
  }

  // ── Teardrop Pins ─────────────────────────────────────────────
  // Teardrop SVG: circle on top + pointed tip at bottom
  private makeTeardropIcon(price: string, color: string, isAnomaly: boolean): any {
    const glow  = isAnomaly ? `drop-shadow(0 0 6px ${color})` : 'none';
    const pulse = isAnomaly ? 'class="pin-pulse"' : '';
    const svg = `
      <svg xmlns="http://www.w3.org/2000/svg" width="52" height="62" viewBox="0 0 52 62" ${pulse}>
        <defs>
          <filter id="glow-${color.replace('#','')}">
            <feGaussianBlur stdDeviation="2" result="blur"/>
            <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
          </filter>
        </defs>
        <!-- Teardrop shape -->
        <path d="M26 2 C12 2 4 12 4 23 C4 38 26 60 26 60 C26 60 48 38 48 23 C48 12 40 2 26 2 Z"
          fill="#0a0a0c" stroke="${color}" stroke-width="2"
          style="filter:${glow}"/>
        <!-- Price text -->
        <text x="26" y="27" text-anchor="middle" dominant-baseline="middle"
          font-family="Inter,sans-serif" font-size="9" font-weight="800" fill="${color}">
          ${price}
        </text>
      </svg>`;
    return this.L.divIcon({
      html: svg,
      className: '',
      iconSize: [52, 62],
      iconAnchor: [26, 60],
      popupAnchor: [0, -62],
    });
  }

  public renderPins(): void {
    if (!this.mapInstance || !this.L) return;

    if (this.clusterGroup) this.mapInstance.removeLayer(this.clusterGroup);
    this.pinMarkers.clear();

    this.clusterGroup = (this.L as any).markerClusterGroup({
      maxClusterRadius: 55,
      iconCreateFunction: (cluster: any) => {
        const n = cluster.getChildCount();
        return this.L.divIcon({
          html: `<div class="cluster-pin"><span>${n}</span></div>`,
          className: '',
          iconSize: [44, 44],
        });
      }
    });

    this.filteredProperties().forEach((p: Property) => {
      if (!p.latitude || !p.longitude) return;

      const color   = this.getScoreColor(p.aiScore);
      const isAnom  = this.anomalyPropertyIds().has(p.id);
      const icon    = this.makeTeardropIcon(this.formatPrice(p.price), color, isAnom);
      const marker  = this.L.marker([p.latitude, p.longitude], { icon });

      marker.on('mouseover', () => {
        this.hoveredPropertyId.set(p.id);
        marker.bindPopup(this.buildPopup(p), {
          closeButton: false, className: 'apex-popup', maxWidth: 230
        }).openPopup();
      });

      marker.on('mouseout', () => {
        this.hoveredPropertyId.set(null);
        marker.closePopup();
      });

      marker.on('click', () => {
        this.drawerOpen.set(true);
        setTimeout(() => this.scrollToCard(p.id), 150);
      });

      this.clusterGroup.addLayer(marker);
      this.pinMarkers.set(p.id, marker);
    });

    this.mapInstance.addLayer(this.clusterGroup);
    this.renderHeatmap();
  }

  public highlightPin(id: number): void {
    const m = this.pinMarkers.get(id);
    if (!m) return;
    const el = m.getElement();
    if (el) el.style.transform += ' scale(1.2)';
  }

  public unhighlightPin(id: number): void {
    const m = this.pinMarkers.get(id);
    if (!m) return;
    const el = m.getElement();
    if (el) el.style.transform = el.style.transform.replace(' scale(1.2)', '');
  }

  private buildPopup(p: Property): string {
    const color = this.getScoreColor(p.aiScore);
    return `<div class="apex-popup-inner" dir="rtl">
      ${p.images?.[0] ? `<img src="${p.images[0]}" alt="${p.title}" style="width:calc(100% + 24px);height:72px;object-fit:cover;margin:-12px -12px 10px;border-radius:10px 10px 0 0">` : ''}
      <div style="font-weight:700;font-size:13px;color:#fff;margin-bottom:3px">${p.title}</div>
      <div style="font-size:11px;color:#94A3B8;margin-bottom:6px">${p.address}</div>
      <div style="display:flex;justify-content:space-between;align-items:center">
        <span style="font-size:13px;font-weight:800;color:#fff">₪${p.price.toLocaleString('he-IL')}</span>
        <span style="font-size:12px;font-weight:700;color:${color}">AI ${p.aiScore}</span>
      </div>
    </div>`;
  }

  // ── Heatmap ───────────────────────────────────────────────────
  public toggleHeatmap(): void {
    this.heatmapActive.update(v => !v);
    this.showHeatmapMenu.set(false);
    this.renderHeatmap();
  }

  public setHeatmapLayer(layer: HeatmapLayer): void {
    this.heatmapLayer.set(layer);
    this.showHeatmapMenu.set(false);
    if (!this.heatmapActive()) this.heatmapActive.set(true);
    this.renderHeatmap();
  }

  private renderHeatmap(): void {
    if (!this.mapInstance || !this.L) return;
    if (this.heatLayer) { this.mapInstance.removeLayer(this.heatLayer); this.heatLayer = null; }
    if (!this.heatmapActive()) return;

    // Inline heatmap using canvas overlay — no leaflet.heat plugin needed
    const type   = this.heatmapLayer();
    const points = this.filteredProperties()
      .filter((p: Property) => p.latitude && p.longitude)
      .map((p: Property) => {
        let intensity = 0.5;
        if (type === 'demand')      intensity = p.aiScore / 100;
        if (type === 'opportunity') intensity = this.anomalyPropertyIds().has(p.id) ? 1 : 0.15;
        if (type === 'growth')      intensity = p.estimatedValue
          ? Math.min(1, (p.estimatedValue - p.price) / p.price * 6) : 0.3;
        return [p.latitude, p.longitude, intensity];
      });

    const gradient =
      type === 'demand'      ? { 0.3: '#8c54ff', 0.6: '#3B82F6', 0.85: '#00f2ff', 1: '#fff' } :
      type === 'opportunity' ? { 0.3: '#10B981', 0.7: '#00f2ff', 1: '#fff' } :
                               { 0.3: '#ffd700', 0.7: '#ff8c00', 1: '#ff6b6b' };

    // Try leaflet.heat (if installed), otherwise fallback to circle markers
    if ((this.L as any).heatLayer) {
      this.heatLayer = (this.L as any).heatLayer(points, {
        radius: 45, blur: 35, maxZoom: 16, gradient
      }).addTo(this.mapInstance);
    } else {
      // Fallback: semi-transparent circles per property
      const fallback = this.L.layerGroup();
      points.forEach((rawPoint: any) => {
  const [lat, lng, intensity] = rawPoint as [number, number, number];
        const colorKey = Object.keys(gradient).reverse()
          .find(k => intensity >= parseFloat(k)) ?? Object.keys(gradient)[0];
        const color = (gradient as any)[colorKey];
        this.L.circle([lat, lng], {
          radius: 400,
          color: 'transparent',
          fillColor: color,
          fillOpacity: intensity * 0.5,
        }).addTo(fallback);
      });
      fallback.addTo(this.mapInstance);
      this.heatLayer = fallback;
    }
  }

  // ── Draw Mode (Polygon) ───────────────────────────────────────
  public toggleDrawMode(): void {
    this.drawModeActive.update(v => !v);
    if (!this.drawModeActive()) this.clearDrawing();
    // Change cursor
    const canvas = document.querySelector('#apex-map') as HTMLElement;
    if (canvas) canvas.style.cursor = this.drawModeActive() ? 'crosshair' : '';
  }

  private renderDrawing(): void {
    if (!this.L || !this.mapInstance) return;
    if (this.drawnPolygon) this.mapInstance.removeLayer(this.drawnPolygon);

    if (this.drawPoints.length < 2) {
      // Just a marker for first point
      this.drawnPolygon = this.L.circleMarker(this.drawPoints[0], {
        radius: 5, color: '#00f2ff', fillColor: '#00f2ff', fillOpacity: 1
      }).addTo(this.mapInstance);
      return;
    }

    this.drawnPolygon = this.L.polygon(this.drawPoints, {
      color: '#00f2ff',
      weight: 2,
      fillColor: 'rgba(0, 242, 255, 0.08)',
      dashArray: '6 4',
    }).addTo(this.mapInstance);
  }

  private finalizePolygon(): void {
    if (this.drawPoints.length < 3) return;
    this.drawModeActive.set(false);

    const canvas = document.querySelector('#apex-map') as HTMLElement;
    if (canvas) canvas.style.cursor = '';

    // Draw solid polygon
    if (this.drawnPolygon) this.mapInstance.removeLayer(this.drawnPolygon);
    this.drawnPolygon = this.L.polygon(this.drawPoints, {
      color: '#00f2ff', weight: 2,
      fillColor: 'rgba(0, 242, 255, 0.06)',
    }).addTo(this.mapInstance);

    // Store polygon points for filtering
    this.selectedPolygonPoints.set([...this.drawPoints]);

    // Update UI
    const inside = this.polygonFilteredProperties();
    this.scanResultCount.set(inside.length);
    if (inside.length > 0) this.drawerOpen.set(true);

    // Re-render pins to update markers
    this.renderPins();
  }

  public clearDrawing(): void {
    if (this.drawnPolygon && this.mapInstance) this.mapInstance.removeLayer(this.drawnPolygon);
    this.drawnPolygon = null;
    this.drawPoints   = [];
    this.drawModeActive.set(false);
    const canvas = document.querySelector('#apex-map') as HTMLElement;
    if (canvas) canvas.style.cursor = '';
  }

  // ── Address Autocomplete ──────────────────────────────────────
  public onSearchInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.searchTerm.set(val);
    this.renderPins();
    if (val.length < 2) { this.showAutocomplete.set(false); return; }

    fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(val)}&format=json&countrycodes=il&limit=5`)
      .then(r => r.json())
      .then((res: any[]) => {
        this.autocompleteResults.set(res.map(r => ({
          label: r.display_name, lat: parseFloat(r.lat), lng: parseFloat(r.lon)
        })));
        this.showAutocomplete.set(res.length > 0);
      })
      .catch(() => this.showAutocomplete.set(false));
  }

  public selectAutocomplete(item: { label: string; lat: number; lng: number }): void {
    this.searchTerm.set(item.label.split(',')[0]);
    this.showAutocomplete.set(false);
    this.mapInstance?.flyTo([item.lat, item.lng], 15, { duration: 1.2 });
  }

  public hideAutocomplete(): void {
    setTimeout(() => this.showAutocomplete.set(false), 180);
  }

  public clearSearch(): void {
    this.searchTerm.set('');
    this.showAutocomplete.set(false);
    this.renderPins();
  }

  // ── Helpers ───────────────────────────────────────────────────
  public setCategory(cat: PropertyCategory): void { this.category.set(cat); this.renderPins(); }
  public setSort(f: SortField): void { this.sortField.set(f); }

  private matchCategory(p: Property, cat: PropertyCategory): boolean {
    if (cat === 'penthouse')  return p.areaSqm > 150 && p.rooms >= 5;
    if (cat === 'investment') return !!(p.estimatedValue && p.estimatedValue > p.price * 1.08);
    if (cat === 'apartments') return p.rooms <= 5 && p.areaSqm <= 150;
    return true;
  }

  public toggleCompare(p: Property, e: Event): void {
    e.stopPropagation();
    this.compareService.isInList(p.id)
      ? this.compareService.remove(p.id)
      : this.compareService.count() < 4 && this.compareService.add(p);
  }

  public isAnomaly(id: number): boolean { return this.anomalyPropertyIds().has(id); }

  public scrollToCard(id: number): void {
    const el = document.getElementById('card-' + id);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      el.classList.add('flash');
      setTimeout(() => el.classList.remove('flash'), 900);
    }
  }

  public zoomOutAndScan(): void {
    this.mapInstance?.zoomOut(1);
    setTimeout(() => this.scanArea(), 400);
  }

  public formatPrice(n: number): string {
    if (n >= 1_000_000) return `₪${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000)     return `₪${Math.round(n / 1_000)}K`;
    return `₪${n}`;
  }

  public getScoreColor(score: number): string {
    if (score >= 80) return '#00f2ff';
    if (score >= 60) return '#ffd700';
    if (score >= 40) return '#ff8c00';
    return '#ff6b6b';
  }

  public getHaloColor(score: number | null): string {
    if (score === null) return 'rgba(0, 242, 255, 0.4)';
    if (score >= 85) return 'rgba(16, 185, 129, 0.5)';   // Green - Excellent
    if (score >= 70) return 'rgba(0, 242, 255, 0.5)';    // Cyan - Good
    if (score >= 40) return 'rgba(59, 130, 246, 0.5)';   // Blue - Neutral
    return 'rgba(255, 107, 107, 0.5)';                   // Red - Warning
  }

  private activateHaloPulse(): void {
    this.haloActive.set(true);
    setTimeout(() => this.haloActive.set(false), 1200);
  }

  // ── Market Intelligence ────────────────────────────────────────
  public generateMarketStats(): void {
    const list = this.filteredProperties();
    if (!list.length) {
      this.marketStats.set(null);
      return;
    }

    // Calculate average price trend (mock 12 months)
    const avgPrice = Math.round(list.reduce((sum, p) => sum + p.price, 0) / list.length);
    const sparklineData = Array.from({ length: 12 }, (_, i) => {
      const variance = (Math.random() - 0.4) * avgPrice * 0.12; // 12% variance — גלוי יותר
return Math.max(avgPrice + variance - (12 - i) * 2000, avgPrice * 0.80);
    });

    // Calculate heat index based on AI scores and anomalies
    const avgScore = list.reduce((sum, p) => sum + p.aiScore, 0) / list.length;
    const anomalyCount = [...this.anomalyPropertyIds()].filter(id => 
      list.some(p => p.id === id)
    ).length;
    const heatIndex = Math.min(100, Math.round(avgScore + (anomalyCount / list.length) * 30));

    // Generate mock recent transactions
    const recentTransactions: Transaction[] = list.slice(0, 5).map(p => ({
      address: p.address,
      price: p.price,
      date: new Date(p.createdAt),
      type: Math.random() > 0.3 ? 'purchase' : 'lease'
    }));

    const minPrice = Math.min(...sparklineData);
    const maxPrice = Math.max(...sparklineData);
    const priceChangePercent = minPrice > 0 ? ((maxPrice - minPrice) / minPrice * 100).toFixed(1) : '0.0';
    const areaDescription = list[0]?.address?.split(',').slice(-2).join(',') || 'Area';

    this.marketStats.set({
      sparklineData,
      heatIndex,
      recentTransactions,
      avgPrice,
      priceChange: `${parseFloat(priceChangePercent as string) > 0 ? '+' : ''}${priceChangePercent}%`,
      areaDescription
    });
  }

  public generateDefaultStats(): void {
    const all = this.properties();
    if (!all.length) {
      this.defaultStats.set(null);
      return;
    }

    // Global market stats
    const avgPrice = Math.round(all.reduce((sum, p) => sum + p.price, 0) / all.length);
    const sparklineData = Array.from({ length: 12 }, (_, i) => {
      const variance = (Math.random() - 0.4) * avgPrice * 0.12; // 12% variance — גלוי יותר
return Math.max(avgPrice + variance - (12 - i) * 2000, avgPrice * 0.80);
    });

    const avgScore = all.reduce((sum, p) => sum + p.aiScore, 0) / all.length;
    const anomalyCount = this.anomalyPropertyIds().size;
    const heatIndex = Math.min(100, Math.round(avgScore + (anomalyCount / all.length) * 30));

    // Top 5 featured properties as transactions
    const recentTransactions: Transaction[] = this.featuredProperties().map(p => ({
      address: p.address,
      price: p.price,
      date: new Date(p.createdAt),
      type: 'purchase'
    }));

    const minPrice = Math.min(...sparklineData);
    const maxPrice = Math.max(...sparklineData);
    const priceChangePercent = minPrice > 0 ? ((maxPrice - minPrice) / minPrice * 100).toFixed(1) : '0.0';

    this.defaultStats.set({
      sparklineData,
      heatIndex,
      recentTransactions,
      avgPrice,
      priceChange: `${parseFloat(priceChangePercent as string) > 0 ? '+' : ''}${priceChangePercent}%`,
      areaDescription: '🌍 שוק נדל"ן כללי'
    });
  }

  public toggleDrawerSide(): void {
    this.drawerShowingProperties.update(v => !v);
  }

  public getMinValue(arr: number[]): number {
    return Math.min(...arr);
  }

  public getMaxValue(arr: number[]): number {
    return Math.max(...arr);
  }

  public onKeydown(e: KeyboardEvent): void {
    if ((e.target as HTMLElement).tagName === 'INPUT') return;
    if (e.key === 's' || e.key === 'S') {
      this.scanArea();
      this.showingDefaultState.set(false);
    }
    if (e.key === 'h' || e.key === 'H') this.toggleHeatmap();
    if (e.key === 'Escape') { this.drawerOpen.set(false); this.showHeatmapMenu.set(false); }
  }
}