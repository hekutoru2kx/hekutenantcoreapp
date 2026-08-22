import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'app-landing',
  imports: [MatButtonModule, MatIconModule, MatCardModule, TranslocoModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing {}