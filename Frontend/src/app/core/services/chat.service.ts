import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { Message, CreateMessageDto } from '../models/message.model';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private url = `${environment.apiUrl}/messages`;
  private connection: signalR.HubConnection | null = null;

  lastMessage = signal<Message | null>(null);
  connected = signal(false);
  connectionError = signal<string | null>(null);

  /** id del amigo cuya conversación está abierta ahora mismo (null si ninguna) */
  activeFriendId = signal<number | null>(null);
  /** ids de amigos con mensajes sin leer, para mostrar antes de abrir la conversación */
  unreadFrom = signal<Set<number>>(new Set());

  constructor(
    private http: HttpClient,
    private auth: AuthService
  ) {}

  connect(): void {
    if (this.connection) return;
    this.buildAndStart(undefined);
  }

  private buildAndStart(forceTransport: signalR.HttpTransportType | undefined): void {
    const builder = new signalR.HubConnectionBuilder();
    const options: signalR.IHttpConnectionOptions = {
      accessTokenFactory: () => this.auth.getToken() ?? ''
    };
    if (forceTransport) {
      options.transport = forceTransport;
    }

    this.connection = builder
      .withUrl(environment.hubUrl, options)
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveMessage', (message: Message) => {
      this.lastMessage.set(message);

      const myId = this.auth.getUserId();
      const isIncoming = message.senderId !== myId;
      const isOpenConversation = message.senderId === this.activeFriendId();
      if (isIncoming && !isOpenConversation) {
        this.unreadFrom.update(current => new Set(current).add(message.senderId));
      }
    });

    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => { this.connected.set(true); this.connectionError.set(null); });
    this.connection.onclose(err => {
      this.connected.set(false);
      this.connection = null;
      if (err) this.connectionError.set('Se perdió la conexión del chat. Volvé a intentar.');
    });

    this.connection.start()
      .then(() => {
        this.connected.set(true);
        this.connectionError.set(null);
      })
      .catch(err => {
        console.error(`Error al conectar el chat (transporte ${forceTransport ?? 'auto'}):`, err);
        this.connection = null;
        this.connected.set(false);

        if (!forceTransport) {
          // El WebSocket puede estar bloqueado por la red (WiFi/proxy). Reintentamos
          // forzando long polling, que funciona sobre HTTPS normal sin "upgrade".
          this.buildAndStart(signalR.HttpTransportType.LongPolling);
          return;
        }

        this.connectionError.set('No se pudo conectar al chat. Revisá tu conexión y volvé a intentar.');
      });
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
    this.connected.set(false);
  }

  setActiveFriend(friendId: number | null): void {
    this.activeFriendId.set(friendId);
    if (friendId !== null) this.markRead(friendId);
  }

  markRead(friendId: number): void {
    if (!this.unreadFrom().has(friendId)) return;
    this.unreadFrom.update(current => {
      const next = new Set(current);
      next.delete(friendId);
      return next;
    });
  }

  sendMessage(dto: CreateMessageDto): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return Promise.reject('No hay conexión de chat activa.');
    }
    return this.connection.invoke('SendMessage', dto);
  }

  getConversation(userAId: number, userBId: number): Observable<Message[]> {
    return this.http.get<Message[]>(`${this.url}/conversation?userAId=${userAId}&userBId=${userBId}`);
  }
}
