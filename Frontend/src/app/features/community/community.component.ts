import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-community',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './community.component.html',
  styleUrl: './community.component.css'
})
export class CommunityComponent {}
