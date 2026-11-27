// Khuôn phản hồi chung của FoMart.Api (Application/DTO/HTTPResponseData.cs), kể cả khi lỗi.
export interface HTTPResponseData<T> {
  dataResponse: T
  message: string
  statusCode: number
  timestamp: string
}

// Khuôn phân trang (Application/DTO/PagedResult.cs).
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Lỗi từ API hoặc mạng. status = 0: không tới được máy chủ (mất mạng, hết thời gian chờ).
// fieldErrors: lỗi kiểm tra dữ liệu (400) theo từng trường, tên trường đã đổi sang camelCase.
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]> | undefined

  constructor(status: number, message: string, fieldErrors?: Record<string, string[]>) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }

  // Không tới được máy chủ: nên thử lại / giữ dữ liệu trên máy.
  get isNetworkError(): boolean {
    return this.status === 0
  }
}
