export interface Message {
  id: number;
  senderId: number;
  receiverId: number;
  content: string;
  sentAt: string;
  readAt?: string;
}

export interface CreateMessageDto {
  receiverId: number;
  content: string;
}
