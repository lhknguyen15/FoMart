import { request } from './client'

// Application/DTO/Auth/AuthModels.cs
export interface LoginRequest {
  phone: string
  password: string
}

export interface PinLoginRequest {
  phone: string
  pin: string
}

export type UserRole = 'owner' | 'staff'

export interface CurrentUserResponse {
  id: number
  name: string
  phone: string
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: CurrentUserResponse
}

// POST /api/auth/login: số điện thoại + mật khẩu (chủ tiệm).
export async function login(body: LoginRequest): Promise<AuthResponse> {
  const response = await request<AuthResponse>('/api/auth/login', { method: 'POST', body, anonymous: true })
  return response.dataResponse
}

// POST /api/auth/login-pin: số điện thoại + PIN 4–6 số (đổi ca nhanh).
export async function loginWithPin(body: PinLoginRequest): Promise<AuthResponse> {
  const response = await request<AuthResponse>('/api/auth/login-pin', { method: 'POST', body, anonymous: true })
  return response.dataResponse
}

// POST /api/auth/logout: thu hồi refresh token của phiên này trên server.
export async function logout(refreshToken: string): Promise<void> {
  await request<null>('/api/auth/logout', { method: 'POST', body: { refreshToken } })
}
