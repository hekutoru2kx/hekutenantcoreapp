import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PersonManagement } from './person-management';
import { provideTestApp } from '../../../testing/test-providers';

describe('PersonManagement', () => {
  let component: PersonManagement;
  let fixture: ComponentFixture<PersonManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonManagement],
      providers: provideTestApp(),
    }).compileComponents();

    fixture = TestBed.createComponent(PersonManagement);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
