import { request } from './client'

// GET /api/stores (Application/DTO/Store/StoreModels.cs). Công khai: màn hình đăng nhập cũng dùng.
export interface StoreResponse {
  name: string
  address: string | null
  phone: string | null
  hasBankAccount: boolean
}

// GET /api/stores/payment: tài khoản nhận chuyển khoản để vẽ mã VietQR. Cần đăng nhập; 404 khi tiệm chưa nhập tài khoản.
export interface StorePaymentResponse {
  bankBin: string
  accountNo: string
  accountName: string
}

export async function getStorePayment(signal?: AbortSignal): Promise<StorePaymentResponse> {
  const response = await request<StorePaymentResponse>('/api/stores/payment', { signal })
  return response.dataResponse
}

export async function getStore(signal?: AbortSignal): Promise<StoreResponse> {
  const response = await request<StoreResponse>('/api/stores', { signal, anonymous: true })
  return response.dataResponse
}
