export interface Notification {
  id: number;
  userId: number;
  taskId?: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  sendEmail: boolean;
  sentAt?: string;
  createdAt: string;
}
