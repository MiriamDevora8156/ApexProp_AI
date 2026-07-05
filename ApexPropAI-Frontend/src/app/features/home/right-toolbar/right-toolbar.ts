import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

type HeatmapLayer = 'demand' | 'opportunity' | 'growth';
type MapStyle     = 'dark' | 'satellite' | 'street';

@Component({
  selector: 'app-right-toolbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './right-toolbar.html',
  styleUrls: ['../home.scss']
})
export class RightToolbarComponent {
  @Input() scanning: boolean = false;
  @Input() scanResultCount: number | null = null;
  @Input() heatmapActive: boolean = false;
  @Input() showHeatmapMenu: boolean = false;
  @Input() heatmapLayer: HeatmapLayer = 'demand';
  @Input() drawModeActive: boolean = false;
  @Input() drawnPolygon: boolean = false;
  @Input() mapTileStyle: MapStyle = 'dark';
  @Input() drawerOpen: boolean = false;

  @Output() scanClicked          = new EventEmitter<void>();
  @Output() heatmapMenuToggled   = new EventEmitter<void>();
  @Output() heatmapLayerChanged  = new EventEmitter<HeatmapLayer>();
  @Output() heatmapToggled       = new EventEmitter<void>();
  @Output() drawModeToggled      = new EventEmitter<void>();
  @Output() drawingCleared       = new EventEmitter<void>();
  @Output() mapStyleChanged      = new EventEmitter<MapStyle>();
  @Output() drawerToggled        = new EventEmitter<void>();

  readonly heatmapOptions = [
    { key: 'demand'      as HeatmapLayer, label: 'ביקוש',      icon: 'ph-bold ph-drop' },
    { key: 'opportunity' as HeatmapLayer, label: 'הזדמנויות',  icon: 'ph-bold ph-diamond' },
    { key: 'growth'      as HeatmapLayer, label: 'עליית מחיר', icon: 'ph-bold ph-trend-up' },
  ];

  readonly mapStyles = [
    { key: 'dark'      as MapStyle, label: 'לילה',   icon: 'ph-bold ph-moon' },
    { key: 'satellite' as MapStyle, label: 'לווין',  icon: 'ph-bold ph-globe' },
    { key: 'street'    as MapStyle, label: 'רחובות', icon: 'ph-bold ph-map-trifold' },
  ];
}