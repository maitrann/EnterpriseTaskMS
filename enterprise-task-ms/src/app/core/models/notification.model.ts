export interface Notification {
  id: number;
  userId: number;
  title?: string;
  content?: string;
  type?: string;
  referenceId?: number;
  isRead: boolean;
  createdAt: Date;
}