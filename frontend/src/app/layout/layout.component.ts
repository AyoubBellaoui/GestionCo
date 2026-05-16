import { Component } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from '../shared/sidebar/sidebar.component';
import { ToastComponent } from '../shared/toast/toast.component';
import { GlobalSearchComponent } from '../shared/global-search/global-search.component';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, ToastComponent, GlobalSearchComponent],
  templateUrl: './layout.component.html',
})
export class LayoutComponent {
  constructor(public auth: AuthService, private router: Router) {
    if (!auth.user && !localStorage.getItem('gc_token')) {
      this.router.navigate(['/login']);
    }
  }
}
