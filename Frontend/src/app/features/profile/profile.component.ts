import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ChatService } from '../../core/services/chat.service';
import { UserService } from '../../core/services/user.service';
import { FollowService } from '../../core/services/follow.service';
import { ThemeService } from '../../core/services/theme.service';
import { FriendStreakService } from '../../core/services/friend-streak.service';
import { TaskService } from '../../core/services/task.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { BadgeService } from '../../core/services/badge.service';
import { DiaryEntryService } from '../../core/services/diary-entry.service';
import { User } from '../../core/models/user.model';
import { UserSummary } from '../../core/models/follow.model';
import { FriendStreak, FriendStreakInvitation } from '../../core/models/friend-streak.model';
import { TaskItem } from '../../core/models/task.model';
import { Objective } from '../../core/models/objective.model';
import { Badge } from '../../core/models/badge.model';
import { DiaryEntry } from '../../core/models/diary-entry.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { environment } from '../../../environments/environment';
import { mensajeDeError } from '../../core/utils/http-error.util';

interface HeatmapCell {
  key: string;
  completed: boolean;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit, AfterViewInit {
  @ViewChild('heatmapWrap') heatmapWrap?: ElementRef<HTMLElement>;

  loading = signal(true);
  loadError = signal(false);
  uploading = signal(false);
  uploadError = signal('');

  user = signal<User | null>(null);
  followers = signal<UserSummary[]>([]);
  following = signal<UserSummary[]>([]);
  streak = signal(0);
  allTasks = signal<TaskItem[]>([]);
  objectives = signal<Objective[]>([]);
  badges = signal<Badge[]>([]);
  diaryEntries = signal<DiaryEntry[]>([]);

  friendStreaks = signal<FriendStreak[]>([]);
  receivedInvitations = signal<FriendStreakInvitation[]>([]);
  sentInvitations = signal<FriendStreakInvitation[]>([]);

  // ── Configuración ────────────────────────────────────
  showSettings = signal(false);

  // ── Tema ──────────────────────────────────────────────
  theme = signal<'Light' | 'Dark'>('Light');
  savingTheme = signal(false);

  // ── Editar perfil ───────────────────────────────────
  showEditProfile = signal(false);
  editProfileForm: FormGroup;
  savingProfile = signal(false);
  editProfileError = signal('');

  // ── Cambiar contraseña ───────────────────────────────
  showChangePassword = signal(false);
  changePasswordForm: FormGroup;
  savingPassword = signal(false);
  changePasswordError = signal('');
  changePasswordSuccess = signal(false);

  // ── Compartir perfil ─────────────────────────────────
  shareCopied = signal(false);

  // ── Privacidad: quién puede seguirme ─────────────────
  savingFollowPrivacy = signal(false);

  // ── Exportar datos ────────────────────────────────────
  exportingData = signal(false);
  exportError = signal('');

  // ── Eliminar cuenta ────────────────────────────────────
  showDeleteAccount = signal(false);
  deleteAccountForm: FormGroup;
  deletingAccount = signal(false);
  deleteAccountError = signal('');

  private apiOrigin = environment.apiUrl.replace('/api', '');

  // ── Agregar amigo por username ────────────────────────
  showAddFriend = signal(false);
  addFriendForm: FormGroup;
  searching = signal(false);
  searchError = signal('');
  allUsers = signal<User[]>([]);
  searchResults = signal<User[]>([]);

  unfollowTarget = signal<UserSummary | null>(null);

  // ── Acción sobre un amigo (invitar a racha / dejar de seguir) ──
  friendAction = signal<UserSummary | null>(null);
  friendActionError = signal('');

  // ── Listado de siguiendo / seguidores ──────────────────
  listModal = signal<'following' | 'followers' | null>(null);

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private auth: AuthService,
    private chatService: ChatService,
    private userService: UserService,
    private followService: FollowService,
    private themeService: ThemeService,
    private friendStreakService: FriendStreakService,
    private taskService: TaskService,
    private objectiveService: ObjectiveService,
    private badgeService: BadgeService,
    private diaryEntryService: DiaryEntryService
  ) {
    this.addFriendForm = this.fb.group({
      username: ['']
    });
    this.addFriendForm.get('username')!.valueChanges.subscribe(() => this.applyFilter());

    this.editProfileForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });

    this.changePasswordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.deleteAccountForm = this.fb.group({
      password: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadAll();
    // El sidebar/drawer enlazan a /profile?settings=1 para "Configuración" en
    // vez de duplicar ese modal como una pantalla propia.
    //
    // Va suscrito y no por snapshot: estando ya en /profile, Angular reutiliza el
    // componente cuando solo cambia el query param, asi que ngOnInit no vuelve a
    // correr y con snapshot el modal no abria.
    this.route.queryParamMap.subscribe(params => {
      if (params.get('settings')) this.openSettings();
    });
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const userId = this.auth.getUserId();
    forkJoin({
      user: this.userService.getById(userId),
      followers: this.followService.getFollowers(userId),
      following: this.followService.getFollowing(userId),
      tasks: this.taskService.getAll(userId),
      objectives: this.objectiveService.getAll(userId),
      badges: this.badgeService.getByUser(userId),
      diaryEntries: this.diaryEntryService.getAll(userId),
      friendStreaks: this.friendStreakService.getForUser(userId),
      receivedInvitations: this.friendStreakService.getReceivedInvitations(userId),
      sentInvitations: this.friendStreakService.getSentInvitations(userId)
    }).subscribe({
      next: ({ user, followers, following, tasks, objectives, badges, diaryEntries, friendStreaks, receivedInvitations, sentInvitations }) => {
        this.user.set(user);
        this.objectives.set(objectives);
        this.badges.set(badges);
        this.diaryEntries.set(diaryEntries);
        this.followers.set(followers);
        this.following.set(following);
        this.allTasks.set(tasks);
        this.streak.set(this.computeStreak(tasks));
        this.friendStreaks.set(friendStreaks);
        this.receivedInvitations.set(receivedInvitations);
        this.sentInvitations.set(sentInvitations);
        this.loading.set(false);
        setTimeout(() => this.scrollHeatmapToToday());
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  ngAfterViewInit(): void {
    this.scrollHeatmapToToday();
  }

  private scrollHeatmapToToday(): void {
    const el = this.heatmapWrap?.nativeElement;
    if (el) el.scrollLeft = el.scrollWidth;
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

  // ── Mapa de calor de actividad ────────────────────────
  private completedDays(): Set<string> {
    return new Set(
      this.allTasks()
        .filter(t => t.status === 'Completed' && !!t.scheduledDate)
        .map(t => t.scheduledDate.substring(0, 10))
    );
  }

  activityHeatmap(): HeatmapCell[] {
    const days = this.completedDays();
    const today = new Date();
    const cells: HeatmapCell[] = [];
    for (let i = 363; i >= 0; i--) {
      const key = this.dateKey(this.addDays(today, -i));
      cells.push({ key, completed: days.has(key) });
    }
    return cells;
  }

  tasksCompletedOn(key: string): TaskItem[] {
    return this.allTasks().filter(
      t => t.status === 'Completed' && t.scheduledDate?.substring(0, 10) === key
    );
  }

  heatmapCellTitle(cell: HeatmapCell): string {
    const date = this.parseDateKey(cell.key);
    const dateLabel = date.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
    if (!cell.completed) return `${dateLabel} — sin actividad`;
    const titles = this.tasksCompletedOn(cell.key).map(t => t.title);
    return `${dateLabel}:\n${titles.join('\n')}`;
  }

  selectedHeatmapCell = signal<string | null>(null);

  selectHeatmapCell(cell: HeatmapCell): void {
    this.selectedHeatmapCell.set(this.selectedHeatmapCell() === cell.key ? null : cell.key);
  }

  selectedHeatmapLabel(): string | null {
    const key = this.selectedHeatmapCell();
    if (!key) return null;
    const date = this.parseDateKey(key);
    const dateLabel = date.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
    const titles = this.tasksCompletedOn(key).map(t => t.title);
    return titles.length > 0 ? `${dateLabel}: ${titles.join(', ')}` : `${dateLabel} — sin tareas completadas`;
  }

  // ── Resumen ────────────────────────────────────────
  activeObjectivesCount(): number {
    return this.objectives().filter(o => o.status !== 'Completed' && o.status !== 'Cancelled').length;
  }

  completedTasksCount(): number {
    return this.allTasks().filter(t => t.status === 'Completed').length;
  }

  previewBadges(): Badge[] {
    return this.badges().slice(0, 4);
  }

  // ── Configuración ────────────────────────────────────
  openSettings(): void {
    this.showSettings.set(true);
    this.themeService.getTheme(this.auth.getUserId()).subscribe(res => this.theme.set(res.theme));
  }

  closeSettings(): void {
    this.showSettings.set(false);
    // Sin esto el ?settings=1 queda pegado en la URL y el siguiente clic en
    // "Configuración" navega al mismo sitio: Angular no emite nada y el modal
    // no vuelve a abrir hasta salir de la pantalla y entrar de nuevo.
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { settings: null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  // ── Tema ──────────────────────────────────────────────
  setTheme(theme: 'Light' | 'Dark'): void {
    if (this.theme() === theme || this.savingTheme()) return;
    this.savingTheme.set(true);
    this.themeService.setTheme(this.auth.getUserId(), theme).subscribe({
      next: res => {
        this.theme.set(res.theme);
        this.savingTheme.set(false);
      },
      error: () => this.savingTheme.set(false)
    });
  }

  logout(): void {
    this.chatService.disconnect();
    this.auth.logout();
  }

  // ── Editar perfil ────────────────────────────────────
  openEditProfile(): void {
    const u = this.user();
    if (!u) return;
    this.editProfileError.set('');
    this.editProfileForm.reset({ name: u.name, email: u.email });
    this.showEditProfile.set(true);
  }

  closeEditProfile(): void {
    this.showEditProfile.set(false);
  }

  submitEditProfile(): void {
    const u = this.user();
    if (!u || this.editProfileForm.invalid) return;
    this.savingProfile.set(true);
    this.editProfileError.set('');

    const v = this.editProfileForm.value;
    this.userService.update(u.id, { name: v.name, email: v.email, isActive: u.isActive }).subscribe({
      next: updated => {
        this.user.set(updated);
        this.savingProfile.set(false);
        this.showEditProfile.set(false);
      },
      error: (e) => {
        this.savingProfile.set(false);
        this.editProfileError.set(mensajeDeError(e, 'No se pudo guardar. Intenta de nuevo.'));
      }
    });
  }

  // ── Cambiar contraseña ────────────────────────────────
  openChangePassword(): void {
    this.changePasswordError.set('');
    this.changePasswordSuccess.set(false);
    this.changePasswordForm.reset({ currentPassword: '', newPassword: '' });
    this.showChangePassword.set(true);
  }

  closeChangePassword(): void {
    this.showChangePassword.set(false);
  }

  submitChangePassword(): void {
    const u = this.user();
    if (!u || this.changePasswordForm.invalid) return;
    this.savingPassword.set(true);
    this.changePasswordError.set('');
    this.changePasswordSuccess.set(false);

    const v = this.changePasswordForm.value;
    this.userService.changePassword(u.id, { currentPassword: v.currentPassword, newPassword: v.newPassword }).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.changePasswordSuccess.set(true);
        this.changePasswordForm.reset({ currentPassword: '', newPassword: '' });
      },
      error: (err) => {
        this.savingPassword.set(false);
        this.changePasswordError.set(
          err?.status === 400 ? 'La contraseña actual es incorrecta.' : 'No se pudo cambiar la contraseña. Intenta de nuevo.'
        );
      }
    });
  }

  // ── Exportar datos ────────────────────────────────────
  exportData(): void {
    const u = this.user();
    if (!u || this.exportingData()) return;
    this.exportingData.set(true);
    this.exportError.set('');

    this.userService.exportData(u.id).subscribe({
      next: data => {
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `seguimiento-objetivos-${u.username}-${this.dateKey(new Date())}.json`;
        link.click();
        URL.revokeObjectURL(url);
        this.exportingData.set(false);
      },
      error: (e) => {
        this.exportingData.set(false);
        this.exportError.set(mensajeDeError(e, 'No se pudieron exportar los datos. Intenta de nuevo.'));
      }
    });
  }

  // ── Eliminar cuenta ────────────────────────────────────
  openDeleteAccount(): void {
    this.deleteAccountError.set('');
    this.deleteAccountForm.reset({ password: '' });
    this.showDeleteAccount.set(true);
  }

  closeDeleteAccount(): void {
    this.showDeleteAccount.set(false);
  }

  submitDeleteAccount(): void {
    const u = this.user();
    if (!u || this.deleteAccountForm.invalid) return;
    this.deletingAccount.set(true);
    this.deleteAccountError.set('');

    this.userService.deleteAccount(u.id, { password: this.deleteAccountForm.value.password }).subscribe({
      next: () => {
        this.chatService.disconnect();
        this.auth.logout();
      },
      error: (err) => {
        this.deletingAccount.set(false);
        this.deleteAccountError.set(
          err?.status === 400 ? 'La contraseña es incorrecta.' : 'No se pudo eliminar la cuenta. Intenta de nuevo.'
        );
      }
    });
  }

  // ── Privacidad: quién puede seguirme ──────────────────
  toggleAllowFollows(): void {
    const u = this.user();
    if (!u || this.savingFollowPrivacy()) return;
    this.savingFollowPrivacy.set(true);

    this.userService.updateFollowPrivacy(u.id, { allowFollows: !u.allowFollows }).subscribe({
      next: updated => {
        this.user.set(updated);
        this.savingFollowPrivacy.set(false);
      },
      error: () => {
        this.savingFollowPrivacy.set(false);
      }
    });
  }

  // ── Compartir perfil ──────────────────────────────────
  shareProfile(): void {
    const u = this.user();
    if (!u) return;
    const url = `${window.location.origin}/profile/${u.id}`;
    navigator.clipboard.writeText(url).then(() => {
      this.shareCopied.set(true);
      setTimeout(() => this.shareCopied.set(false), 2000);
    });
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
      error: (e) => {
        this.uploading.set(false);
        this.uploadError.set(mensajeDeError(e, 'No se pudo subir la foto. Probá con una imagen JPG, PNG o WEBP.'));
        input.value = '';
      }
    });
  }

  // ── Agregar amigo ──────────────────────────────────────
  openAddFriend(): void {
    this.addFriendForm.reset({ username: '' });
    this.searchError.set('');
    this.searchResults.set([]);
    this.showAddFriend.set(true);

    this.searching.set(true);
    this.userService.getAll().subscribe({
      next: users => {
        this.allUsers.set(users);
        this.searching.set(false);
        this.applyFilter();
      },
      error: (e) => {
        this.searching.set(false);
        this.searchError.set(mensajeDeError(e, 'No se pudo cargar la lista de usuarios.'));
      }
    });
  }

  closeAddFriend(): void {
    this.showAddFriend.set(false);
  }

  private applyFilter(): void {
    const query = (this.addFriendForm.value.username ?? '').trim().toLowerCase();
    if (!query) {
      this.searchResults.set([]);
      return;
    }
    // Los que ya seguis tambien aparecen: buscar a alguien y no encontrarlo se lee
    // como que no existe, no como que ya es tu amigo. En la lista se distinguen
    // porque en vez del boton "Seguir" muestran que ya lo seguis.
    const myId = this.auth.getUserId();
    this.searchResults.set(
      this.allUsers()
        .filter(u => u.id !== myId)
        .filter(u => u.username.toLowerCase().includes(query) || u.name.toLowerCase().includes(query))
        .slice(0, 20)
    );
  }

  yaLoSigo(userId: number): boolean {
    return this.following().some(f => f.id === userId);
  }

  follow(found: User): void {
    this.followService.follow(this.auth.getUserId(), found.id).subscribe({
      next: () => {
        this.following.set([...this.following(), {
          id: found.id,
          username: found.username,
          name: found.name,
          profilePhotoUrl: found.profilePhotoUrl
        }]);
        // Ya no se saca de la lista: se queda visible y el boton pasa a "Ya lo
        // sigues", que confirma que la accion funciono. Sacarlo parecia un error.
      },
      error: (e) => {
        this.searchError.set(mensajeDeError(e, 'No se pudo seguir a este usuario. Intenta de nuevo.'));
      }
    });
  }

  // ── Listado de siguiendo / seguidores ──────────────────
  openList(type: 'following' | 'followers'): void {
    this.listModal.set(type);
  }

  closeList(): void {
    this.listModal.set(null);
  }

  openFriendFromList(friend: UserSummary): void {
    this.listModal.set(null);
    this.openFriendAction(friend);
  }

  // ── Acción sobre un amigo ──────────────────────────────
  openFriendAction(friend: UserSummary): void {
    this.friendActionError.set('');
    this.friendAction.set(friend);
  }

  closeFriendAction(): void {
    this.friendAction.set(null);
  }

  viewFriendProfile(friend: UserSummary): void {
    this.friendAction.set(null);
    this.router.navigate(['/profile', friend.id]);
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
      error: (e) => {
        this.friendActionError.set(mensajeDeError(e, 'No se pudo enviar la invitación. Intenta de nuevo.'));
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
