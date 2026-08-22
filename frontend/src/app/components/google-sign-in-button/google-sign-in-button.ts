import { Component, ElementRef, EventEmitter, Output, ViewChild, AfterViewInit } from '@angular/core';
import { environment } from '../../../environments/environment';

interface GoogleCredentialResponse {
  credential: string;
}

declare const google: {
  accounts: {
    id: {
      initialize(config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }): void;
      renderButton(parent: HTMLElement, options: { theme?: string; size?: string; width?: number; text?: string }): void;
    };
  };
};

@Component({
  selector: 'app-google-sign-in-button',
  template: '<div #buttonContainer></div>',
})
export class GoogleSignInButton implements AfterViewInit {
  @ViewChild('buttonContainer', { static: true }) buttonContainer!: ElementRef<HTMLDivElement>;
  @Output() credential = new EventEmitter<string>();

  ngAfterViewInit(): void {
    if (!environment.googleClientId) {
      console.warn('Google sign-in is not configured (missing googleClientId).');
      return;
    }

    if (typeof google === 'undefined') {
      console.warn('Google Identity Services script has not loaded yet.');
      return;
    }

    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response) => this.credential.emit(response.credential)
    });

    google.accounts.id.renderButton(this.buttonContainer.nativeElement, {
      theme: 'outline',
      size: 'large',
      width: 300,
      text: 'continue_with'
    });
  }
}
