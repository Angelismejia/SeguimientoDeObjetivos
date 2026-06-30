export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Skipped';
export type TaskPriority = 'Low' | 'Medium' | 'High';
export type RecurrenceType = 'None' | 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  recurrenceType: RecurrenceType;
  scheduledDate?: string;
  dueDate?: string;
  objectiveId?: number;
  categoryId?: number;
  userId: number;
  createdAt: string;
}

export interface CreateTaskDto {
  title: string;
  description?: string;
  priority?: TaskPriority;
  recurrenceType?: RecurrenceType;
  scheduledDate?: string;
  dueDate?: string;
  objectiveId?: number;
  categoryId?: number;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  status?: TaskStatus;
  priority?: TaskPriority;
  recurrenceType?: RecurrenceType;
  scheduledDate?: string;
  dueDate?: string;
}
