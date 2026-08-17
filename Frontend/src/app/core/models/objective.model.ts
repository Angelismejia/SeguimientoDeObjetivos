export type ObjectiveStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';

export interface Objective {
  id: number;
  title: string;
  description?: string;
  status: ObjectiveStatus;
  progressPercentage: number;
  isPrimary: boolean;
  isPrivate: boolean;
  categoryId?: number;
  /** Con cuantas tareas el usuario dijo "todavia no" a darlo por terminado. */
  completionAskedAtTaskCount?: number | null;
  categoryName?: string;
  startDate?: string;
  endDate?: string;
  userId: number;
  createdAt: string;
}

export interface CreateObjectiveDto {
  title: string;
  description?: string;
  categoryId?: number;
  /** Con cuantas tareas el usuario dijo "todavia no" a darlo por terminado. */
  completionAskedAtTaskCount?: number | null;
  startDate?: string;
  endDate?: string;
}

export interface UpdateObjectiveDto {
  title?: string;
  description?: string;
  status?: ObjectiveStatus;
  progressPercentage?: number;
  categoryId?: number;
  /** Con cuantas tareas el usuario dijo "todavia no" a darlo por terminado. */
  completionAskedAtTaskCount?: number | null;
  startDate?: string;
  endDate?: string;
}
