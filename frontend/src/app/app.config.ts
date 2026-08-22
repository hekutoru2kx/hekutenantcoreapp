import { ApplicationConfig, provideBrowserGlobalErrorListeners, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { TranslocoHttpLoader } from './transloco-loader';
import { provideTransloco } from '@jsverse/transloco';
import { authInterceptor } from './interceptors/auth-interceptor';
import { AVAILABLE_LANGUAGES } from './constants/languages';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes),
    provideTransloco({
      config: {
        availableLangs: AVAILABLE_LANGUAGES,
        defaultLang: 'es',
        // Remove this option if your application doesn't support changing language in runtime.
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
        // Scoped keys must be referenced explicitly with their scope prefix — autoPrefixKeys
        // would otherwise also prefix shared/global keys (persons.*, admin.users.*, ...)
        // referenced from within a scoped component, breaking them.
        scopes: { autoPrefixKeys: false },
      },
      loader: TranslocoHttpLoader,
    }),
  ],
};
