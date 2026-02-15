import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'logs', loadComponent: () => import('./features/log-explorer/log-explorer.component').then(m => m.LogExplorerComponent) },
  { path: 'analytics', loadComponent: () => import('./features/analytics/analytics.component').then(m => m.AnalyticsComponent) },
  { path: 'settings', loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent) },
  { path: 'changelog', loadComponent: () => import('./features/changelog/changelog.component').then(m => m.ChangelogComponent) }
];
