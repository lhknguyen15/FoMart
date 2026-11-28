import { request } from './client'
import type { PagedResult } from './types'

// Application/DTO/Purchasing/GoodsReceiptModels.cs

export type GoodsReceiptStatus = 'draft' | 'completed' | 'cancelled'
export type ExpiryWarning = 'near_expiry' | 'expired'

// Một dòng trong danh sách phiếu nhập. total = null khi người xem bị ẩn giá nhập (BR-08).
export interface GoodsReceiptListItemResponse {
  id: number
  code: string
  supplierName: string | null
  locationName: string
  status: GoodsReceiptStatus
  total: number | null
  lineCount: number
  createdByName: string
  createdAt: string
  completedAt: string | null
}

// Một dòng phiếu nhập. baseUnitCost = giá vốn mỗi đơn vị cơ sở (BR-11). Ngày hạn dùng dạng "yyyy-mm-dd".
export interface ReceiptLineResponse {
  id: number
  lineNo: number
  productId: number
  productName: string
  trackExpiry: boolean
  unitId: number
  unitName: string
  factor: number
  qty: number
  baseQty: number
  unitPrice: number | null
  lineTotal: number | null
  baseUnitCost: number | null
  expiryDate: string | null
  lotNo: string | null
  lotId: number | null
  expiryWarning: ExpiryWarning | null
}

// Chi tiết phiếu nhập (SC-10). pricesHidden = true thì mọi giá/tiền là null (BR-08).
export interface GoodsReceiptResponse {
  id: number
  code: string
  supplierId: number | null
  supplierName: string | null
  locationId: number
  locationName: string
  status: GoodsReceiptStatus
  total: number | null
  note: string | null
  createdBy: number
  createdByName: string
  createdAt: string
  completedByName: string | null
  completedAt: string | null
  cancelledByName: string | null
  cancelledAt: string | null
  cancelReason: string | null
  pricesHidden: boolean
  missingPriceLines: number
  lines: ReceiptLineResponse[]
}

export interface LastPurchasePriceResponse {
  unitId: number
  unitPrice: number
  receivedAt: string
  receiptCode: string
  supplierName: string | null
}

export interface GoodsReceiptSearchParams {
  status?: GoodsReceiptStatus
  page?: number
  pageSize?: number
}

// unitPrice null = chưa có giá (nhân viên bị ẩn giá); expiryDate bắt buộc nếu sản phẩm quản lý hạn dùng.
export interface ReceiptLineRequest {
  unitId: number
  qty: number
  unitPrice?: number | null
  expiryDate?: string | null
  lotNo?: string | null
}

export interface CreateGoodsReceiptRequest {
  supplierId?: number | null
  locationId?: number | null
  note?: string | null
  lines?: ReceiptLineRequest[]
}

export interface UpdateGoodsReceiptRequest {
  supplierId: number | null
  locationId: number
  note: string | null
}

const base = '/api/goods-receipts'

export async function searchGoodsReceipts(params: GoodsReceiptSearchParams, signal?: AbortSignal): Promise<PagedResult<GoodsReceiptListItemResponse>> {
  const query = new URLSearchParams()
  if (params.status) query.set('status', params.status)
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const response = await request<PagedResult<GoodsReceiptListItemResponse>>(`${base}?${query.toString()}`, { signal })
  return response.dataResponse
}

export async function getGoodsReceipt(id: number, signal?: AbortSignal): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}`, { signal })
  return response.dataResponse
}

// Giá nhập lần trước của một đơn vị (UC-04 bước 3). 404 = chưa nhập lần nào; 403 = nhân viên bị ẩn giá.
export async function getLastPurchasePrice(unitId: number, signal?: AbortSignal): Promise<LastPurchasePriceResponse> {
  const response = await request<LastPurchasePriceResponse>(`${base}/last-price?unitId=${unitId}`, { signal })
  return response.dataResponse
}

export async function createGoodsReceipt(body: CreateGoodsReceiptRequest): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(base, { method: 'POST', body })
  return response.dataResponse
}

export async function updateGoodsReceipt(id: number, body: UpdateGoodsReceiptRequest): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}`, { method: 'PUT', body })
  return response.dataResponse
}

// Xóa phiếu nháp (chưa đổi tồn kho).
export async function deleteGoodsReceipt(id: number): Promise<void> {
  await request<GoodsReceiptResponse>(`${base}/${id}`, { method: 'DELETE' })
}

// Các thao tác trên dòng đều trả lại cả phiếu (tổng tiền, cảnh báo đã tính lại).
export async function addReceiptLine(id: number, body: ReceiptLineRequest): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}/lines`, { method: 'POST', body })
  return response.dataResponse
}

export async function updateReceiptLine(id: number, lineId: number, body: ReceiptLineRequest): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}/lines/${lineId}`, { method: 'PUT', body })
  return response.dataResponse
}

export async function deleteReceiptLine(id: number, lineId: number): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}/lines/${lineId}`, { method: 'DELETE' })
  return response.dataResponse
}

// Hoàn tất: tạo lô, ghi sổ kho. 409 khi còn cảnh báo hạn dùng chưa xác nhận hoặc còn dòng thiếu giá.
export async function completeGoodsReceipt(id: number, confirmExpiryWarning: boolean): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}/complete`, { method: 'POST', body: { confirmExpiryWarning } })
  return response.dataResponse
}

// Chủ tiệm hủy phiếu đã hoàn tất bằng chứng từ đảo ngược (BR-03).
export async function cancelGoodsReceipt(id: number, reason: string): Promise<GoodsReceiptResponse> {
  const response = await request<GoodsReceiptResponse>(`${base}/${id}/cancel`, { method: 'POST', body: { reason } })
  return response.dataResponse
}
