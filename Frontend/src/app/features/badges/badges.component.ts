import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { BadgeService } from '../../core/services/badge.service';
import { Badge } from '../../core/models/badge.model';

@Component({
  selector: 'app-badges',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './badges.component.html',
  styleUrl: './badges.component.css'
})
export class BadgesComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  allBadges = signal<Badge[]>([]);
  earnedIds = signal<Set<number>>(new Set());

  constructor(
    private auth: AuthService,
    private badgeService: BadgeService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    forkJoin({
      all: this.badgeService.getAll(),
      earned: this.badgeService.getByUser(this.auth.getUserId())
    }).subscribe({
      next: ({ all, earned }) => {
        this.allBadges.set(all);
        this.earnedIds.set(new Set(earned.map(b => b.id)));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  isEarned(badge: Badge): boolean {
    return this.earnedIds().has(badge.id);
  }
}
