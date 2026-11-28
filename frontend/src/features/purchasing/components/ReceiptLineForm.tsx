import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import type { ProductUnitResponse } from '../../../api/catalog'
import type { ReceiptLineRequest } from '../../../api/purchasing'
import { formatDate, formatMoney } from '../../../lib/format'
import { useLastPrice } from '../hooks/useGoodsReceipts'
import { receiptLineSchema, toDigits, type ReceiptLineValues } from '../schemas/receiptLineSchema'

// Sản phẩm đang nhập: tên, có quản lý hạn dùng không, các đơn vị đang dùng (lon, lốc, thùng).
export interface ReceivingProduct {
  name: string
  trackExpiry: boolean
  units: ProductUnitResponse[]
}

interface ReceiptLineFormProps {
  product: ReceivingProduct
  initial: Partial<ReceiptLineValues> & { unitId: number }
  canSeeCost: boolean
  busy: boolean
  error: string | null
  submitLabel: string
  onSubmit: (body: ReceiptLineRequest) => void
  onCancel: () => void
}

const inputClass =
  'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40 aria-invalid:border-bad'

// Nhập một dòng (UC-04 bước 3-4): đơn vị, số lượng, giá nhập mỗi đơn vị (gợi ý giá lần trước),
// hạn dùng + số lô nếu sản phẩm có quản lý hạn dùng. Nhân viên bị ẩn giá thì không có ô giá (BR-08).
export function ReceiptLineForm({ product, initial, canSeeCost, busy, error, submitLabel, onSubmit, onCancel }: ReceiptLineFormProps) {
  const {
    register,
    handleSubmit,
    setValue,
    control,
    formState: { errors },
  } = useForm<ReceiptLineValues>({
    resolver: zodResolver(receiptLineSchema(product.trackExpiry)),
    defaultValues: { qty: 1, unitPrice: '', expiryDate: '', lotNo: '', ...initial },
  })
  const unitId = useWatch({ control, name: 'unitId' })
  const qty = useWatch({ control, name: 'qty' })
  const unitPrice = useWatch({ control, name: 'unitPrice' })
  const last = useLastPrice(unitId, canSeeCost).data
  const unit = product.units.find((item) => item.id === unitId)

  const priceDigits = toDigits(unitPrice)
  const priceNumber = /^\d{1,10}$/.test(priceDigits) ? Number(priceDigits) : null
  const lineTotal = priceNumber !== null && Number.isInteger(qty) ? priceNumber * qty : null

  const submit = (values: ReceiptLineValues) =>
    onSubmit({
      unitId: values.unitId,
      qty: values.qty,
      unitPrice: canSeeCost && values.unitPrice !== '' ? Number(values.unitPrice) : null,
      expiryDate: product.trackExpiry && values.expiryDate !== '' ? values.expiryDate : null,
      lotNo: product.trackExpiry && values.lotNo !== '' ? values.lotNo : null,
    })

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-3 rounded-xl border-2 border-brand-600 bg-surface p-3">
      <p className="text-lg font-semibold">{product.name}</p>

      {product.units.length > 1 && (
        <fieldset className="flex flex-col gap-1">
          <legend className="text-sm font-semibold">Đơn vị</legend>
          <div className="flex flex-wrap gap-2">
            {product.units.map((item) => (
              <button
                key={item.id}
                type="button"
                aria-pressed={item.id === unitId}
                onClick={() => setValue('unitId', item.id)}
                className="min-h-11 rounded-lg border border-line bg-surface px-3 font-semibold aria-pressed:border-brand-600 aria-pressed:bg-brand-600 aria-pressed:text-white"
              >
                {item.name}
                {item.factor > 1 ? ` (${item.factor})` : ''}
              </button>
            ))}
          </div>
        </fieldset>
      )}

      <div className={canSeeCost ? 'grid grid-cols-[7rem_1fr] gap-2' : 'flex flex-col'}>
        <div className="flex flex-col gap-1">
          <label htmlFor="line-qty" className="text-sm font-semibold">
            Số {unit?.name ?? 'lượng'}
          </label>
          <input
            id="line-qty"
            inputMode="numeric"
            aria-invalid={errors.qty ? true : undefined}
            className={`${inputClass} tabular-nums`}
            {...register('qty', { valueAsNumber: true })}
          />
        </div>
        {canSeeCost && (
          <div className="flex flex-col gap-1">
            <label htmlFor="line-price" className="text-sm font-semibold">
              Giá nhập mỗi {unit?.name ?? 'đơn vị'} (đồng)
            </label>
            <input
              id="line-price"
              inputMode="numeric"
              placeholder="Chưa có giá"
              aria-invalid={errors.unitPrice ? true : undefined}
              className={`${inputClass} tabular-nums`}
              {...register('unitPrice')}
            />
          </div>
        )}
      </div>
      {errors.qty && <p className="text-sm font-semibold text-bad">{errors.qty.message}</p>}
      {errors.unitPrice && <p className="text-sm font-semibold text-bad">{errors.unitPrice.message}</p>}

      {canSeeCost && last && (
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span className="text-ink-muted">
            Lần trước <span className="font-semibold text-ink tabular-nums">{formatMoney(last.unitPrice)}</span> · {formatDate(last.receivedAt)}
            {last.supplierName ? ` · ${last.supplierName}` : ''}
          </span>
          {priceNumber !== last.unitPrice && (
            <button
              type="button"
              onClick={() => setValue('unitPrice', String(last.unitPrice), { shouldValidate: true })}
              className="min-h-11 rounded-lg border border-brand-600 px-3 font-semibold text-brand-700 active:bg-brand-50"
            >
              Dùng giá này
            </button>
          )}
        </div>
      )}
      {!canSeeCost && <p className="text-sm text-ink-muted">Giá nhập do chủ tiệm điền (BR-08).</p>}

      {product.trackExpiry && (
        <div className="grid grid-cols-2 gap-2">
          <div className="flex flex-col gap-1">
            <label htmlFor="line-expiry" className="text-sm font-semibold">
              Hạn dùng
            </label>
            <input id="line-expiry" type="date" aria-invalid={errors.expiryDate ? true : undefined} className={inputClass} {...register('expiryDate')} />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="line-lot" className="text-sm font-semibold">
              Số lô (nếu có)
            </label>
            <input id="line-lot" autoComplete="off" aria-invalid={errors.lotNo ? true : undefined} className={inputClass} {...register('lotNo')} />
          </div>
        </div>
      )}
      {errors.expiryDate && <p className="text-sm font-semibold text-bad">{errors.expiryDate.message}</p>}
      {errors.lotNo && <p className="text-sm font-semibold text-bad">{errors.lotNo.message}</p>}

      {unit && unit.factor > 1 && Number.isInteger(qty) && qty > 0 && (
        <p className="text-sm text-ink-muted tabular-nums">
          = {qty * unit.factor} {product.units.find((item) => item.isBase)?.name ?? 'đơn vị cơ sở'}
          {lineTotal !== null ? ` · thành tiền ${formatMoney(lineTotal)}` : ''}
        </p>
      )}
      {(!unit || unit.factor === 1) && lineTotal !== null && <p className="text-sm text-ink-muted tabular-nums">Thành tiền {formatMoney(lineTotal)}</p>}

      {error && (
        <p role="alert" className="text-sm font-semibold text-bad">
          {error}
        </p>
      )}
      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button type="submit" disabled={busy} className="h-12 rounded-lg bg-brand-600 font-semibold text-white active:bg-brand-700 disabled:opacity-60">
          {busy ? 'Đang lưu…' : submitLabel}
        </button>
        <button type="button" onClick={onCancel} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
