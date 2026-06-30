export interface DiaryEntry {
  id: number;
  title: string;
  content: string;
  userId: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateDiaryEntryDto {
  title: string;
  content: string;
}

export interface UpdateDiaryEntryDto {
  title?: string;
  content?: string;
}
