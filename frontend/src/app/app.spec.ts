import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { CurrentUser } from './services/auth';
import { provideTestApp } from './testing/test-providers';

const signedInUser = (overrides: Partial<CurrentUser> = {}): CurrentUser => ({
  email: 'jane@example.com',
  userName: 'jane',
  mustChangePassword: false,
  preferredTheme: 'light',
  // A tenant is selected, so the "no tenant → tenant picker" redirect backstop stays out of the way.
  tenantId: 1,
  tenantName: 'Clinic',
  multiTenantDisabled: false,
  ...overrides,
});

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: provideTestApp(),
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('renders the nav bar and the routed page outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('app-nav-bar')).not.toBeNull();
    expect(compiled.querySelector('router-outlet')).not.toBeNull();
  });

  it('shows no password warning for a user who does not have to change theirs', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.auth.currentUser.set(signedInUser({ mustChangePassword: false }));
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('.password-warning')).toBeNull();
  });

  it('shows the password warning, with a link to change it, when the user must change their password', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.auth.currentUser.set(signedInUser({ mustChangePassword: true }));
    await fixture.whenStable();

    const warning = (fixture.nativeElement as HTMLElement).querySelector('.password-warning');
    expect(warning).not.toBeNull();
    expect(warning!.querySelector('a')?.getAttribute('href')).toBe('/change-password');
  });
});
