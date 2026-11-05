import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, forkJoin, of, switchMap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { FollowService } from '../../core/services/follow.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { User } from '../../core/models/user.model';
import {
  CommunityFeedItem, Objective, ObjectiveComment, ObjectiveSocial, ObjectiveStatus
} from '../../core/models/objective.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { environment } from '../../../environments/environment';
import { mensajeDeError } from '../../core/utils/http-error.util';
import { fechaDelBackend } from '../../core/utils/fecha.util';

/** Cuantas personas se muestran: sin buscar, una muestra; buscando, los primeros resultados. */
const PERSONAS_POR_PAGINA = 30;

const STATUS_LABEL: Record<ObjectiveStatus, string> = {
  Pending: 'Pendiente',
  InProgress: 'En progreso',
  Completed: 'Completado',
  Cancelled: 'Cancelado'
};

const VACIO: ObjectiveSocial = { objectiveId: 0, likeCount: 0, likedByMe: false, commentCount: 0 };

@Component({
  selector: 'app-community',
  standalone: true,
  imports: [RouterLink, DatePipe, ConfirmDialogComponent],
  templateUrl: './community.component.html',
  styleUrl: './community.component.css'
})
export class CommunityComponent implements OnInit {
  loading = signal(true);
  loadError = signal('');
  actionError = signal('');

  people = signal<User[]>([]);
  followingIds = signal<Set<number>>(new Set());
  // Quienes me siguen: se marcan con "Te sigue" en la lista de personas.
  followerIds = signal<Set<number>>(new Set());
  query = signal('');
  // Mientras una peticion de seguir/dejar de seguir esta en vuelo, el boton se
  // deshabilita: un doble click mandaba el POST dos veces y el segundo fallaba.
  pendingIds = signal<Set<number>>(new Set());
  unfollowTarget = signal<User | null>(null);

  // El feed llega ya ordenado del backend: logrados primero, despues lo mas
  // reciente. Antes se pedian los objetivos de cada persona seguida por separado.
  activity = signal<CommunityFeedItem[]>([]);
  activityLoading = signal(false);
  searching = signal(false);
  private searchTerms = new Subject<string>();
  private destroyRef = inject(DestroyRef);

  // Likes y comentarios por id de objetivo (solo los logrados admiten reacciones).
  socialById = signal<Map<number, ObjectiveSocial>>(new Map());
  likePendingIds = signal<Set<number>>(new Set());
  openCommentsId = signal<number | null>(null);
  comments = signal<ObjectiveComment[]>([]);
  commentsLoading = signal(false);
  commentDraft = signal('');
  sendingComment = signal(false);
  commentError = signal('');
  readonly myId: number;

  private apiOrigin = environment.apiUrl.replace('/api', '');

  // La busqueda la hace el servidor; aca solo se saca a uno mismo.
  filtered = computed(() => this.people().filter(u => u.id !== this.myId));

  constructor(
    private auth: AuthService,
    private userService: UserService,
    private followService: FollowService,
    private objectiveService: ObjectiveService
  ) {
    this.myId = this.auth.getUserId();
  }

  ngOnInit(): void {
    // Busca en el servidor 300 ms despues de la ultima tecla; switchMap descarta
    // la respuesta de una busqueda vieja si llega despues de una nueva.
    this.searchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => {
        this.searching.set(true);
        return this.userService.search(q, PERSONAS_POR_PAGINA).pipe(catchError(() => of(null)));
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(users => {
      this.searching.set(false);
      if (users) this.people.set(users);
      else this.actionError.set('No se pudo buscar. Intenta de nuevo.');
    });
    this.load();
  }

  load(): void {
    const myId = this.auth.getUserId();
    this.loading.set(true);
    this.loadError.set('');

    forkJoin({
      users: this.userService.search(this.query().trim(), PERSONAS_POR_PAGINA),
      following: this.followService.getFollowing(myId),
      // Si falla, la pagina sigue funcionando: solo no se marca "Te sigue".
      followers: this.followService.getFollowers(myId).pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ users, following, followers }) => {
        // El servidor ya deja afuera las cuentas desactivadas.
        this.people.set(users);
        this.followingIds.set(new Set(following.map(f => f.id)));
        this.followerIds.set(new Set(followers.map(f => f.id)));
        this.loading.set(false);
        this.loadActivity();
      },
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudo cargar la comunidad.'));
      }
    });
  }

  private loadActivity(): void {
    if (this.followingIds().size === 0) {
      this.activity.set([]);
      this.socialById.set(new Map());
      return;
    }
    this.activityLoading.set(true);
    this.objectiveService.getFeed().subscribe({
      next: items => {
        this.activity.set(items);
        this.socialById.set(new Map(items.map(i => [i.objective.id, i.social] as [number, ObjectiveSocial])));
        this.activityLoading.set(false);
      },
      error: () => {
        // Sin feed la pagina sigue sirviendo para buscar y seguir personas.
        this.activity.set([]);
        this.activityLoading.set(false);
      }
    });
  }

  // ── Likes y comentarios ──────────────────────────────

  canReact(objective: Objective): boolean {
    return objective.status === 'Completed';
  }

  socialOf(objective: Objective): ObjectiveSocial {
    return this.socialById().get(objective.id) ?? { ...VACIO, objectiveId: objective.id };
  }

  isLikePending(objective: Objective): boolean {
    return this.likePendingIds().has(objective.id);
  }

  toggleLike(objective: Objective): void {
    if (this.isLikePending(objective)) return;
    const liked = !this.socialOf(objective).likedByMe;
    this.setLikePending(objective.id, true);
    this.actionError.set('');
    this.objectiveService.setLike(objective.id, liked).subscribe({
      next: social => {
        this.socialById.set(new Map(this.socialById()).set(objective.id, social));
        this.setLikePending(objective.id, false);
      },
      error: (e) => {
        this.setLikePending(objective.id, false);
        this.actionError.set(mensajeDeError(e, 'No se pudo guardar el me gusta. Intenta de nuevo.'));
      }
    });
  }

  toggleComments(objective: Objective): void {
    if (this.openCommentsId() === objective.id) {
      this.openCommentsId.set(null);
      return;
    }
    this.openCommentsId.set(objective.id);
    this.comments.set([]);
    this.commentDraft.set('');
    this.commentError.set('');
    this.commentsLoading.set(true);
    this.objectiveService.getComments(objective.id).subscribe({
      next: list => {
        this.comments.set(list);
        this.commentsLoading.set(false);
      },
      error: (e) => {
        this.commentsLoading.set(false);
        this.commentError.set(mensajeDeError(e, 'No se pudieron cargar los comentarios.'));
      }
    });
  }

  onCommentInput(event: Event): void {
    this.commentDraft.set((event.target as HTMLTextAreaElement).value);
  }

  sendComment(objective: Objective): void {
    const content = this.commentDraft().trim();
    if (!content || this.sendingComment()) return;
    this.sendingComment.set(true);
    this.commentError.set('');
    this.objectiveService.addComment(objective.id, content).subscribe({
      next: comment => {
        this.comments.set([...this.comments(), comment]);
        this.commentDraft.set('');
        this.bumpCommentCount(objective, 1);
        this.sendingComment.set(false);
      },
      error: (e) => {
        this.sendingComment.set(false);
        this.commentError.set(mensajeDeError(e, 'No se pudo publicar el comentario.'));
      }
    });
  }

  // Lo puede borrar quien lo escribio o el dueño del objetivo (el backend lo controla).
  canDeleteComment(comment: ObjectiveComment, objective: Objective): boolean {
    return comment.userId === this.myId || objective.userId === this.myId;
  }

  deleteComment(comment: ObjectiveComment, objective: Objective): void {
    this.commentError.set('');
    this.objectiveService.deleteComment(comment.id).subscribe({
      next: () => {
        this.comments.set(this.comments().filter(c => c.id !== comment.id));
        this.bumpCommentCount(objective, -1);
      },
      error: (e) => this.commentError.set(mensajeDeError(e, 'No se pudo borrar el comentario.'))
    });
  }

  commentDate(comment: ObjectiveComment): Date {
    return fechaDelBackend(comment.createdAt);
  }

  commentPhoto(comment: ObjectiveComment): string | null {
    return comment.profilePhotoUrl ? this.apiOrigin + comment.profilePhotoUrl : null;
  }

  private bumpCommentCount(objective: Objective, delta: number): void {
    const actual = this.socialOf(objective);
    const nuevo = { ...actual, commentCount: Math.max(0, actual.commentCount + delta) };
    this.socialById.set(new Map(this.socialById()).set(objective.id, nuevo));
  }

  private setLikePending(objectiveId: number, pending: boolean): void {
    const ids = new Set(this.likePendingIds());
    if (pending) ids.add(objectiveId); else ids.delete(objectiveId);
    this.likePendingIds.set(ids);
  }

  statusLabel(status: ObjectiveStatus): string {
    return STATUS_LABEL[status] ?? status;
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.searchTerms.next(value.trim());
  }

  followsMe(userId: number): boolean {
    return this.followerIds().has(userId);
  }

  isFollowing(userId: number): boolean {
    return this.followingIds().has(userId);
  }

  isPending(userId: number): boolean {
    return this.pendingIds().has(userId);
  }

  photoUrl(u: { profilePhotoUrl?: string | null }): string | null {
    return u.profilePhotoUrl ? this.apiOrigin + u.profilePhotoUrl : null;
  }

  follow(u: User): void {
    this.actionError.set('');
    this.setPending(u.id, true);
    this.followService.follow(this.auth.getUserId(), u.id).subscribe({
      next: () => {
        this.followingIds.set(new Set(this.followingIds()).add(u.id));
        this.setPending(u.id, false);
        this.loadActivity();
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
        this.activity.set(this.activity().filter(i => i.owner.id !== target.id));
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
