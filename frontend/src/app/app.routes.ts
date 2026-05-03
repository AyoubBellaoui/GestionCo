import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'ventes', loadComponent: () => import('./pages/ventes/ventes.component').then(m => m.VentesComponent) },
      { path: 'ventes/nouvelle', loadComponent: () => import('./pages/vente-form/vente-form.component').then(m => m.VenteFormComponent) },
      { path: 'achats', loadComponent: () => import('./pages/achats/achats.component').then(m => m.AchatsComponent) },
      { path: 'achats/nouveau', loadComponent: () => import('./pages/achat-form/achat-form.component').then(m => m.AchatFormComponent) },
      { path: 'produits', loadComponent: () => import('./pages/produits/produits.component').then(m => m.ProduitsComponent) },
      { path: 'produits/nouveau', loadComponent: () => import('./pages/produit-form/produit-form.component').then(m => m.ProduitFormComponent) },
      { path: 'produits/:id/modifier', loadComponent: () => import('./pages/produit-form/produit-form.component').then(m => m.ProduitFormComponent) },
      { path: 'categories', loadComponent: () => import('./pages/categories/categories.component').then(m => m.CategoriesComponent) },
      { path: 'clients', loadComponent: () => import('./pages/clients/clients.component').then(m => m.ClientsComponent) },
      { path: 'clients/nouveau', loadComponent: () => import('./pages/client-form/client-form.component').then(m => m.ClientFormComponent) },
      { path: 'clients/:id/modifier', loadComponent: () => import('./pages/client-form/client-form.component').then(m => m.ClientFormComponent) },
      { path: 'fournisseurs', loadComponent: () => import('./pages/fournisseurs/fournisseurs.component').then(m => m.FournisseursComponent) },
      { path: 'fournisseurs/nouveau', loadComponent: () => import('./pages/fournisseur-form/fournisseur-form.component').then(m => m.FournisseurFormComponent) },
      { path: 'fournisseurs/:id/modifier', loadComponent: () => import('./pages/fournisseur-form/fournisseur-form.component').then(m => m.FournisseurFormComponent) },
      { path: 'paiements', loadComponent: () => import('./pages/paiements/paiements.component').then(m => m.PaiementsComponent) },
      { path: 'mouvements-stock', loadComponent: () => import('./pages/mouvements-stock/mouvements-stock.component').then(m => m.MouvementsStockComponent) },
      { path: 'factures', loadComponent: () => import('./pages/factures/factures.component').then(m => m.FacturesComponent) },
      { path: 'journal-audit', loadComponent: () => import('./pages/journal-audit/journal-audit.component').then(m => m.JournalAuditComponent) },
      { path: 'parametres', loadComponent: () => import('./pages/settings/settings.component').then(m => m.SettingsComponent) },
    ]
  },
  { path: '**', redirectTo: '' }
];
