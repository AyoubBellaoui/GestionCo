import { Component, OnDestroy } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from '../shared/sidebar/sidebar.component';
import { ToastComponent } from '../shared/toast/toast.component';
import { GlobalSearchComponent } from '../shared/global-search/global-search.component';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { InactivityService } from '../core/services/inactivity.service';

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
  ) {
    if (!auth.user && !localStorage.getItem('gc_token')) {
      this.router.navigate(['/login']);
    } else {
      this.inactivity.start();
    }
  }

  ngOnDestroy(): void {
    this.inactivity.stop();
  }
}
