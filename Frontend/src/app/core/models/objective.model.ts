export type ObjectiveStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';

export interface Objective {
  id: number;
  title: string;
  description?: string;
  status: ObjectiveStatus;
  progressPercentage: number;
  categoryId?: number;
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
  startDate?: string;
  endDate?: string;
}

export interface UpdateObjectiveDto {
  title?: string;
  description?: string;
  status?: ObjectiveStatus;
  progressPercentage?: number;
  categoryId?: number;
  startDate?: string;
  endDate?: string;
}
