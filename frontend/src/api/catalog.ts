import { request } from './client'
import type { PagedResult } from './types'

// Application/DTO/Catalog/ProductModels.cs

export type ProductStatus = 'active' | 'inactive' | 'incomplete'

// GET /api/products/barcode/{barcode}: kết quả quét mã (ProductBarcodeResponse).
export interface ProductBarcodeResponse {
  productId: number
  productName: string
  productStatus: ProductStatus
  trackExpiry: boolean
  unitId: number
  unitName: string
  factor: number
  salePrice: number
}

// Một dòng trong danh sách tìm sản phẩm (ProductListItemResponse).
export interface ProductListItemResponse {
  id: number
  name: string
  categoryName: string | null
  status: ProductStatus
  baseUnitName: string
  baseSalePrice: number
  baseBarcode: string | null
  totalStock: number
  needsCount: boolean
}

export interface ProductUnitResponse {
  id: number
  name: string
  factor: number
  barcode: string | null
  salePrice: number
  isBase: boolean
  isDefaultPurchase: boolean
  isActive: boolean
  hasTransactions: boolean
}

export interface ProductStockResponse {
  locationId: number
  locationName: string
  qty: number
}

// Chi tiết sản phẩm (ProductResponse).
export interface ProductResponse {
  id: number
  name: string
  categoryId: number | null
  categoryName: string | null
  defaultSupplierId: number | null
  defaultSupplierName: string | null
  trackExpiry: boolean
  minStock: number
  minShelfStock: number
  status: ProductStatus
  needsCount: boolean
  lastCountedAt: string | null
  quickButtonOrder: number | null
  createdAt: string
  updatedAt: string | null
  units: ProductUnitResponse[]
  stocks: ProductStockResponse[]
}

export interface ProductSearchParams {
  keyword?: string
  status?: ProductStatus
  page?: number
  pageSize?: number
}

// Tra mã vạch: trả sản phẩm và đúng đơn vị mang mã đó. 404 = mã chưa có trong danh mục.
export async function getProductByBarcode(barcode: string, signal?: AbortSignal): Promise<ProductBarcodeResponse> {
  const response = await request<ProductBarcodeResponse>(`/api/products/barcode/${encodeURIComponent(barcode)}`, { signal })
  return response.dataResponse
}

// Tìm sản phẩm theo tên (không dấu) hoặc mã vạch, có phân trang.
export async function searchProducts(params: ProductSearchParams, signal?: AbortSignal): Promise<PagedResult<ProductListItemResponse>> {
  const query = new URLSearchParams()
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.status) query.set('status', params.status)
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const response = await request<PagedResult<ProductListItemResponse>>(`/api/products?${query.toString()}`, { signal })
  return response.dataResponse
}

// Chi tiết sản phẩm: các đơn vị (giá, mã vạch) và tồn theo vị trí.
export async function getProduct(id: number, signal?: AbortSignal): Promise<ProductResponse> {
  const response = await request<ProductResponse>(`/api/products/${id}`, { signal })
  return response.dataResponse
}
