import { Component, OnInit, OnDestroy, computed, effect, signal } from '@angular/core';
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
import { mensajeDeError } from '../../core/utils/http-error.util';
import { fechaDelBackend } from '../../core/utils/fecha.util';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit, OnDestroy {
  loading = signal(true);
  loadError = signal('');
  conversationError = signal('');

  friends = signal<UserSummary[]>([]);
  activeFriend = signal<UserSummary | null>(null);
  messages = signal<Message[]>([]);
  loadingConversation = signal(false);
  sendError = signal<string | null>(null);
  enviando = signal(false);

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
    this.loadError.set('');
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
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudo cargar tu lista de amigos.'));
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
      error: (e) => this.loadError.set(
        mensajeDeError(e, 'No se pudo abrir esa conversación.'))
    });
  }

  /**
   * Los mensajes agrupados por dia, para poder separarlos con su fecha. Sin esto
   * una conversacion larga es un bloque continuo donde no se sabe si algo se
   * dijo hoy o hace tres semanas.
   */
  readonly messageDays = computed(() => {
    const dias: { clave: string; etiqueta: string; mensajes: Message[] }[] = [];

    for (const mensaje of this.messages()) {
      const clave = this.claveDeDia(fechaDelBackend(mensaje.sentAt));
      const ultimo = dias[dias.length - 1];

      if (ultimo && ultimo.clave === clave) {
        ultimo.mensajes.push(mensaje);
      } else {
        dias.push({
          clave,
          etiqueta: this.etiquetaDeDia(fechaDelBackend(mensaje.sentAt)),
          mensajes: [mensaje]
        });
      }
    }

    return dias;
  });

  hora(mensaje: Message): string {
    return fechaDelBackend(mensaje.sentAt)
      .toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' });
  }

  private claveDeDia(fecha: Date): string {
    return `${fecha.getFullYear()}-${fecha.getMonth()}-${fecha.getDate()}`;
  }

  // "Hoy" y "Ayer" se leen mejor que la fecha completa, que es lo que uno busca
  // al desplazarse hacia arriba en una conversacion. El anio solo aparece cuando
  // no es el actual, para no repetirlo en cada separador.
  private etiquetaDeDia(fecha: Date): string {
    const hoy = new Date();
    if (this.claveDeDia(fecha) === this.claveDeDia(hoy)) return 'Hoy';

    const ayer = new Date(hoy);
    ayer.setDate(hoy.getDate() - 1);
    if (this.claveDeDia(fecha) === this.claveDeDia(ayer)) return 'Ayer';

    return fecha.toLocaleDateString('es', fecha.getFullYear() === hoy.getFullYear()
      ? { day: 'numeric', month: 'long' }
      : { day: 'numeric', month: 'long', year: 'numeric' });
  }

  friendPhotoUrl(friend: UserSummary): string | null {
    return friend.profilePhotoUrl ? this.apiOrigin + friend.profilePhotoUrl : null;
  }

  openConversation(friend: UserSummary): void {
    this.activeFriend.set(friend);
    this.chatService.setActiveFriend(friend.id);
    this.messages.set([]);
    this.conversationError.set('');
    this.loadingConversation.set(true);
    this.chatService.getConversation(this.myId, friend.id).subscribe({
      next: messages => {
        this.messages.set(messages);
        this.loadingConversation.set(false);
      },
      error: (e) => {
        this.loadingConversation.set(false);
        this.conversationError.set(
          mensajeDeError(e, 'No se pudo cargar la conversación.'));
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
    // El input se limpia recien cuando responde el servidor, asi que dos Enter
    // seguidos alcanzaban para mandar el mismo mensaje dos veces: el segundo
    // send() encontraba el texto todavia puesto. El cerrojo cierra esa ventana.
    if (this.enviando()) return;

    if (this.messageForm.invalid) return;
    const friend = this.activeFriend();
    if (!friend) return;

    const content = this.messageForm.value.content.trim();
    if (!content) return;

    this.sendError.set(null);
    this.enviando.set(true);
    this.chatService.sendMessage({ receiverId: friend.id, content })
      .then(() => {
        this.messageForm.reset({ content: '' });
      })
      .catch(err => {
        console.error('Error al enviar mensaje:', err);
        this.sendError.set('No se pudo enviar el mensaje. Revisá tu conexión.');
      })
      .finally(() => {
        this.enviando.set(false);
      });
  }
}
