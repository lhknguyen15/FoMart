import { request } from './client'
import type { PagedResult } from './types'

// Application/DTO/Sales/SaleModels.cs

export type SaleStatus = 'pending_payment' | 'paid' | 'cancelled'
export type PaymentMethod = 'cash' | 'transfer'

// Một dòng giỏ gửi lên. Hàng đã khai báo: unitId (giá lấy theo đơn vị). Hàng chưa khai báo: name + unitPrice.
export interface SaleLineRequest {
  unitId?: number
  qty: number
  name?: string
  unitPrice?: number
  confirmExpired?: boolean
}

export interface CreateSaleRequest {
  id: string // UUID v7 sinh trên máy; gửi lại cùng id thì server không ghi trùng
  paymentMethod: PaymentMethod
  cashGiven?: number
  createdAt?: string
  lines: SaleLineRequest[]
}

export interface SaleLotResponse {
  lotId: number
  lotKind: string
  lotNo: string | null
  expiryDate: string | null
  qty: number
  unitCost: number | null
}

export interface SaleLineResponse {
  id: number
  lineNo: number
  productId: number | null
  unitId: number | null
  name: string
  qty: number
  unitPrice: number
  lineTotal: number
  baseQty: number | null
  costTotal: number | null
  expiredConfirmed: boolean
  isUndeclared: boolean
  lots: SaleLotResponse[]
}

export interface SaleResponse {
  id: string
  code: string
  status: SaleStatus
  paymentMethod: PaymentMethod
  locationId: number
  locationName: string
  total: number
  cashGiven: number | null
  change: number | null
  createdBy: number
  createdByName: string
  createdAt: string
  paidAt: string | null
  syncedAt: string
  cancelledByName: string | null
  cancelledAt: string | null
  cancelReason: string | null
  costHidden: boolean
  costTotal: number | null
  grossProfit: number | null
  lines: SaleLineResponse[]
  // Chỉ có khi vừa bán / vừa xác nhận: tồn không đủ, lô tạm, bán lô hết hạn.
  warnings: string[]
}

export interface SaleListItemResponse {
  id: string
  code: string
  status: SaleStatus
  paymentMethod: PaymentMethod
  total: number
  lineCount: number
  createdByName: string
  createdAt: string
  paidAt: string | null
}

// from / to: ngày bán theo giờ Việt Nam, dạng "yyyy-mm-dd" (gồm cả ngày to).
export interface SaleSearchParams {
  keyword?: string
  status?: SaleStatus
  paymentMethod?: PaymentMethod
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

// POST /api/sales: tiền mặt → thanh toán và trừ kho ngay; chuyển khoản → chờ "Đã nhận".
export async function createSale(body: CreateSaleRequest): Promise<{ sale: SaleResponse; message: string }> {
  const response = await request<SaleResponse>('/api/sales', { method: 'POST', body })
  return { sale: response.dataResponse, message: response.message }
}

// POST /api/sales/{id}/confirm-payment: người bán bấm "Đã nhận" tiền chuyển khoản.
export async function confirmSalePayment(id: string, confirmExpired = false): Promise<SaleResponse> {
  const response = await request<SaleResponse>(`/api/sales/${id}/confirm-payment`, { method: 'POST', body: { confirmExpired } })
  return response.dataResponse
}

// POST /api/sales/{id}/cancel: bỏ hóa đơn chờ thanh toán (hoặc chủ tiệm hủy hóa đơn đã thanh toán).
export async function cancelSale(id: string, reason: string): Promise<SaleResponse> {
  const response = await request<SaleResponse>(`/api/sales/${id}/cancel`, { method: 'POST', body: { reason } })
  return response.dataResponse
}

// GET /api/sales: danh sách hóa đơn (ở màn hình bán hàng: các hóa đơn chờ chuyển khoản).
export async function searchSales(params: SaleSearchParams, signal?: AbortSignal): Promise<PagedResult<SaleListItemResponse>> {
  const query = new URLSearchParams()
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.status) query.set('status', params.status)
  if (params.paymentMethod) query.set('paymentMethod', params.paymentMethod)
  if (params.from) query.set('from', params.from)
  if (params.to) query.set('to', params.to)
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const response = await request<PagedResult<SaleListItemResponse>>(`/api/sales?${query.toString()}`, { signal })
  return response.dataResponse
}

// GET /api/sales/{id}: chi tiết hóa đơn kèm các lô đã trừ. Nhân viên không thấy giá vốn, lãi (costHidden, BR-08).
export async function getSale(id: string, signal?: AbortSignal): Promise<SaleResponse> {
  const response = await request<SaleResponse>(`/api/sales/${id}`, { signal })
  return response.dataResponse
}
