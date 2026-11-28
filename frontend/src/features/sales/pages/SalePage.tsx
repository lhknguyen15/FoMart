import { CheckCircleIcon, WarningIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import type { PaymentMethod, SaleResponse } from '../../../api/sales'
import { ApiError } from '../../../api/types'
import { ScanPanel } from '../../../components/ui/ScanPanel'
import { scanFeedback, unlockAudio } from '../../../lib/feedback'
import { formatMoney } from '../../../lib/format'
import { cartTotal, useCartStore } from '../../../stores/useCartStore'
import { ProductPicker } from '../../catalog/components/ProductPicker'
import { useBarcodeLookup } from '../../catalog/hooks/useProducts'
import { CartList } from '../components/CartList'
import { CheckoutBar } from '../components/CheckoutBar'
import { PendingSales } from '../components/PendingSales'
import { UndeclaredItemForm } from '../components/UndeclaredItemForm'
import { useCreateSale } from '../hooks/useSales'

// Kết quả lần quét / chọn gần nhất, hiện ngay dưới camera (DESIGN.md: Scan Result Card).
type Notice =
  | { kind: 'added'; name: string; unit: string; price: number }
  | { kind: 'missing'; code: string }
  | { kind: 'blocked'; message: string }

// Màn hình bán hàng (UC-05, SC-03): quét / tìm → giỏ → tiền mặt hoặc chuyển khoản.
// Bốn nguồn mã đi chung một hàm handleCode: camera, máy quét bàn phím, gõ mã trong ô tìm, chụp ảnh (ADR-0003 mục 5).
export function SalePage() {
  const lines = useCartStore((state) => state.lines)
  const saleId = useCartStore((state) => state.saleId)
  const addDeclared = useCartStore((state) => state.addDeclared)
  const addUndeclared = useCartStore((state) => state.addUndeclared)
  const setQty = useCartStore((state) => state.setQty)
  const resetCart = useCartStore((state) => state.reset)
  const lookup = useBarcodeLookup()
  const createSale = useCreateSale()

  const [notice, setNotice] = useState<Notice | null>(null)
  const [undeclaredFor, setUndeclaredFor] = useState<{ code?: string } | null>(null)
  const [outcome, setOutcome] = useState<SaleResponse | null>(null)
  const [checkoutError, setCheckoutError] = useState<string | null>(null)
  // Chỉ còn lô hết hạn (409, BR-05): người bán xác nhận đã kiểm hàng trên tay rồi gửi lại cùng id hóa đơn.
  const [expiredPrompt, setExpiredPrompt] = useState<{ method: PaymentMethod; cashGiven?: number; message: string } | null>(null)

  const total = cartTotal(lines)
  const itemCount = lines.reduce((sum, line) => sum + line.qty, 0)

  // Một mã vừa nhận được, từ bất kỳ nguồn nào: tra mã → thêm vào giỏ (UC-05 bước 2-3).
  const handleCode = async (raw: string) => {
    setOutcome(null)
    try {
      const item = await lookup(raw)
      if (item.productStatus === 'inactive') {
        scanFeedback('warn')
        setNotice({ kind: 'blocked', message: `${item.productName} đã ngừng kinh doanh.` })
        return
      }
      addDeclared({ unitId: item.unitId, productName: item.productName, unitName: item.unitName, unitPrice: item.salePrice })
      scanFeedback('ok')
      setNotice({ kind: 'added', name: item.productName, unit: item.unitName, price: item.salePrice })
    } catch (error) {
      scanFeedback('warn')
      if (error instanceof ApiError && error.status === 404) setNotice({ kind: 'missing', code: raw.trim() })
      else setNotice({ kind: 'blocked', message: error instanceof ApiError ? error.message : 'Không tra được mã.' })
    }
  }

  const checkout = (method: PaymentMethod, cashGiven?: number, confirmExpired = false) => {
    unlockAudio()
    setCheckoutError(null)
    createSale.mutate(
      {
        id: saleId,
        paymentMethod: method,
        cashGiven: method === 'cash' ? cashGiven : undefined,
        lines: lines.map((line) =>
          line.unitId !== null
            ? { unitId: line.unitId, qty: line.qty, confirmExpired }
            : { name: line.productName, unitPrice: line.unitPrice, qty: line.qty },
        ),
      },
      {
        onSuccess: ({ sale }) => {
          resetCart()
          setExpiredPrompt(null)
          setNotice(null)
          setOutcome(sale)
          scanFeedback('ok')
        },
        onError: (error) => {
          if (error instanceof ApiError && error.status === 409 && error.message.includes('hết hạn')) {
            setExpiredPrompt({ method, cashGiven, message: error.message })
          } else {
            // Giỏ và id hóa đơn giữ nguyên: bấm lại an toàn, server không ghi trùng (NFR-05).
            setCheckoutError(error instanceof ApiError ? error.message : 'Không lưu được hóa đơn. Thử lại.')
          }
        },
      },
    )
  }

  return (
    <div onPointerDown={unlockAudio} className="grid gap-4 pb-[calc(13.5rem+env(safe-area-inset-bottom))] lg:grid-cols-[minmax(0,1fr)_24rem] lg:pb-4">
      <h1 className="sr-only">Bán hàng</h1>

      <div className="flex min-w-0 flex-col gap-4">
        <ScanPanel onCode={(code) => void handleCode(code)} />

        <div aria-live="polite">
          {notice?.kind === 'added' && (
            <div className="flex items-center gap-3 rounded-xl border-2 border-ok bg-ok/10 px-4 py-3">
              <CheckCircleIcon size="1.75rem" weight="fill" className="shrink-0 text-ok" aria-hidden="true" />
              <div className="min-w-0">
                <p className="truncate text-lg font-semibold">{notice.name}</p>
                <p className="tabular-nums">
                  {notice.unit} · <span className="font-semibold">{formatMoney(notice.price)}</span>
                </p>
              </div>
            </div>
          )}
          {notice?.kind === 'missing' && (
            <div className="flex flex-col gap-2 rounded-xl border-2 border-warn bg-warn/10 px-4 py-3">
              <p className="font-semibold text-warn">
                Mã <span className="tabular-nums">{notice.code}</span> chưa có trong danh mục.
              </p>
              <button
                type="button"
                onClick={() => setUndeclaredFor({ code: notice.code })}
                className="min-h-11 self-start rounded-lg border border-warn bg-surface px-3 text-sm font-semibold"
              >
                Bán như hàng chưa khai báo
              </button>
            </div>
          )}
          {notice?.kind === 'blocked' && (
            <p className="flex items-center gap-2 rounded-xl border-2 border-bad bg-bad/10 px-4 py-3 font-semibold text-bad">
              <WarningIcon size="1.5rem" weight="bold" className="shrink-0" aria-hidden="true" />
              {notice.message}
            </p>
          )}
        </div>

        {undeclaredFor && (
          <UndeclaredItemForm
            scannedCode={undeclaredFor.code}
            onCancel={() => setUndeclaredFor(null)}
            onAdd={(values) => {
              addUndeclared(values.name, values.unitPrice, values.qty)
              setUndeclaredFor(null)
              setNotice(null)
            }}
          />
        )}

        <ProductPicker
          inputId="sale-search"
          onBarcode={(code) => void handleCode(code)}
          describeItem={(item) => `${item.baseUnitName} · ${formatMoney(item.baseSalePrice)} · tồn ${item.totalStock}`}
          describeUnit={(unit) => formatMoney(unit.salePrice)}
          onPick={(product, unit) => {
            addDeclared({ unitId: unit.id, productName: product.name, unitName: unit.name, unitPrice: unit.salePrice })
            setNotice({ kind: 'added', name: product.name, unit: unit.name, price: unit.salePrice })
          }}
        />
        {!undeclaredFor && (
          <button type="button" onClick={() => setUndeclaredFor({})} className="min-h-11 self-start text-sm font-semibold text-brand-700 underline-offset-4 hover:underline">
            Món không có mã, chưa khai báo?
          </button>
        )}
      </div>

      <div className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-20 lg:self-start">
        {outcome && (
          <div role="status" className={`flex flex-col gap-1 rounded-xl border-2 px-4 py-3 ${outcome.status === 'paid' ? 'border-ok bg-ok/10' : 'border-warn bg-warn/10'}`}>
            <p className="font-semibold">
              {outcome.status === 'paid' ? 'Đã bán' : 'Chờ chuyển khoản'} <span className="tabular-nums">{outcome.code}</span> ·{' '}
              <span className="tabular-nums">{formatMoney(outcome.total)}</span>
            </p>
            {outcome.status === 'pending_payment' && <p className="text-sm">Khách quét mã QR bên dưới. Thấy tiền về thì bấm “Đã nhận tiền”.</p>}
            {outcome.change !== null && outcome.change > 0 && (
              <p className="text-lg font-bold tabular-nums">Thối {formatMoney(outcome.change)}</p>
            )}
            {outcome.warnings.map((warning) => (
              <p key={warning} className="text-sm font-semibold text-warn">
                {warning}
              </p>
            ))}
          </div>
        )}

        <CartList lines={lines} onQty={setQty} disabled={createSale.isPending} />
        <PendingSales focusId={outcome?.status === 'pending_payment' ? outcome.id : undefined} />

        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-line bg-surface px-[max(1rem,env(safe-area-inset-left))] pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] lg:static lg:rounded-xl lg:border lg:p-4">
          <div className="mx-auto flex max-w-3xl flex-col gap-2 lg:max-w-none">
            {expiredPrompt && (
              <div role="alert" className="flex flex-col gap-2 rounded-lg border border-warn/50 bg-warn/10 p-2">
                <p className="text-sm font-semibold text-warn">{expiredPrompt.message}</p>
                <div className="grid grid-cols-[1fr_auto] gap-2">
                  <button
                    type="button"
                    onClick={() => checkout(expiredPrompt.method, expiredPrompt.cashGiven, true)}
                    className="min-h-11 rounded-lg bg-warn px-3 font-semibold text-white"
                  >
                    Đã kiểm hàng, vẫn bán
                  </button>
                  <button type="button" onClick={() => setExpiredPrompt(null)} className="min-h-11 rounded-lg border border-line bg-surface px-3 font-semibold">
                    Thôi
                  </button>
                </div>
              </div>
            )}
            {checkoutError && (
              <p role="alert" className="text-sm font-semibold text-bad">
                {checkoutError}
              </p>
            )}
            <CheckoutBar
              total={total}
              itemCount={itemCount}
              busy={createSale.isPending}
              onCash={(cashGiven) => checkout('cash', cashGiven)}
              onTransfer={() => checkout('transfer')}
            />
          </div>
        </div>
      </div>
    </div>
  )
}
