import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

/**
 * App initializer: fetches the XSRF token once. The API sets the cookie that Angular's HttpClient
 * echoes in X-XSRF-TOKEN. Errors are swallowed so the app still renders when the API is down.
 */
export async function fetchXsrfToken(): Promise<void> {
  const http = inject(HttpClient);
  try {
    await firstValueFrom(http.get('/api/xsrf-token'), { defaultValue: null });
  } catch {
    // the health page reports the API being down
  }
}
