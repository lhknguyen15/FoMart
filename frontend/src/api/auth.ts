import { request } from './client'

// Application/DTO/Auth/AuthModels.cs
export interface LoginRequest {
  phone: string
  password: string
}

export interface CurrentUserResponse {
  id: number
  name: string
  phone: string
  role: 'owner' | 'staff'
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: CurrentUserResponse
}

// POST /api/auth/login: chủ tiệm đăng nhập bằng số điện thoại + mật khẩu.
export async function login(body: LoginRequest): Promise<AuthResponse> {
  const response = await request<AuthResponse>('/api/auth/login', { method: 'POST', body })
  return response.dataResponse
}
