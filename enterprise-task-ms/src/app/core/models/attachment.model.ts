export interface Attachment {
  id: number;
  taskId?: number;
  fileName?: string;
  fileUrl?: string;
  fileSize?: number;
  uploadedBy?: number;
  createdAt: Date;
}