import { ErrorHandler } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  const handleError = vi.fn();
  let http: HttpTestingController;

  beforeEach(() => {
    handleError.mockClear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ErrorHandler, useValue: { handleError } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows checking until the response arrives', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Checking API');
    http.expectOne('/health').flush({ status: 'Healthy' });
  });

  it('shows healthy when /health answers 200', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http.expectOne('/health').flush({ status: 'Healthy' }, { status: 200, statusText: 'OK' });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('API is healthy');
  });

  it('shows unavailable on a network error', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http.expectOne('/health').error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('API is unavailable');
    expect(handleError).not.toHaveBeenCalled();
  });

  it.each([
    [500, 'Internal Server Error'],
    [503, 'Service Unavailable'],
    [404, 'Not Found'],
    [204, 'No Content'],
  ])('shows unavailable when /health answers %i', (status, statusText) => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http.expectOne('/health').flush(status === 204 ? null : 'x', { status, statusText });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('API is unavailable');
    expect(handleError).not.toHaveBeenCalled();
  });
});
