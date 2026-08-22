import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { claimGuard } from './claim-guard';

describe('claimGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => claimGuard('UserManagementPermission', 'Read')(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
