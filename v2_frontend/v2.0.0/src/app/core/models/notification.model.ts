export interface AppNotification {
  id: number;
  message: string;
  linkPath: string | null;
  isRead: boolean;
  createdAt: string;
}
