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
