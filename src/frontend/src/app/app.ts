import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { ApiHealth, HealthState } from './api-health';

type PageState = HealthState | 'checking';

@Component({
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly state$: Observable<PageState> = inject(ApiHealth).check();
  protected readonly health = toSignal(this.state$, { initialValue: 'checking' });
}
