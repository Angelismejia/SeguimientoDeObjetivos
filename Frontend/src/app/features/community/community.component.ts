import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { FollowService } from '../../core/services/follow.service';
import { User } from '../../core/models/user.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { environment } from '../../../environments/environment';
import { mensajeDeError } from '../../core/utils/http-error.util';

@Component({
  selector: 'app-community',
  standalone: true,
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './community.component.html',
  styleUrl: './community.component.css'
})
export class CommunityComponent implements OnInit {
  loading = signal(true);
  loadError = signal('');
  actionError = signal('');

  people = signal<User[]>([]);
  followingIds = signal<Set<number>>(new Set());
  query = signal('');
  // Mientras una peticion de seguir/dejar de seguir esta en vuelo, el boton se
  // deshabilita: un doble click mandaba el POST dos veces y el segundo fallaba.
  pendingIds = signal<Set<number>>(new Set());
  unfollowTarget = signal<User | null>(null);

  private apiOrigin = environment.apiUrl.replace('/api', '');

  filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.people();
    return this.people().filter(u =>
      u.username.toLowerCase().includes(q) || u.name.toLowerCase().includes(q));
  });

  constructor(
    private auth: AuthService,
    private userService: UserService,
    private followService: FollowService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const myId = this.auth.getUserId();
    this.loading.set(true);
    this.loadError.set('');

    forkJoin({
      users: this.userService.getAll(),
      following: this.followService.getFollowing(myId)
    }).subscribe({
      next: ({ users, following }) => {
        // GetAll no filtra por IsActive: las cuentas desactivadas se ocultan aca
        // para no ofrecer seguir a alguien que ya no usa la app.
        this.people.set(users.filter(u => u.id !== myId && u.isActive));
        this.followingIds.set(new Set(following.map(f => f.id)));
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudo cargar la comunidad.'));
      }
    });
  }

  onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  isFollowing(userId: number): boolean {
    return this.followingIds().has(userId);
  }

  isPending(userId: number): boolean {
    return this.pendingIds().has(userId);
  }

  photoUrl(u: User): string | null {
    return u.profilePhotoUrl ? this.apiOrigin + u.profilePhotoUrl : null;
  }

  follow(u: User): void {
    this.actionError.set('');
    this.setPending(u.id, true);
    this.followService.follow(this.auth.getUserId(), u.id).subscribe({
      next: () => {
        this.followingIds.set(new Set(this.followingIds()).add(u.id));
        this.setPending(u.id, false);
      },
      error: (e) => {
        this.setPending(u.id, false);
        this.actionError.set(mensajeDeError(e, 'No se pudo seguir a este usuario. Intenta de nuevo.'));
      }
    });
  }

  askUnfollow(u: User): void {
    this.unfollowTarget.set(u);
  }

  cancelUnfollow(): void {
    this.unfollowTarget.set(null);
  }

  confirmUnfollow(): void {
    const target = this.unfollowTarget();
    if (!target) return;
    this.unfollowTarget.set(null);
    this.actionError.set('');
    this.setPending(target.id, true);
    this.followService.unfollow(this.auth.getUserId(), target.id).subscribe({
      next: () => {
        const ids = new Set(this.followingIds());
        ids.delete(target.id);
        this.followingIds.set(ids);
        this.setPending(target.id, false);
      },
      error: (e) => {
        this.setPending(target.id, false);
        this.actionError.set(mensajeDeError(e, 'No se pudo dejar de seguir. Intenta de nuevo.'));
      }
    });
  }

  private setPending(userId: number, pending: boolean): void {
    const ids = new Set(this.pendingIds());
    if (pending) ids.add(userId); else ids.delete(userId);
    this.pendingIds.set(ids);
  }
}
