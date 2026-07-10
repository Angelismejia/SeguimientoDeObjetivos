export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Skipped';
export type TaskPriority = 'Low' | 'Medium' | 'High';
export type RecurrenceType = 'None' | 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  emoji?: string;
  color?: string;
  scheduledDate: string;
  scheduledTime?: string;
  reminderMinutesBefore?: number;
  priority: TaskPriority;
  status: TaskStatus;
  isRecurring: boolean;
  recurrenceType: RecurrenceType;
  repeatEveryWeeks?: number;
  endRepeatDate?: string;
  userId: number;
  objectiveId?: number;
  categoryId?: number;
  createdAt: string;
  updatedAt?: string;
  completedAt?: string;
}

export interface CreateTaskDto {
  title: string;
  description?: string;
  emoji?: string;
  color?: string;
  scheduledDate: string;
  scheduledTime?: string;
  reminderMinutesBefore?: number;
  priority?: TaskPriority;
  isRecurring?: boolean;
  recurrenceType?: RecurrenceType;
  repeatEveryWeeks?: number;
  endRepeatDate?: string;
  objectiveId?: number;
  categoryId?: number;
}

export interface UpdateTaskDto {
  title: string;
  description?: string;
  emoji?: string;
  color?: string;
  scheduledDate: string;
  scheduledTime?: string;
  reminderMinutesBefore?: number;
  priority: TaskPriority;
  status: TaskStatus;
  isRecurring: boolean;
  recurrenceType?: RecurrenceType;
  repeatEveryWeeks?: number;
  endRepeatDate?: string;
  objectiveId?: number;
  categoryId?: number;
}
