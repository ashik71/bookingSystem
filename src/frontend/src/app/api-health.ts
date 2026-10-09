import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

export type HealthState = 'healthy' | 'unavailable';

@Injectable({ providedIn: 'root' })
export class ApiHealth {
  private readonly http = inject(HttpClient);

  check(): Observable<HealthState> {
    return this.http.get('/health', { observe: 'response' }).pipe(
      map((response): HealthState => (response.status === 200 ? 'healthy' : 'unavailable')),
      catchError(() => of<HealthState>('unavailable')),
    );
  }
}
