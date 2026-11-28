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

export interface CreateProductUnitRequest {
  name: string
  factor: number
  barcode?: string | null
  salePrice: number
  isBase: boolean
  isDefaultPurchase: boolean
}

// POST /api/products (chủ tiệm). Tạo nhanh khi quét mã mới: một đơn vị cơ sở, hệ số 1 (UC-01 nhánh tạo nhanh).
export interface CreateProductRequest {
  name: string
  trackExpiry: boolean
  defaultSupplierId?: number | null
  units: CreateProductUnitRequest[]
  confirmPriceWarning?: boolean
}

export async function createProduct(body: CreateProductRequest): Promise<ProductResponse> {
  const response = await request<ProductResponse>('/api/products', { method: 'POST', body })
  return response.dataResponse
}

// Application/DTO/Catalog/SupplierModels.cs
export interface SupplierResponse {
  id: number
  name: string
  phone: string | null
  zalo: string | null
  note: string | null
  isActive: boolean
  createdAt: string
}

// Nhà cung cấp đang hợp tác (tìm theo tên không dấu hoặc số điện thoại; bỏ trống = tất cả).
export async function searchSuppliers(keyword?: string, signal?: AbortSignal): Promise<SupplierResponse[]> {
  const query = new URLSearchParams()
  if (keyword) query.set('keyword', keyword)
  const response = await request<SupplierResponse[]>(`/api/suppliers?${query.toString()}`, { signal })
  return response.dataResponse
}

// Application/DTO/Catalog/LocationModels.cs. type: storage = kho, shelf = kệ bán.
export interface LocationResponse {
  id: number
  name: string
  type: 'storage' | 'shelf'
  isDefaultSale: boolean
  isActive: boolean
  stockQty: number
}

// Các vị trí đang dùng, vị trí bán mặc định đứng đầu (UC-02).
export async function getLocations(signal?: AbortSignal): Promise<LocationResponse[]> {
  const response = await request<LocationResponse[]>('/api/locations', { signal })
  return response.dataResponse
}
