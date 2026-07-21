import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { FollowService } from '../../core/services/follow.service';
import { BadgeService } from '../../core/services/badge.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskService } from '../../core/services/task.service';
import { User } from '../../core/models/user.model';
import { UserSummary } from '../../core/models/follow.model';
import { Badge } from '../../core/models/badge.model';
import { Objective } from '../../core/models/objective.model';
import { TaskItem } from '../../core/models/task.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-friend-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './friend-profile.component.html',
  styleUrl: './friend-profile.component.css'
})
export class FriendProfileComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  user = signal<User | null>(null);
  followers = signal<UserSummary[]>([]);
  following = signal<UserSummary[]>([]);
  streak = signal(0);
  badges = signal<Badge[]>([]);
  objectives = signal<Objective[]>([]);

  isFollowing = signal(false);
  followBusy = signal(false);

  listModal = signal<'following' | 'followers' | null>(null);

  private apiOrigin = environment.apiUrl.replace('/api', '');
  private myId: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private auth: AuthService,
    private userService: UserService,
    private followService: FollowService,
    private badgeService: BadgeService,
    private objectiveService: ObjectiveService,
    private taskService: TaskService
  ) {
    this.myId = this.auth.getUserId();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (id === this.myId) {
        this.router.navigate(['/profile'], { replaceUrl: true });
        return;
      }
      this.loadAll(id);
    });
  }

  private loadAll(userId: number): void {
    this.loading.set(true);
    this.loadError.set(false);
    forkJoin({
      user: this.userService.getById(userId),
      followers: this.followService.getFollowers(userId),
      following: this.followService.getFollowing(userId),
      myFollowing: this.followService.getFollowing(this.myId),
      tasks: this.taskService.getAll(userId),
      badges: this.badgeService.getByUser(userId),
      objectives: this.objectiveService.getAll(userId)
    }).subscribe({
      next: ({ user, followers, following, myFollowing, tasks, badges, objectives }) => {
        this.user.set(user);
        this.followers.set(followers);
        this.following.set(following);
        this.isFollowing.set(myFollowing.some(f => f.id === userId));
        this.streak.set(this.computeStreak(tasks));
        this.badges.set(badges);
        this.objectives.set(objectives);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  private computeStreak(tasks: TaskItem[]): number {
    const completedDays = new Set(
      tasks
        .filter(t => t.status === 'Completed' && !!t.scheduledDate)
        .map(t => t.scheduledDate.substring(0, 10))
    );

    let cursor = this.dateKey(new Date());
    if (!completedDays.has(cursor)) {
      cursor = this.dateKey(this.addDays(new Date(), -1));
    }
    let streak = 0;
    while (completedDays.has(cursor)) {
      streak++;
      cursor = this.dateKey(this.addDays(this.parseDateKey(cursor), -1));
    }
    return streak;
  }

  private addDays(date: Date, days: number): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() + days);
    return d;
  }

  private dateKey(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private parseDateKey(key: string): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  photoUrl(): string | null {
    const url = this.user()?.profilePhotoUrl;
    return url ? this.apiOrigin + url : null;
  }

  personPhotoUrl(person: UserSummary): string | null {
    return person.profilePhotoUrl ? this.apiOrigin + person.profilePhotoUrl : null;
  }

  toggleFollow(): void {
    const target = this.user();
    if (!target || this.followBusy()) return;
    this.followBusy.set(true);

    const onDone = () => {
      this.isFollowing.set(!this.isFollowing());
      this.followBusy.set(false);
      this.followService.getFollowers(target.id).subscribe(followers => this.followers.set(followers));
    };
    const onError = () => this.followBusy.set(false);

    if (this.isFollowing()) {
      this.followService.unfollow(this.myId, target.id).subscribe({ next: onDone, error: onError });
    } else {
      this.followService.follow(this.myId, target.id).subscribe({ next: onDone, error: onError });
    }
  }

  openList(type: 'following' | 'followers'): void {
    this.listModal.set(type);
  }

  closeList(): void {
    this.listModal.set(null);
  }

  goBack(): void {
    this.location.back();
  }

  goToPerson(person: UserSummary): void {
    this.listModal.set(null);
    this.router.navigate(['/profile', person.id]);
  }

  chatWith(): void {
    const target = this.user();
    if (!target) return;
    this.router.navigate(['/chat'], { queryParams: { with: target.id } });
  }

  statusLabel(status: Objective['status']): string {
    switch (status) {
      case 'Pending': return 'Pendiente';
      case 'InProgress': return 'En progreso';
      case 'Completed': return 'Completado';
      case 'Cancelled': return 'Cancelado';
      default: return status;
    }
  }
}
