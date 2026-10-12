import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { fetchXsrfToken } from './xsrf';

describe('fetchXsrfToken', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
  });

  it('GETs /api/xsrf-token once', async () => {
    const done = TestBed.runInInjectionContext(fetchXsrfToken);
    http
      .expectOne({ method: 'GET', url: '/api/xsrf-token' })
      .flush(null, { status: 204, statusText: 'No Content' });
    await done;
  });

  it('resolves when the request fails', async () => {
    const done = TestBed.runInInjectionContext(fetchXsrfToken);
    http.expectOne('/api/xsrf-token').flush(null, { status: 500, statusText: 'Server Error' });
    await expect(done).resolves.toBeUndefined();
  });

  it('sends X-XSRF-TOKEN from the cookie on a POST', () => {
    document.cookie = 'XSRF-TOKEN=abc123; path=/';
    TestBed.inject(HttpClient).post('/api/anything', {}).subscribe();
    const request = http.expectOne('/api/anything');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('abc123');
    request.flush(null);
  });
});
