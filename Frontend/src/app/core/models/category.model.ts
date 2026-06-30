export interface Category {
  id: number;
  name: string;
      color: string;
  icon: string;
  userid: number;
}                                                                           

export interface CreateCategoryDto {
  name: string;
  color: string;
  icon: string;
}

export interface UpdateCategoryDto {
  name: string;
  color: string;
  icon: string;
}

