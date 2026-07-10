export interface Category {
  id: number;
  name: string;
  color?: string;
  icon?: string;
  isDefault: boolean;
  userId: number;
  createdAt: string;
}

export interface CreateCategoryDto {
  name: string;
  color?: string;
  icon?: string;
  isDefault?: boolean;
}

export interface UpdateCategoryDto {
  name: string;
  color?: string;
  icon?: string;
  isDefault?: boolean;
}
