import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import { newSaleId } from '../lib/id'

// Giỏ hàng đang bán trên máy này (UC-05). Lưu localStorage: F5 hay lỡ tay đóng tab không mất giỏ.
// saleId sinh MỘT lần cho mỗi giỏ: gửi lại sau lỗi mạng dùng cùng id, server không ghi trùng hóa đơn (NFR-05).

export interface CartLine {
  // Khóa dòng: "u<unitId>" với hàng đã khai báo, "m<số>" với hàng chưa khai báo.
  key: string
  unitId: number | null
  productName: string
  unitName: string | null
  unitPrice: number
  qty: number
}

export interface DeclaredItem {
  unitId: number
  productName: string
  unitName: string
  unitPrice: number
}

interface CartState {
  saleId: string
  lines: CartLine[]
  // Quét / chọn cùng đơn vị lần nữa thì +1 (UC-05 bước 3); món mới lên đầu giỏ.
  addDeclared: (item: DeclaredItem) => void
  addUndeclared: (name: string, unitPrice: number, qty: number) => void
  setQty: (key: string, qty: number) => void
  remove: (key: string) => void
  // Bán xong hoặc khách đổi ý: giỏ trống với id hóa đơn mới.
  reset: () => void
}

const MAX_QTY = 9999

export const useCartStore = create<CartState>()(
  persist(
    (set) => ({
      saleId: newSaleId(),
      lines: [],
      addDeclared: (item) =>
        set((state) => {
          const key = `u${item.unitId}`
          const existing = state.lines.find((line) => line.key === key)
          if (existing) {
            const updated = { ...existing, qty: Math.min(existing.qty + 1, MAX_QTY) }
            return { lines: [updated, ...state.lines.filter((line) => line.key !== key)] }
          }
          const line: CartLine = { key, unitId: item.unitId, productName: item.productName, unitName: item.unitName, unitPrice: item.unitPrice, qty: 1 }
          return { lines: [line, ...state.lines] }
        }),
      addUndeclared: (name, unitPrice, qty) =>
        set((state) => ({
          lines: [{ key: `m${Date.now()}`, unitId: null, productName: name, unitName: null, unitPrice, qty }, ...state.lines],
        })),
      setQty: (key, qty) =>
        set((state) => ({
          lines: qty <= 0
            ? state.lines.filter((line) => line.key !== key)
            : state.lines.map((line) => (line.key === key ? { ...line, qty: Math.min(qty, MAX_QTY) } : line)),
        })),
      remove: (key) => set((state) => ({ lines: state.lines.filter((line) => line.key !== key) })),
      reset: () => set({ saleId: newSaleId(), lines: [] }),
    }),
    {
      name: 'fomart-cart',
      version: 1,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ saleId: state.saleId, lines: state.lines }),
    },
  ),
)

// Tổng tiền giỏ (đồng).
export function cartTotal(lines: CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.unitPrice * line.qty, 0)
}
