export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  user: {
    id: number;
    username: string;
    name: string;
    email: string;
    isActive: boolean;
    createdAt: string;
  };
}

export interface RegisterRequest {
  username: string;
  name: string;
  email: string;
  password: string;
}

export interface User {
  id: number;
  username: string;
  name: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}
