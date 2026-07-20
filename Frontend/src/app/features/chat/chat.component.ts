import { Component, OnInit, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ChatService } from '../../core/services/chat.service';
import { FollowService } from '../../core/services/follow.service';
import { UserSummary } from '../../core/models/follow.model';
import { Message } from '../../core/models/message.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit {
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
    private followService: FollowService
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

  reconnect(): void {
    this.chatService.connect();
  }

  private loadFriends(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.followService.getFollowing(this.myId).subscribe({
      next: friends => {
        this.friends.set(friends);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  friendPhotoUrl(friend: UserSummary): string | null {
    return friend.profilePhotoUrl ? this.apiOrigin + friend.profilePhotoUrl : null;
  }

  openConversation(friend: UserSummary): void {
    this.activeFriend.set(friend);
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
