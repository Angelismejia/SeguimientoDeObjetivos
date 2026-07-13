export interface UserSummary {
  id: number;
  username: string;
  name: string;
  profilePhotoUrl?: string;
}

export interface Follow {
  id: number;
  followerId: number;
  followingId: number;
  createdAt: string;
}
