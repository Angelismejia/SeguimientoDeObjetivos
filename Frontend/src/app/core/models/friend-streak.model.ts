export interface FriendStreakInvitation {
  id: number;
  fromUserId: number;
  fromUsername: string;
  fromName: string;
  fromProfilePhotoUrl?: string;
  toUserId: number;
  status: string;
  createdAt: string;
}

export interface FriendStreak {
  id: number;
  partnerId: number;
  partnerUsername: string;
  partnerName: string;
  partnerProfilePhotoUrl?: string;
  currentStreak: number;
  createdAt: string;
}
