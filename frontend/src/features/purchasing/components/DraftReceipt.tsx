import { CheckCircleIcon, WarningIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import type { ProductResponse } from '../../../api/catalog'
import type { GoodsReceiptResponse, ReceiptLineRequest, ReceiptLineResponse } from '../../../api/purchasing'
import { ApiError } from '../../../api/types'
import { ScanPanel } from '../../../components/ui/ScanPanel'
import { normalizeBarcode } from '../../../lib/barcode'
import { scanFeedback } from '../../../lib/feedback'
import { formatMoney } from '../../../lib/format'
import { useAuthStore } from '../../../stores/useAuthStore'
import { ProductPicker } from '../../catalog/components/ProductPicker'
import { QuickProductForm } from '../../catalog/components/QuickProductForm'
import { useBarcodeLookup, useProductLoader } from '../../catalog/hooks/useProducts'
import { useLocations } from '../../catalog/hooks/useLocations'
import { useSuppliers } from '../../catalog/hooks/useSuppliers'
import {
  useAddReceiptLine,
  useCompleteReceipt,
  useDeleteReceipt,
  useDeleteReceiptLine,
  useUpdateReceipt,
  useUpdateReceiptLine,
} from '../hooks/useGoodsReceipts'
import type { ReceiptLineValues } from '../schemas/receiptLineSchema'
import { ReceiptLineForm, type ReceivingProduct } from './ReceiptLineForm'
import { ReceiptLineList } from './ReceiptLineList'

// Dòng đang nhập / đang sửa. key đổi mỗi lần mở để form tạo lại với giá trị mới.
interface Editing {
  key: number
  code?: string // mã vừa quét: camera thấy lại đúng mã này thì bỏ qua, không mở lại form
  lineId?: number
  product: ReceivingProduct
  initial: Partial<ReceiptLineValues> & { unitId: number }
}

type Notice = { kind: 'ok' | 'bad'; text: string }

// Thông báo 409 của backend có phần hướng dẫn cho lập trình viên ("Gửi lại với ..."): chỉ hiện phần đầu.
const forPeople = (message: string) => message.split(' Gửi lại với')[0] ?? message

const errorText = (caught: unknown, fallback: string) => (caught instanceof ApiError ? caught.message : fallback)

const selectClass = 'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40'

// Chọn đơn vị khi nhập: đơn vị nhập mặc định (thường là thùng), không có thì đơn vị cơ sở.
const purchaseUnit = (units: ProductResponse['units']) => units.find((unit) => unit.isDefaultPurchase) ?? units.find((unit) => unit.isBase) ?? units[0]

// Phiếu nhập đang nháp (UC-04 bước 1-5): chọn nhà cung cấp, quét / tìm hàng, nhập từng dòng, hoàn tất.
export function DraftReceipt({ receipt }: { receipt: GoodsReceiptResponse }) {
  const role = useAuthStore((state) => state.session?.user.role)
  const canSeeCost = !receipt.pricesHidden
  const navigate = useNavigate()
  const suppliers = useSuppliers()
  const locations = useLocations()
  const lookup = useBarcodeLookup()
  const loadProduct = useProductLoader()
  const updateHeader = useUpdateReceipt(receipt.id)
  const addLine = useAddReceiptLine(receipt.id)
  const updateLine = useUpdateReceiptLine(receipt.id)
  const deleteLine = useDeleteReceiptLine(receipt.id)
  const complete = useCompleteReceipt(receipt.id)
  const deleteReceipt = useDeleteReceipt(receipt.id)

  const [editing, setEditing] = useState<Editing | null>(null)
  const [unknownCode, setUnknownCode] = useState<string | null>(null)
  const [notice, setNotice] = useState<Notice | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [expiryPrompt, setExpiryPrompt] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)

  const open = (product: ProductResponse, initial: Editing['initial'], extra: Pick<Editing, 'code' | 'lineId'> = {}) => {
    setFormError(null)
    setUnknownCode(null)
    setEditing({
      key: Date.now(),
      product: { name: product.name, trackExpiry: product.trackExpiry, units: product.units.filter((unit) => unit.isActive) },
      initial,
      ...extra,
    })
  }

  // Một mã vừa nhận được (camera, máy quét bàn phím, ảnh, gõ mã): tra mã → mở form dòng nhập (UC-04 bước 2).
  const handleCode = async (raw: string) => {
    const code = normalizeBarcode(raw)
    if (editing?.code === code) return
    setNotice(null)
    try {
      const item = await lookup(code)
      if (item.productStatus === 'inactive') {
        scanFeedback('warn')
        setNotice({ kind: 'bad', text: `${item.productName} đã ngừng kinh doanh.` })
        return
      }
      const product = await loadProduct(item.productId)
      scanFeedback('ok')
      open(product, { unitId: item.unitId }, { code })
    } catch (caught) {
      scanFeedback('warn')
      if (caught instanceof ApiError && caught.status === 404) {
        setEditing(null)
        setUnknownCode(code)
      } else setNotice({ kind: 'bad', text: errorText(caught, 'Không tra được mã.') })
    }
  }

  const editLine = async (line: ReceiptLineResponse) => {
    setNotice(null)
    try {
      const product = await loadProduct(line.productId)
      open(
        product,
        { unitId: line.unitId, qty: line.qty, unitPrice: line.unitPrice?.toString() ?? '', expiryDate: line.expiryDate ?? '', lotNo: line.lotNo ?? '' },
        { lineId: line.id },
      )
    } catch (caught) {
      setNotice({ kind: 'bad', text: errorText(caught, 'Không tải được sản phẩm.') })
    }
  }

  const saveLine = (body: ReceiptLineRequest) => {
    if (!editing) return
    setFormError(null)
    const name = editing.product.name
    const handlers = {
      onSuccess: () => {
        setEditing(null)
        setExpiryPrompt(null)
        setNotice({ kind: 'ok' as const, text: editing.lineId ? `Đã sửa dòng ${name}.` : `Đã thêm ${name}.` })
      },
      onError: (caught: Error) => setFormError(errorText(caught, 'Không lưu được dòng.')),
    }
    if (editing.lineId) updateLine.mutate({ lineId: editing.lineId, body }, handlers)
    else addLine.mutate(body, handlers)
  }

  const finish = (confirmExpiryWarning = false) => {
    setNotice(null)
    complete.mutate(confirmExpiryWarning, {
      onSuccess: () => scanFeedback('ok'),
      onError: (caught) => {
        if (caught instanceof ApiError && caught.status === 409 && caught.message.startsWith('Cảnh báo hạn dùng')) setExpiryPrompt(forPeople(caught.message))
        else setNotice({ kind: 'bad', text: errorText(caught, 'Không hoàn tất được phiếu.') })
      },
    })
  }

  const busyLine = addLine.isPending || updateLine.isPending
  const activeSuppliers = (suppliers.data ?? []).filter((supplier) => supplier.isActive || supplier.id === receipt.supplierId)
  const receivingLocations = (locations.data ?? []).filter((location) => location.isActive || location.id === receipt.locationId)

  // Đổi đầu phiếu: API cần đủ nhà cung cấp + vị trí + ghi chú, nên giữ nguyên phần không đổi.
  const saveHeader = (change: { supplierId?: number | null; locationId?: number }, failText: string) =>
    updateHeader.mutate(
      { supplierId: receipt.supplierId, locationId: receipt.locationId, note: receipt.note, ...change },
      { onError: (caught) => setNotice({ kind: 'bad', text: errorText(caught, failText) }) },
    )

  return (
    <div className="grid gap-4 pb-[calc(9rem+env(safe-area-inset-bottom))] lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-6 lg:pb-4 xl:grid-cols-[minmax(0,1fr)_28rem] 2xl:grid-cols-[minmax(0,1fr)_34rem]">
      <div className="flex min-w-0 flex-col gap-4">
        <div className="flex flex-col gap-2 rounded-xl border border-line bg-surface p-3">
          <h1 className="text-xl font-bold">
            Phiếu nhập <span className="tabular-nums">{receipt.code}</span> <span className="text-base font-semibold text-warn">· Nháp</span>
          </h1>
          <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-1 2xl:grid-cols-2">
            <div className="flex flex-col gap-1">
              <label htmlFor="receipt-supplier" className="text-sm font-semibold">
                Nhà cung cấp
              </label>
              <select
                id="receipt-supplier"
                value={receipt.supplierId ?? ''}
                disabled={updateHeader.isPending}
                onChange={(event) =>
                  saveHeader({ supplierId: event.target.value ? Number(event.target.value) : null }, 'Không đổi được nhà cung cấp.')
                }
                className={selectClass}
              >
                <option value="">Chưa chọn (hàng mua lẻ)</option>
                {activeSuppliers.map((supplier) => (
                  <option key={supplier.id} value={supplier.id}>
                    {supplier.name}
                  </option>
                ))}
              </select>
            </div>
            {/* Tiệm có từ hai vị trí (kho sau + kệ) mới cần chọn; tiệm một vị trí thì luôn nhận vào kệ (UC-04 bước 1). */}
            {receivingLocations.length > 1 ? (
              <div className="flex flex-col gap-1">
                <label htmlFor="receipt-location" className="text-sm font-semibold">
                  Nhận vào
                </label>
                <select
                  id="receipt-location"
                  value={receipt.locationId}
                  disabled={updateHeader.isPending}
                  onChange={(event) => saveHeader({ locationId: Number(event.target.value) }, 'Không đổi được vị trí nhận.')}
                  className={selectClass}
                >
                  {receivingLocations.map((location) => (
                    <option key={location.id} value={location.id}>
                      {location.name}{location.type === 'shelf' ? ' · bán được ngay' : ''}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <p className="self-end text-sm text-ink-muted sm:pb-3">Nhận vào: {receipt.locationName}</p>
            )}
          </div>
        </div>

        <ScanPanel onCode={(code) => void handleCode(code)} />

        <div aria-live="polite">
          {notice && (
            <p
              className={`flex items-center gap-2 rounded-xl border-2 px-4 py-3 font-semibold ${notice.kind === 'ok' ? 'border-ok bg-ok/10' : 'border-bad bg-bad/10 text-bad'}`}
            >
              {notice.kind === 'ok' ? (
                <CheckCircleIcon size="1.5rem" weight="fill" className="shrink-0 text-ok" aria-hidden="true" />
              ) : (
                <WarningIcon size="1.5rem" weight="bold" className="shrink-0" aria-hidden="true" />
              )}
              {notice.text}
            </p>
          )}
        </div>

        {unknownCode &&
          (role === 'owner' ? (
            <QuickProductForm
              barcode={unknownCode}
              defaultSupplierId={receipt.supplierId}
              onCancel={() => setUnknownCode(null)}
              onCreated={(product) => {
                const unit = purchaseUnit(product.units)
                if (unit) open(product, { unitId: unit.id }, { code: unknownCode })
              }}
            />
          ) : (
            <div className="flex flex-col gap-2 rounded-xl border-2 border-warn bg-warn/10 px-4 py-3">
              <p className="font-semibold text-warn">
                Mã <span className="tabular-nums">{unknownCode}</span> chưa có trong danh mục. Nhờ chủ tiệm khai báo sản phẩm rồi nhập tiếp.
              </p>
              <button type="button" onClick={() => setUnknownCode(null)} className="min-h-11 self-start rounded-lg border border-warn bg-surface px-3 text-sm font-semibold">
                Đã hiểu
              </button>
            </div>
          ))}

        {editing && (
          <ReceiptLineForm
            key={editing.key}
            product={editing.product}
            initial={editing.initial}
            canSeeCost={canSeeCost}
            busy={busyLine}
            error={formError}
            submitLabel={editing.lineId ? 'Lưu dòng' : 'Thêm vào phiếu'}
            onSubmit={saveLine}
            onCancel={() => setEditing(null)}
          />
        )}

        {!editing && (
          <ProductPicker
            inputId="receipt-search"
            onBarcode={(code) => void handleCode(code)}
            describeItem={(item) => `${item.baseUnitName} · tồn ${item.totalStock}`}
            describeUnit={(unit) => (unit.factor > 1 ? `${unit.factor} đơn vị cơ sở` : 'đơn vị cơ sở')}
            autoUnit={purchaseUnit}
            onPick={(product, unit) => open(product, { unitId: unit.id })}
          />
        )}
      </div>

      <div className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-6 lg:self-start">
        <h2 className="text-lg font-semibold">Hàng trong phiếu · {receipt.lines.length} dòng</h2>
        <ReceiptLineList
          lines={receipt.lines}
          pricesHidden={receipt.pricesHidden}
          busy={deleteLine.isPending}
          onEdit={(line) => void editLine(line)}
          onDelete={(line) =>
            deleteLine.mutate(line.id, { onError: (caught) => setNotice({ kind: 'bad', text: errorText(caught, 'Không xóa được dòng.') }) })
          }
        />

        <div className="flex flex-col gap-2">
          {confirmDelete ? (
            <div className="grid grid-cols-[1fr_auto] gap-2">
              <button
                type="button"
                disabled={deleteReceipt.isPending}
                onClick={() =>
                  deleteReceipt.mutate(undefined, {
                    onSuccess: () => void navigate('/nhap-hang'),
                    onError: (caught) => setNotice({ kind: 'bad', text: errorText(caught, 'Không xóa được phiếu.') }),
                  })
                }
                className="min-h-11 rounded-lg bg-bad px-3 font-semibold text-white disabled:opacity-60"
              >
                Xóa hẳn phiếu nháp này
              </button>
              <button type="button" onClick={() => setConfirmDelete(false)} className="min-h-11 rounded-lg border border-line bg-surface px-3 font-semibold">
                Thôi
              </button>
            </div>
          ) : (
            <button type="button" onClick={() => setConfirmDelete(true)} className="min-h-11 self-start rounded-lg px-1 text-sm font-semibold text-bad underline-offset-4 hover:underline">
              Xóa phiếu nháp
            </button>
          )}
        </div>

        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-line bg-surface px-[max(1rem,env(safe-area-inset-left))] pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] lg:static lg:rounded-xl lg:border lg:p-4">
          <div className="mx-auto flex max-w-3xl flex-col gap-2 lg:max-w-none">
            {expiryPrompt && (
              <div role="alert" className="flex flex-col gap-2 rounded-lg border border-warn/50 bg-warn/10 p-2">
                <p className="text-sm font-semibold text-warn">{expiryPrompt}</p>
                <div className="grid grid-cols-[1fr_auto] gap-2">
                  <button type="button" onClick={() => finish(true)} disabled={complete.isPending} className="min-h-11 rounded-lg bg-warn px-3 font-semibold text-white disabled:opacity-60">
                    Đã xem, vẫn nhập
                  </button>
                  <button type="button" onClick={() => setExpiryPrompt(null)} className="min-h-11 rounded-lg border border-line bg-surface px-3 font-semibold">
                    Thôi
                  </button>
                </div>
              </div>
            )}
            {canSeeCost && receipt.missingPriceLines > 0 && (
              <p className="text-sm font-semibold text-warn">Còn {receipt.missingPriceLines} dòng chưa có giá nhập.</p>
            )}
            <div className="flex items-center gap-3">
              <div className="min-w-0 flex-1">
                <p className="text-sm text-ink-muted">{receipt.lines.length} dòng</p>
                {receipt.total !== null && <p className="text-2xl font-bold tabular-nums">{formatMoney(receipt.total)}</p>}
              </div>
              <button
                type="button"
                onClick={() => finish()}
                disabled={complete.isPending || receipt.lines.length === 0 || editing !== null}
                className="h-12 rounded-lg bg-brand-600 px-5 font-semibold text-white active:bg-brand-700 disabled:opacity-60"
              >
                {complete.isPending ? 'Đang nhập kho…' : 'Hoàn tất nhập kho'}
              </button>
            </div>
            {editing && <p className="text-sm text-ink-muted">Lưu hoặc bỏ dòng đang nhập trước khi hoàn tất.</p>}
          </div>
        </div>
      </div>
    </div>
  )
}
