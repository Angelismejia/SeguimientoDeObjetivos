import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { FollowService } from '../../core/services/follow.service';
import { FriendStreakService } from '../../core/services/friend-streak.service';
import { TaskService } from '../../core/services/task.service';
import { User } from '../../core/models/user.model';
import { UserSummary } from '../../core/models/follow.model';
import { FriendStreak, FriendStreakInvitation } from '../../core/models/friend-streak.model';
import { TaskItem } from '../../core/models/task.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);
  uploading = signal(false);
  uploadError = signal('');

  user = signal<User | null>(null);
  followers = signal<UserSummary[]>([]);
  following = signal<UserSummary[]>([]);
  streak = signal(0);

  friendStreaks = signal<FriendStreak[]>([]);
  receivedInvitations = signal<FriendStreakInvitation[]>([]);
  sentInvitations = signal<FriendStreakInvitation[]>([]);

  private apiOrigin = environment.apiUrl.replace('/api', '');

  // ── Agregar amigo por username ────────────────────────
  showAddFriend = signal(false);
  addFriendForm: FormGroup;
  searching = signal(false);
  searchError = signal('');
  foundUser = signal<User | null>(null);

  unfollowTarget = signal<UserSummary | null>(null);

  // ── Acción sobre un amigo (invitar a racha / dejar de seguir) ──
  friendAction = signal<UserSummary | null>(null);
  friendActionError = signal('');

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private userService: UserService,
    private followService: FollowService,
    private friendStreakService: FriendStreakService,
    private taskService: TaskService
  ) {
    this.addFriendForm = this.fb.group({
      username: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const userId = this.auth.getUserId();
    forkJoin({
      user: this.userService.getById(userId),
      followers: this.followService.getFollowers(userId),
      following: this.followService.getFollowing(userId),
      tasks: this.taskService.getAll(userId),
      friendStreaks: this.friendStreakService.getForUser(userId),
      receivedInvitations: this.friendStreakService.getReceivedInvitations(userId),
      sentInvitations: this.friendStreakService.getSentInvitations(userId)
    }).subscribe({
      next: ({ user, followers, following, tasks, friendStreaks, receivedInvitations, sentInvitations }) => {
        this.user.set(user);
        this.followers.set(followers);
        this.following.set(following);
        this.streak.set(this.computeStreak(tasks));
        this.friendStreaks.set(friendStreaks);
        this.receivedInvitations.set(receivedInvitations);
        this.sentInvitations.set(sentInvitations);
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

  friendPhotoUrl(friend: UserSummary): string | null {
    return friend.profilePhotoUrl ? this.apiOrigin + friend.profilePhotoUrl : null;
  }

  invitationPhotoUrl(inv: FriendStreakInvitation): string | null {
    return inv.fromProfilePhotoUrl ? this.apiOrigin + inv.fromProfilePhotoUrl : null;
  }

  streakWith(friendId: number): number | null {
    const fs = this.friendStreaks().find(f => f.partnerId === friendId);
    return fs ? fs.currentStreak : null;
  }

  hasPendingInvite(friendId: number): boolean {
    return this.sentInvitations().some(i => i.toUserId === friendId);
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.uploadError.set('');
    this.userService.uploadPhoto(this.auth.getUserId(), file).subscribe({
      next: updated => {
        this.user.set(updated);
        this.uploading.set(false);
        input.value = '';
      },
      error: () => {
        this.uploading.set(false);
        this.uploadError.set('No se pudo subir la foto. Probá con una imagen JPG, PNG o WEBP.');
        input.value = '';
      }
    });
  }

  // ── Agregar amigo ──────────────────────────────────────
  openAddFriend(): void {
    this.addFriendForm.reset({ username: '' });
    this.searchError.set('');
    this.foundUser.set(null);
    this.showAddFriend.set(true);
  }

  closeAddFriend(): void {
    this.showAddFriend.set(false);
  }

  search(): void {
    if (this.addFriendForm.invalid) return;
    const username = this.addFriendForm.value.username.trim();
    this.searching.set(true);
    this.searchError.set('');
    this.foundUser.set(null);

    this.userService.getByUsername(username).subscribe({
      next: found => {
        this.searching.set(false);
        if (found.id === this.auth.getUserId()) {
          this.searchError.set('Ese sos vos :)');
          return;
        }
        if (this.following().some(f => f.id === found.id)) {
          this.searchError.set('Ya seguís a este usuario.');
          return;
        }
        this.foundUser.set(found);
      },
      error: () => {
        this.searching.set(false);
        this.searchError.set('No se encontró ningún usuario con ese username.');
      }
    });
  }

  follow(): void {
    const found = this.foundUser();
    if (!found) return;
    this.followService.follow(this.auth.getUserId(), found.id).subscribe({
      next: () => {
        this.following.set([...this.following(), {
          id: found.id,
          username: found.username,
          name: found.name,
          profilePhotoUrl: found.profilePhotoUrl
        }]);
        this.showAddFriend.set(false);
      },
      error: () => {
        this.searchError.set('No se pudo seguir a este usuario. Intenta de nuevo.');
      }
    });
  }

  // ── Acción sobre un amigo ──────────────────────────────
  openFriendAction(friend: UserSummary): void {
    this.friendActionError.set('');
    this.friendAction.set(friend);
  }

  closeFriendAction(): void {
    this.friendAction.set(null);
  }

  inviteToStreak(): void {
    const friend = this.friendAction();
    if (!friend) return;
    this.friendActionError.set('');
    this.friendStreakService.invite(this.auth.getUserId(), friend.id).subscribe({
      next: created => {
        this.sentInvitations.set([...this.sentInvitations(), created]);
        this.friendAction.set(null);
      },
      error: () => {
        this.friendActionError.set('No se pudo enviar la invitación. Intenta de nuevo.');
      }
    });
  }

  askUnfollowFromAction(): void {
    const friend = this.friendAction();
    if (!friend) return;
    this.friendAction.set(null);
    this.unfollowTarget.set(friend);
  }

  cancelUnfollow(): void {
    this.unfollowTarget.set(null);
  }

  confirmUnfollow(): void {
    const target = this.unfollowTarget();
    if (!target) return;
    this.followService.unfollow(this.auth.getUserId(), target.id).subscribe({
      next: () => {
        this.following.set(this.following().filter(f => f.id !== target.id));
        this.unfollowTarget.set(null);
      },
      error: () => {
        this.unfollowTarget.set(null);
      }
    });
  }

  // ── Invitaciones de racha recibidas ────────────────────
  acceptInvite(inv: FriendStreakInvitation): void {
    this.friendStreakService.accept(inv.id, this.auth.getUserId()).subscribe({
      next: created => {
        this.receivedInvitations.set(this.receivedInvitations().filter(i => i.id !== inv.id));
        this.friendStreaks.set([...this.friendStreaks(), created]);
        if (!this.following().some(f => f.id === inv.fromUserId)) {
          this.followers.set([...this.followers(), {
            id: inv.fromUserId,
            username: inv.fromUsername,
            name: inv.fromName,
            profilePhotoUrl: inv.fromProfilePhotoUrl
          }]);
        }
      }
    });
  }

  rejectInvite(inv: FriendStreakInvitation): void {
    this.friendStreakService.reject(inv.id, this.auth.getUserId()).subscribe({
      next: () => {
        this.receivedInvitations.set(this.receivedInvitations().filter(i => i.id !== inv.id));
      }
    });
  }
}
