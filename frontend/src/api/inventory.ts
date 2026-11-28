import { request } from './client'
import type { PagedResult } from './types'

// Application/DTO/Inventory/StockModels.cs

export type DocType = 'receipt' | 'receipt_cancel' | 'sale' | 'sale_cancel' | 'transfer_out' | 'transfer_in' | 'disposal' | 'count_adjust'
export type ExpiryWarning = 'near_expiry' | 'expired'

// Tồn của một lô tại một vị trí (đơn vị cơ sở). unitCost = null khi người xem bị ẩn giá vốn (BR-08) hoặc lô tạm.
export interface StockLotResponse {
  productId: number
  productName: string
  baseUnitName: string
  lotId: number
  lotKind: 'receipt' | 'provisional'
  lotNo: string | null
  expiryDate: string | null
  receivedAt: string
  unitCost: number | null
  locationId: number
  locationName: string
  qty: number
  expiryWarning: ExpiryWarning | null
}

// Một dòng thẻ kho (mới nhất trước). balance = tồn sau biến động này.
export interface StockMoveResponse {
  id: number
  createdAt: string
  docType: DocType
  docCode: string
  lotId: number
  lotNo: string | null
  expiryDate: string | null
  locationId: number
  locationName: string
  qtyChange: number
  balance: number
  unitCost: number | null
  userName: string
  saleId: string | null
  receiptId: number | null
}

// GET /api/stock?productId=: tồn theo lô và vị trí của một sản phẩm (bỏ lô đã hết).
export async function getProductStock(productId: number, signal?: AbortSignal): Promise<StockLotResponse[]> {
  const response = await request<PagedResult<StockLotResponse>>(`/api/stock?productId=${productId}&pageSize=200`, { signal })
  return response.dataResponse.items
}

// GET /api/stock/moves: thẻ kho một sản phẩm, có phân trang.
export async function getStockMoves(productId: number, page: number, signal?: AbortSignal): Promise<PagedResult<StockMoveResponse>> {
  const response = await request<PagedResult<StockMoveResponse>>(`/api/stock/moves?productId=${productId}&page=${page}&pageSize=30`, { signal })
  return response.dataResponse
}

export type StockExpiryFilter = 'warning' | 'expired' | 'near_expiry'

// GET /api/stock?expiry=: lô còn tồn đã hết hạn / sắp hết hạn trên toàn tiệm, hạn sớm nhất đứng đầu (UC-08).
export async function getExpiringLots(expiry: StockExpiryFilter, page: number, signal?: AbortSignal): Promise<PagedResult<StockLotResponse>> {
  const response = await request<PagedResult<StockLotResponse>>(`/api/stock?expiry=${expiry}&page=${page}&pageSize=50`, { signal })
  return response.dataResponse
}

// Tổng hợp hạn dùng. *Value = null khi người xem bị ẩn giá vốn (BR-08).
export interface StockExpirySummaryResponse {
  warnDays: number
  expiredLots: number
  expiredQty: number
  expiredValue: number | null
  nearLots: number
  nearQty: number
  nearValue: number | null
}

export async function getExpirySummary(signal?: AbortSignal): Promise<StockExpirySummaryResponse> {
  const response = await request<StockExpirySummaryResponse>('/api/stock/expiry-summary', { signal })
  return response.dataResponse
}

// ---- Xuất hủy (Application/DTO/Inventory/DisposalModels.cs, UC-08) ----

export type DisposalStatus = 'pending' | 'approved' | 'rejected'
export type DisposalReason = 'expired' | 'broken' | 'damaged' | 'other'

export interface DisposalListItemResponse {
  id: number
  code: string
  status: DisposalStatus
  lineCount: number
  totalBaseQty: number
  lossValue: number | null // tổn thất theo giá vốn (đồng); null khi chưa duyệt hoặc bị ẩn giá vốn (BR-08)
  createdByName: string
  createdAt: string
  decidedByName: string | null
  decidedAt: string | null
}

export interface DisposalLotResponse {
  lotId: number
  lotNo: string | null
  expiryDate: string | null
  qty: number
  unitCost: number | null
}

export interface DisposalLineResponse {
  id: number
  productId: number
  productName: string
  baseUnitName: string
  locationId: number
  locationName: string
  lotId: number | null // null = hệ thống chọn lô hạn sớm nhất khi duyệt
  lotNo: string | null
  expiryDate: string | null
  baseQty: number
  reason: DisposalReason
  note: string | null
  lossValue: number | null
  lots: DisposalLotResponse[] // lô đã trừ thật (sau khi duyệt)
}

export interface DisposalResponse {
  id: number
  code: string
  status: DisposalStatus
  note: string | null
  createdBy: number
  createdByName: string
  createdAt: string
  decidedByName: string | null
  decidedAt: string | null
  rejectReason: string | null
  costHidden: boolean
  lossValue: number | null
  lines: DisposalLineResponse[]
}

export interface DisposalLineRequest {
  productId: number
  locationId: number
  lotId: number | null
  baseQty: number
  reason: DisposalReason
  note: string | null
}

export interface CreateDisposalRequest {
  note: string | null
  lines: DisposalLineRequest[]
}

export async function searchDisposals(status: DisposalStatus | undefined, page: number, signal?: AbortSignal): Promise<PagedResult<DisposalListItemResponse>> {
  const query = new URLSearchParams({ page: String(page), pageSize: '20' })
  if (status) query.set('status', status)
  const response = await request<PagedResult<DisposalListItemResponse>>(`/api/disposals?${query.toString()}`, { signal })
  return response.dataResponse
}

export async function getDisposal(id: number, signal?: AbortSignal): Promise<DisposalResponse> {
  const response = await request<DisposalResponse>(`/api/disposals/${id}`, { signal })
  return response.dataResponse
}

// Chủ tiệm lập: trừ tồn ngay (BR-15). Nhân viên lập: chờ duyệt (BR-21). 409 khi tồn không đủ.
export async function createDisposal(body: CreateDisposalRequest): Promise<{ disposal: DisposalResponse; message: string }> {
  const response = await request<DisposalResponse>('/api/disposals', { method: 'POST', body })
  return { disposal: response.dataResponse, message: response.message }
}

export async function approveDisposal(id: number): Promise<DisposalResponse> {
  const response = await request<DisposalResponse>(`/api/disposals/${id}/approve`, { method: 'POST' })
  return response.dataResponse
}

export async function rejectDisposal(id: number, reason: string): Promise<DisposalResponse> {
  const response = await request<DisposalResponse>(`/api/disposals/${id}/reject`, { method: 'POST', body: { reason } })
  return response.dataResponse
}
