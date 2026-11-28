import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '../../../api/types'
import { useCancelSale } from '../hooks/useSales'
import { cancelSaleSchema, type CancelSaleValues } from '../schemas/cancelSaleSchema'

// Chủ tiệm hủy hóa đơn đã thanh toán (UC-06): hệ thống cộng tồn lại đúng các lô đã trừ, hóa đơn gốc giữ nguyên (BR-03).
export function CancelSaleForm({ saleId }: { saleId: string }) {
  const [open, setOpen] = useState(false)
  const cancel = useCancelSale()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CancelSaleValues>({ resolver: zodResolver(cancelSaleSchema), defaultValues: { reason: '' } })

  if (!open) {
    return (
      <button type="button" onClick={() => setOpen(true)} className="min-h-11 self-start rounded-lg border border-bad px-3 font-semibold text-bad active:bg-bad/10">
        Hủy hóa đơn
      </button>
    )
  }

  const submit = ({ reason }: CancelSaleValues) =>
    cancel.mutate(
      { id: saleId, reason },
      { onError: (caught) => setError('root', { message: caught instanceof ApiError ? caught.message : 'Không hủy được hóa đơn.' }) },
    )

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-2 rounded-xl border-2 border-bad/50 bg-surface p-3">
      <p className="font-semibold">Hủy hóa đơn: hàng được cộng lại vào kho, doanh thu bị trừ.</p>
      <label htmlFor="sale-cancel-reason" className="text-sm font-semibold">
        Lý do
      </label>
      <input
        id="sale-cancel-reason"
        autoComplete="off"
        placeholder="Bán nhầm, trùng hóa đơn…"
        aria-invalid={errors.reason ? true : undefined}
        className="h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40 aria-invalid:border-bad"
        {...register('reason')}
      />
      {errors.reason && <p className="text-sm font-semibold text-bad">{errors.reason.message}</p>}
      {errors.root && (
        <p role="alert" className="text-sm font-semibold text-bad">
          {errors.root.message}
        </p>
      )}
      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button type="submit" disabled={cancel.isPending} className="h-12 rounded-lg bg-bad font-semibold text-white disabled:opacity-60">
          {cancel.isPending ? 'Đang hủy…' : 'Xác nhận hủy hóa đơn'}
        </button>
        <button type="button" onClick={() => setOpen(false)} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
