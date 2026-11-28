import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import type { ProductResponse } from '../../../api/catalog'
import { ChoiceChips, type Choice } from '../../../components/ui/ChoiceChips'
import { formatDate, formatDisposalReason } from '../../../lib/format'
import { useLocations } from '../../catalog/hooks/useLocations'
import { useProductStock } from '../hooks/useStock'
import { disposalLineSchema, type DisposalDraftLine, type DisposalLineValues } from '../schemas/disposalSchema'

interface DisposalLineEditorProps {
  product: ProductResponse
  initial?: Partial<DisposalLineValues>
  onAdd: (line: DisposalDraftLine) => void
  onCancel: () => void
}

const reasonChoices: Choice<DisposalLineValues['reason']>[] = (['expired', 'broken', 'damaged', 'other'] as const).map((value) => ({
  value,
  label: formatDisposalReason(value),
}))

const selectClass = 'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40'
const inputClass = `${selectClass} aria-invalid:border-bad`

// Soạn một dòng hủy (UC-08 bước 2): vị trí, lô (hoặc để hệ thống chọn hạn sớm nhất), đơn vị + số lượng, lý do.
// Hiện số còn trong hệ thống để người dùng biết trước; server vẫn kiểm cuối cùng (hủy không cho tồn âm).
export function DisposalLineEditor({ product, initial, onAdd, onCancel }: DisposalLineEditorProps) {
  const locations = useLocations()
  const lots = useProductStock(product.id)
  const units = product.units.filter((unit) => unit.isActive)
  const baseUnit = units.find((unit) => unit.isBase)
  const stockedLocation = product.stocks.find((stock) => stock.qty > 0)?.locationId

  const {
    register,
    handleSubmit,
    setValue,
    control,
    formState: { errors },
  } = useForm<DisposalLineValues>({
    resolver: zodResolver(disposalLineSchema),
    defaultValues: {
      locationId: initial?.locationId ?? stockedLocation ?? 0,
      lotId: initial?.lotId ?? 0,
      unitId: initial?.unitId ?? baseUnit?.id ?? units[0]?.id ?? 0,
      qty: initial?.qty ?? 1,
      reason: initial?.reason ?? 'broken',
      note: '',
    },
  })
  const [locationId, lotId, unitId, qty, reason] = useWatch({ control, name: ['locationId', 'lotId', 'unitId', 'qty', 'reason'] })

  const locationLots = (lots.data ?? []).filter((lot) => lot.locationId === locationId && lot.qty > 0)
  const unit = units.find((item) => item.id === unitId)
  const baseQty = unit && Number.isInteger(qty) ? qty * unit.factor : null
  const chosenLot = locationLots.find((lot) => lot.lotId === lotId)
  const available = lotId ? (chosenLot?.qty ?? 0) : locationLots.reduce((sum, lot) => sum + lot.qty, 0)
  const lotText = (lot: (typeof locationLots)[number]) =>
    [lot.lotNo ? `Lô ${lot.lotNo}` : 'Lô không số', lot.expiryDate ? `HSD ${formatDate(lot.expiryDate)}` : null, `còn ${lot.qty}`].filter(Boolean).join(' · ')

  const submit = (values: DisposalLineValues) => {
    if (!unit || baseQty === null) return
    const location = locations.data?.find((item) => item.id === values.locationId)
    onAdd({
      productId: product.id,
      productName: product.name,
      baseUnitName: baseUnit?.name ?? '',
      locationId: values.locationId,
      locationName: location?.name ?? '',
      lotId: values.lotId || null,
      lotLabel: chosenLot ? lotText(chosenLot).replace(/ · còn \d+$/, '') : 'Hệ thống chọn lô hạn sớm nhất',
      baseQty,
      qtyLabel: unit.factor > 1 ? `${values.qty} ${unit.name} = ${baseQty} ${baseUnit?.name ?? ''}` : `${values.qty} ${unit.name}`,
      reason: values.reason,
      note: values.note,
    })
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-3 rounded-xl border-2 border-brand-600 bg-surface p-3">
      <p className="text-lg font-semibold">{product.name}</p>

      <div className="grid gap-3 sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <label htmlFor="disposal-location" className="text-sm font-semibold">
            Vị trí
          </label>
          <select
            id="disposal-location"
            className={selectClass}
            {...register('locationId', {
              valueAsNumber: true,
              onChange: () => setValue('lotId', 0), // đổi vị trí thì lô cũ không còn đúng
            })}
          >
            {locationId === 0 && <option value={0}>Chọn vị trí</option>}
            {(locations.data ?? []).map((location) => (
              <option key={location.id} value={location.id}>
                {location.name}
              </option>
            ))}
          </select>
          {errors.locationId && <p className="text-sm font-semibold text-bad">{errors.locationId.message}</p>}
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="disposal-lot" className="text-sm font-semibold">
            Lô
          </label>
          {/* Điều khiển bằng value: danh sách lô tải sau, ô chọn vẫn hiện đúng lô điền sẵn từ màn hình Hạn dùng. */}
          <select id="disposal-lot" className={selectClass} value={lotId} onChange={(event) => setValue('lotId', Number(event.target.value))}>
            <option value={0}>Để hệ thống chọn (hạn sớm nhất)</option>
            {locationLots.map((lot) => (
              <option key={lot.lotId} value={lot.lotId}>
                {lotText(lot)}
              </option>
            ))}
          </select>
        </div>
      </div>

      {units.length > 1 && (
        <fieldset className="flex flex-col gap-1">
          <legend className="text-sm font-semibold">Đơn vị</legend>
          <div className="flex flex-wrap gap-2">
            {units.map((item) => (
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

      <div className="flex flex-col gap-1">
        <label htmlFor="disposal-qty" className="text-sm font-semibold">
          Số {unit?.name ?? 'lượng'} cần hủy
        </label>
        <input id="disposal-qty" inputMode="numeric" aria-invalid={errors.qty ? true : undefined} className={`${inputClass} tabular-nums sm:w-40`} {...register('qty', { valueAsNumber: true })} />
        {errors.qty && <p className="text-sm font-semibold text-bad">{errors.qty.message}</p>}
        <p className={`text-sm tabular-nums ${baseQty !== null && baseQty > available ? 'font-semibold text-bad' : 'text-ink-muted'}`}>
          {baseQty !== null && unit && unit.factor > 1 ? `= ${baseQty} ${baseUnit?.name ?? ''} · ` : ''}
          Hệ thống còn {available} {baseUnit?.name ?? ''} {lotId ? 'ở lô này' : 'ở vị trí này'}
          {baseQty !== null && baseQty > available ? ': không đủ để hủy, hãy kiểm kê trước.' : ''}
        </p>
      </div>

      <ChoiceChips label="Lý do" choices={reasonChoices} value={reason} onChange={(value) => value && setValue('reason', value)} />

      <div className="flex flex-col gap-1">
        <label htmlFor="disposal-line-note" className="text-sm font-semibold">
          Ghi chú (nếu có)
        </label>
        <input id="disposal-line-note" autoComplete="off" placeholder="Rơi vỡ khi xếp kệ…" aria-invalid={errors.note ? true : undefined} className={inputClass} {...register('note')} />
        {errors.note && <p className="text-sm font-semibold text-bad">{errors.note.message}</p>}
      </div>

      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button type="submit" className="h-12 rounded-lg bg-brand-600 font-semibold text-white active:bg-brand-700">
          Thêm vào phiếu
        </button>
        <button type="button" onClick={onCancel} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
