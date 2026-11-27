import { request } from './client'

// GET /api/products/barcode/{barcode} (Application/DTO/Catalog/ProductModels.cs: ProductBarcodeResponse).
export interface ProductBarcodeResponse {
  productId: number
  productName: string
  productStatus: 'active' | 'inactive' | 'incomplete'
  trackExpiry: boolean
  unitId: number
  unitName: string
  factor: number
  salePrice: number
}

// Tra mã vạch: trả sản phẩm và đúng đơn vị mang mã đó. 404 = mã chưa có trong danh mục.
export async function getProductByBarcode(barcode: string, signal?: AbortSignal): Promise<ProductBarcodeResponse> {
  const response = await request<ProductBarcodeResponse>(`/api/products/barcode/${encodeURIComponent(barcode)}`, { signal })
  return response.dataResponse
}
