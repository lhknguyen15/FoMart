import { request } from './client'

// GET /api/stores (Application/DTO/Store/StoreModels.cs). Công khai: màn hình đăng nhập cũng dùng.
export interface StoreResponse {
  name: string
  address: string | null
  phone: string | null
  hasBankAccount: boolean
}

export async function getStore(signal?: AbortSignal): Promise<StoreResponse> {
  const response = await request<StoreResponse>('/api/stores', { signal, anonymous: true })
  return response.dataResponse
}
