import { useAuthStore } from '../stores/useAuthStore'
import type { AuthResponse } from './auth'
import { ApiError, type HTTPResponseData } from './types'

// Nơi DUY NHẤT trong frontend gọi fetch. Các file api/<mảng>.ts gọi request(), component không gọi trực tiếp.
// Tự gắn access token của phiên hiện tại và tự làm mới khi token sắp hết hạn hoặc server trả 401.

const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? ''
const timeoutMs = 15_000
// Làm mới trước khi access token hết hạn chừng này, để request không bị 401 giữa chừng.
const refreshAheadMs = 30_000

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export interface RequestOptions {
  method?: HttpMethod
  body?: unknown
  // TanStack Query truyền signal để hủy request khi màn hình không cần nữa.
  signal?: AbortSignal
  // Endpoint công khai (đăng nhập, refresh, thông tin cửa hàng): không gắn token, không tự làm mới.
  anonymous?: boolean
}

// Gửi request JSON, trả nguyên khuôn HTTPResponseData. Mã ngoài 2xx → ném ApiError mang message của server.
export async function request<T>(path: string, options: RequestOptions = {}): Promise<HTTPResponseData<T>> {
  if (options.anonymous) return requestOnce<T>(path, options, undefined)

  if (isExpiringSoon()) await refreshSession()
  try {
    return await requestOnce<T>(path, options, useAuthStore.getState().session?.accessToken)
  } catch (error) {
    // Token bị từ chối (hết hạn sớm, bị thu hồi): làm mới một lần rồi thử lại đúng một lần.
    if (error instanceof ApiError && error.status === 401 && useAuthStore.getState().session && (await refreshSession())) {
      return requestOnce<T>(path, options, useAuthStore.getState().session?.accessToken)
    }
    throw error
  }
}

// Nhiều request cùng lúc gặp token hết hạn thì dùng chung MỘT lần gọi refresh.
// Server xoay vòng refresh token: gọi refresh hai lần với cùng token cũ sẽ bị coi là đánh cắp và thu hồi mọi phiên.
let refreshing: Promise<boolean> | null = null

function refreshSession(): Promise<boolean> {
  refreshing ??= doRefresh().finally(() => {
    refreshing = null
  })
  return refreshing
}

async function doRefresh(): Promise<boolean> {
  const session = useAuthStore.getState().session
  if (!session) return false
  try {
    // POST /api/auth/refresh: đổi refresh token lấy cặp mới (server xoay vòng, token cũ hết dùng).
    const response = await requestOnce<AuthResponse>(
      '/api/auth/refresh',
      { method: 'POST', body: { refreshToken: session.refreshToken } },
      undefined,
    )
    useAuthStore.getState().replaceTokens(response.dataResponse)
    return true
  } catch (error) {
    // Mất mạng: giữ phiên, lần sau thử lại. Server từ chối (hết hạn, bị thu hồi): đăng xuất.
    if (error instanceof ApiError && !error.isNetworkError) useAuthStore.getState().signOut()
    return false
  }
}

function isExpiringSoon(): boolean {
  const session = useAuthStore.getState().session
  if (!session) return false
  return new Date(session.accessTokenExpiresAt).getTime() - Date.now() < refreshAheadMs
}

// Một lần gửi, có thời gian chờ. Không dùng AbortSignal.timeout / AbortSignal.any:
// điện thoại đời cũ (Safari < 17.4, Chrome < 116) chưa có (ADR-0003 mục 6).
async function requestOnce<T>(path: string, options: RequestOptions, accessToken: string | undefined): Promise<HTTPResponseData<T>> {
  const controller = new AbortController()
  let timedOut = false
  const timer = setTimeout(() => {
    timedOut = true
    controller.abort()
  }, timeoutMs)
  const abortFromCaller = () => controller.abort()
  if (options.signal?.aborted) controller.abort()
  else options.signal?.addEventListener('abort', abortFromCaller, { once: true })

  try {
    return await send<T>(path, options, accessToken, controller.signal, () => timedOut)
  } finally {
    clearTimeout(timer)
    options.signal?.removeEventListener('abort', abortFromCaller)
  }
}

async function send<T>(
  path: string,
  options: RequestOptions,
  accessToken: string | undefined,
  signal: AbortSignal,
  isTimedOut: () => boolean,
): Promise<HTTPResponseData<T>> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`

  let response: Response
  try {
    response = await fetch(baseUrl + path, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal,
    })
  } catch (error) {
    if (options.signal?.aborted) throw error // người gọi tự hủy: để TanStack Query xử lý
    if (isTimedOut()) throw new ApiError(0, 'Máy chủ không phản hồi. Kiểm tra mạng rồi thử lại.')
    throw new ApiError(0, 'Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại.')
  }

  const payload = (await response.json().catch(() => null)) as HTTPResponseData<T> | null

  if (!response.ok) {
    const message = payload?.message ?? `Máy chủ trả lỗi ${response.status}.`
    const fieldErrors = response.status === 400 ? toFieldErrors(payload?.dataResponse) : undefined
    throw new ApiError(response.status, message, fieldErrors)
  }
  if (payload === null) throw new ApiError(response.status, 'Phản hồi của máy chủ không đọc được.')
  return payload
}

// Lỗi 400 của ASP.NET: { "Lines[0].Qty": ["..."] } → { "lines[0].qty": ["..."] } để khớp tên trường của form.
function toFieldErrors(data: unknown): Record<string, string[]> | undefined {
  if (data === null || typeof data !== 'object' || Array.isArray(data)) return undefined
  const result: Record<string, string[]> = {}
  for (const [key, value] of Object.entries(data)) {
    if (!Array.isArray(value)) continue
    const camelKey = key
      .split('.')
      .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
      .join('.')
    result[camelKey] = value.map(String)
  }
  return Object.keys(result).length > 0 ? result : undefined
}
