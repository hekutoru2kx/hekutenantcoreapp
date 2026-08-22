import { TestBed } from '@angular/core/testing';

import { Geography } from './geography';

describe('Geography', () => {
  let service: Geography;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Geography);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
