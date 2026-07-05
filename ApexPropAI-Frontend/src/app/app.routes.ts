import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home';
import { PropertyDetailsComponent } from './features/property-details/property-details';
import { AuthComponent } from './features/auth/auth';
import { authGuard } from './core/guards/auth';
import { CompareComponent } from './features/compare/compare';
import { SavedComponent } from './features/saved/saved';

export const routes: Routes = [
  // ============= AUTH =============
  {
    path: '',
    component: AuthComponent,
    data: { title: 'התחברות - PropertyInsight' }
  },

  // ============= HOME / SEARCH =============
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [authGuard],
    data: { title: 'PropertyInsight - חיפוש נכסים' }
  },

  // ============= PROPERTY DETAILS =============
  {
    path: 'property/:id',
    component: PropertyDetailsComponent,
    canActivate: [authGuard],
    data: { title: 'פרטי נכס - PropertyInsight' }
  },

  // ============= COMPARE =============
  {
    path: 'compare',
    component: CompareComponent,
    canActivate: [authGuard],
    data: { title: 'השוואת נכסים - PropertyInsight' }
  },

  // ============= SAVED PROPERTIES =============
  {
    path: 'saved',
    component: SavedComponent,
    canActivate: [authGuard],
    data: { title: 'נכסים שמורים - PropertyInsight' }
  },

  // ============= WILDCARD =============
  {
    path: '**',
    redirectTo: ''
  }
];