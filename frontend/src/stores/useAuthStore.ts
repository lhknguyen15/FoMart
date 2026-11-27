import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { AuthResponse, CurrentUserResponse, UserRole } from '../api/auth'

// Phiên đăng nhập trên máy này + danh sách người đã đăng nhập trên máy (để đổi ca bằng PIN).
// Lưu ở localStorage để F5 hay mở lại app không phải đăng nhập lại.
// Lưu ý: refresh token nằm trong localStorage, nên app không được chạy mã HTML/JS lạ (XSS).

export type LoginMethod = 'password' | 'pin'

export interface Session {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  user: CurrentUserResponse
}

// Người từng đăng nhập trên máy: chạm tên rồi nhập PIN (hoặc mật khẩu) thay vì gõ lại số điện thoại.
export interface KnownUser {
  id: number
  name: string
  phone: string
  role: UserRole
  method: LoginMethod
}

interface AuthState {
  session: Session | null
  knownUsers: KnownUser[]
  // Đăng nhập thành công: lưu phiên, đưa người này lên đầu danh sách trên máy.
  signIn: (auth: AuthResponse, method: LoginMethod) => void
  // Refresh thành công: thay cặp token, giữ nguyên người dùng.
  replaceTokens: (auth: AuthResponse) => void
  // Đăng xuất / phiên hết hạn: bỏ phiên, vẫn nhớ người dùng để lần sau chạm tên.
  signOut: () => void
  // Bỏ một người khỏi danh sách trên máy này.
  forgetUser: (id: number) => void
}

const MAX_KNOWN_USERS = 8

function toSession(auth: AuthResponse): Session {
  return {
    accessToken: auth.accessToken,
    accessTokenExpiresAt: auth.accessTokenExpiresAt,
    refreshToken: auth.refreshToken,
    user: auth.user,
  }
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      session: null,
      knownUsers: [],
      signIn: (auth, method) =>
        set((state) => ({
          session: toSession(auth),
          knownUsers: [
            { id: auth.user.id, name: auth.user.name, phone: auth.user.phone, role: auth.user.role, method },
            ...state.knownUsers.filter((u) => u.id !== auth.user.id),
          ].slice(0, MAX_KNOWN_USERS),
        })),
      replaceTokens: (auth) => set((state) => (state.session ? { session: toSession(auth) } : state)),
      signOut: () => set({ session: null }),
      forgetUser: (id) => set((state) => ({ knownUsers: state.knownUsers.filter((u) => u.id !== id) })),
    }),
    {
      name: 'fomart-auth',
      version: 1,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ session: state.session, knownUsers: state.knownUsers }),
    },
  ),
)
