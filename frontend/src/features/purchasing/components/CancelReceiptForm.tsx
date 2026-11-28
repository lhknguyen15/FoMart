import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '../../../api/types'
import { useCancelReceipt } from '../hooks/useGoodsReceipts'
import { cancelReceiptSchema, type CancelReceiptValues } from '../schemas/cancelReceiptSchema'

// Chủ tiệm hủy phiếu nhập đã hoàn tất (UC-04 ngoại lệ, BR-03): hệ thống ghi chứng từ đảo ngược,
// chỉ được khi hàng của phiếu chưa bán / chuyển / hủy / kiểm kê.
export function CancelReceiptForm({ receiptId }: { receiptId: number }) {
  const [open, setOpen] = useState(false)
  const cancel = useCancelReceipt(receiptId)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CancelReceiptValues>({ resolver: zodResolver(cancelReceiptSchema), defaultValues: { reason: '' } })

  if (!open) {
    return (
      <button type="button" onClick={() => setOpen(true)} className="min-h-11 self-start rounded-lg border border-bad px-3 font-semibold text-bad active:bg-bad/10">
        Hủy phiếu nhập
      </button>
    )
  }

  const submit = ({ reason }: CancelReceiptValues) =>
    cancel.mutate(reason, {
      onError: (caught) => setError('root', { message: caught instanceof ApiError ? caught.message : 'Không hủy được phiếu.' }),
    })

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-2 rounded-xl border-2 border-bad/50 bg-surface p-3">
      <p className="font-semibold">Hủy phiếu: tồn kho trừ lại đúng số đã nhập.</p>
      <label htmlFor="cancel-reason" className="text-sm font-semibold">
        Lý do
      </label>
      <input
        id="cancel-reason"
        autoComplete="off"
        placeholder="Nhập nhầm số lượng, sai hàng…"
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
          {cancel.isPending ? 'Đang hủy…' : 'Xác nhận hủy phiếu'}
        </button>
        <button type="button" onClick={() => setOpen(false)} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
