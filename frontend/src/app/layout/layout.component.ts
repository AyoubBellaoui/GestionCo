import { Component, OnDestroy } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from '../shared/sidebar/sidebar.component';
import { ToastComponent } from '../shared/toast/toast.component';
import { GlobalSearchComponent } from '../shared/global-search/global-search.component';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { InactivityService } from '../core/services/inactivity.service';
import { ApiService } from '../core/services/api.service';
import { SettingsService } from '../core/services/settings.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, ToastComponent, GlobalSearchComponent],
  templateUrl: './layout.component.html',
})
export class LayoutComponent implements OnDestroy {
  constructor(
    public auth: AuthService,
    private router: Router,
    private inactivity: InactivityService,
    _theme: ThemeService,
    api: ApiService,
    settings: SettingsService,
  ) {
    if (!auth.user && !localStorage.getItem('gc_token')) {
      this.router.navigate(['/login']);
    } else {
      this.inactivity.start();
      // Nom et logo de la barre latérale : ceux enregistrés par l'administrateur, pour tous les rôles
      settings.refreshEntrepriseFromServer(() => api.getEntrepriseSettings()).catch(() => { /* copie locale */ });
    }
  }

  ngOnDestroy(): void {
    this.inactivity.stop();
  }
}
