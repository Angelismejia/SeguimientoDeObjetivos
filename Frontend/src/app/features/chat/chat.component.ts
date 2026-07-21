import { Component, OnInit, OnDestroy, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ChatService } from '../../core/services/chat.service';
import { FollowService } from '../../core/services/follow.service';
import { UserService } from '../../core/services/user.service';
import { UserSummary } from '../../core/models/follow.model';
import { Message } from '../../core/models/message.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit, OnDestroy {
  loading = signal(true);
  loadError = signal(false);

  friends = signal<UserSummary[]>([]);
  activeFriend = signal<UserSummary | null>(null);
  messages = signal<Message[]>([]);
  loadingConversation = signal(false);
  sendError = signal<string | null>(null);

  messageForm: FormGroup;

  private apiOrigin = environment.apiUrl.replace('/api', '');
  private myId: number;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    protected chatService: ChatService,
    private followService: FollowService,
    private userService: UserService,
    private route: ActivatedRoute
  ) {
    this.myId = this.auth.getUserId();
    this.messageForm = this.fb.group({
      content: ['', Validators.required]
    });

    // Cuando llega un mensaje nuevo por SignalR, si es de/para la conversación abierta, lo agrego
    effect(() => {
      const incoming = this.chatService.lastMessage();
      if (!incoming) return;
      const friend = this.activeFriend();
      if (!friend) return;
      const belongsToThisChat =
        (incoming.senderId === friend.id && incoming.receiverId === this.myId) ||
        (incoming.senderId === this.myId && incoming.receiverId === friend.id);
      if (belongsToThisChat) {
        this.messages.update(current => [...current, incoming]);
      }
    });
  }

  ngOnInit(): void {
    this.chatService.connect();
    this.loadFriends();
  }

  ngOnDestroy(): void {
    this.chatService.setActiveFriend(null);
  }

  reconnect(): void {
    this.chatService.connect();
  }

  private loadFriends(): void {
    this.loading.set(true);
    this.loadError.set(false);
    forkJoin({
      following: this.followService.getFollowing(this.myId),
      followers: this.followService.getFollowers(this.myId)
    }).subscribe({
      next: ({ following, followers }) => {
        const byId = new Map<number, UserSummary>();
        for (const person of [...following, ...followers]) {
          byId.set(person.id, person);
        }
        this.friends.set(Array.from(byId.values()));
        this.loading.set(false);
        this.openFromQueryParam();
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  /** Si venimos de "Hablar" en el perfil de alguien (?with=id), abrimos esa conversación */
  private openFromQueryParam(): void {
    const withId = Number(this.route.snapshot.queryParamMap.get('with'));
    if (!withId) return;

    const existing = this.friends().find(f => f.id === withId);
    if (existing) {
      this.openConversation(existing);
      return;
    }

    this.userService.getById(withId).subscribe({
      next: user => {
        const summary: UserSummary = {
          id: user.id, username: user.username, name: user.name, profilePhotoUrl: user.profilePhotoUrl
        };
        this.friends.update(current => [...current, summary]);
        this.openConversation(summary);
      },
      error: () => {}
    });
  }

  friendPhotoUrl(friend: UserSummary): string | null {
    return friend.profilePhotoUrl ? this.apiOrigin + friend.profilePhotoUrl : null;
  }

  openConversation(friend: UserSummary): void {
    this.activeFriend.set(friend);
    this.chatService.setActiveFriend(friend.id);
    this.messages.set([]);
    this.loadingConversation.set(true);
    this.chatService.getConversation(this.myId, friend.id).subscribe({
      next: messages => {
        this.messages.set(messages);
        this.loadingConversation.set(false);
      },
      error: () => {
        this.loadingConversation.set(false);
      }
    });
  }

  closeConversation(): void {
    this.activeFriend.set(null);
    this.chatService.setActiveFriend(null);
  }

  isUnread(friend: UserSummary): boolean {
    return this.chatService.unreadFrom().has(friend.id);
  }

  isMine(message: Message): boolean {
    return message.senderId === this.myId;
  }

  send(): void {
    if (this.messageForm.invalid) return;
    const friend = this.activeFriend();
    if (!friend) return;

    const content = this.messageForm.value.content.trim();
    if (!content) return;

    this.sendError.set(null);
    this.chatService.sendMessage({ receiverId: friend.id, content })
      .then(() => {
        this.messageForm.reset({ content: '' });
      })
      .catch(err => {
        console.error('Error al enviar mensaje:', err);
        this.sendError.set('No se pudo enviar el mensaje. Revisá tu conexión.');
      });
  }
}
