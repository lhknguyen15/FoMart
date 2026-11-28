import { useMutation, useQueryClient } from '@tanstack/react-query'
import { login, loginWithPin, logout } from '../../../api/auth'
import { useAuthStore, type LoginMethod } from '../../../stores/useAuthStore'

export interface LoginInput {
  phone: string
  secret: string
  method: LoginMethod
}

// Đăng nhập bằng mật khẩu hoặc PIN. Thành công: lưu phiên vào useAuthStore, xóa cache dữ liệu của người trước.
export function useLogin() {
  const queryClient = useQueryClient()
  const signIn = useAuthStore((state) => state.signIn)
  return useMutation({
    mutationFn: ({ phone, secret, method }: LoginInput) =>
      method === 'pin' ? loginWithPin({ phone, pin: secret }) : login({ phone, password: secret }),
    onSuccess: (auth, input) => {
      queryClient.clear()
      signIn(auth, input.method)
    },
  })
}

// Đăng xuất: thu hồi refresh token trên server (lỗi mạng cũng không giữ người dùng lại), bỏ phiên, xóa cache.
export function useLogout() {
  const queryClient = useQueryClient()
  const signOut = useAuthStore((state) => state.signOut)
  return useMutation({
    mutationFn: async () => {
      const session = useAuthStore.getState().session
      if (session) await logout(session.refreshToken).catch(() => undefined)
    },
    onSettled: () => {
      signOut()
      queryClient.clear()
    },
  })
}
