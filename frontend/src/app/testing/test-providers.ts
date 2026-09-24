import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoLoader } from '@jsverse/transloco';
import { of } from 'rxjs';

// Serves an empty dictionary for every language — a component under test just needs Transloco to
// *exist* (its pipe/service resolve to the raw key), not real translations.
class EmptyTranslocoLoader implements TranslocoLoader {
  getTranslation() {
    return of({});
  }
}

// The app-wide providers a component needs to be instantiated in a spec, mirroring app.config.ts
// with test doubles where the real thing would touch the network: Transloco (stub loader), the
// Router/ActivatedRoute (no routes), and HttpClient wired to the testing backend so an on-init
// request is captured instead of leaving the process. Spread into `providers` of
// TestBed.configureTestingModule — specs that need more (a mocked service, a route param) add their
// own alongside it.
export function provideTestApp() {
  return [
    provideHttpClient(),
    provideHttpClientTesting(),
    provideRouter([]),
    provideTransloco({
      config: { availableLangs: ['en', 'es'], defaultLang: 'en' },
      loader: EmptyTranslocoLoader,
    }),
  ];
}
