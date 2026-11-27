import { ApiError, type HTTPResponseData } from './types'

// Nơi DUY NHẤT trong frontend gọi fetch. Các file api/<mảng>.ts gọi request(), component không gọi trực tiếp.

const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? ''
const timeoutMs = 15_000

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export interface RequestOptions {
  method?: HttpMethod
  body?: unknown
  // TanStack Query truyền signal để hủy request khi màn hình không cần nữa.
  signal?: AbortSignal
  // Tạm thời truyền tay cho đến khi có useAuthStore (bước đăng nhập sẽ gắn token tự động ở đây).
  accessToken?: string
}

// Gửi request JSON, trả nguyên khuôn HTTPResponseData. Mã ngoài 2xx → ném ApiError mang message của server.
export async function request<T>(path: string, options: RequestOptions = {}): Promise<HTTPResponseData<T>> {
  const timeout = AbortSignal.timeout(timeoutMs)
  const signal = options.signal ? AbortSignal.any([options.signal, timeout]) : timeout

  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (options.accessToken) headers.Authorization = `Bearer ${options.accessToken}`

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
    if (timeout.aborted) throw new ApiError(0, 'Máy chủ không phản hồi. Kiểm tra mạng rồi thử lại.')
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
