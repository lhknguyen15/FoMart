import { v7 } from 'uuid'

// Id hóa đơn UUID v7 sinh trên máy bán (NFR-05): tăng dần theo thời gian, gửi lại cùng id thì server không ghi trùng.
export function newSaleId(): string {
  return v7()
}
