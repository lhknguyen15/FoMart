import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useParams } from 'react-router'
import type { DisposalLineResponse } from '../../../api/inventory'
import { ApiError } from '../../../api/types'
import { formatDate, formatDateTime, formatDisposalReason, formatMoney } from '../../../lib/format'
import { useAuthStore } from '../../../stores/useAuthStore'
import { DisposalStatusBadge } from '../components/DisposalStatusBadge'
import { useApproveDisposal, useDisposal, useRejectDisposal } from '../hooks/useDisposals'
import { rejectDisposalSchema, type RejectDisposalValues } from '../schemas/disposalSchema'

const backLink = (
  <Link to="/xuat-huy" className="flex min-h-11 items-center self-start font-semibold text-brand-700 underline-offset-4 hover:underline">
    Về danh sách phiếu hủy
  </Link>
)

const lotName = (lot: { lotNo: string | null; expiryDate: string | null }) =>
  [lot.lotNo ? `lô ${lot.lotNo}` : 'lô', lot.expiryDate ? `HSD ${formatDate(lot.expiryDate)}` : null].filter(Boolean).join(' ')

// Chi tiết phiếu xuất hủy (UC-08): các dòng, lô đã trừ, tổn thất theo giá vốn (BR-08).
// Phiếu chờ: chủ tiệm duyệt (trừ tồn) hoặc từ chối kèm lý do (BR-21).
export function DisposalDetailPage() {
  const { id = '' } = useParams()
  const disposalId = Number(id)
  const disposal = useDisposal(disposalId)
  const role = useAuthStore((state) => state.session?.user.role)
  const approve = useApproveDisposal(disposalId)
  const reject = useRejectDisposal(disposalId)
  const [rejecting, setRejecting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RejectDisposalValues>({ resolver: zodResolver(rejectDisposalSchema), defaultValues: { reason: '' } })

  if (!Number.isInteger(disposalId) || disposalId <= 0 || (disposal.error instanceof ApiError && disposal.error.status === 404)) {
    return (
      <div className="flex flex-col gap-2">
        <p className="font-semibold">Không tìm thấy phiếu hủy này.</p>
        {backLink}
      </div>
    )
  }
  if (disposal.isPending) return <p className="text-ink-muted">Đang tải phiếu hủy…</p>
  if (disposal.isError) return <p className="font-semibold text-bad">{disposal.error.message}</p>

  const data = disposal.data
  const canDecide = data.status === 'pending' && role === 'owner'
  const tone = data.status === 'approved' ? 'border-ok bg-ok/10' : data.status === 'pending' ? 'border-warn bg-warn/10' : 'border-line bg-surface'
  const onError = (caught: Error) => setError(caught instanceof ApiError ? caught.message : 'Không thực hiện được.')

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 lg:grid lg:max-w-none lg:grid-cols-[22rem_minmax(0,1fr)] lg:items-start lg:gap-6 xl:grid-cols-[26rem_minmax(0,1fr)]">
      <div className="flex flex-col gap-4">
        <section className={`flex flex-col gap-1 rounded-xl border-2 px-4 py-3 ${tone}`}>
          <h1 className="flex flex-wrap items-center gap-2 text-xl font-bold">
            Phiếu hủy <span className="tabular-nums">{data.code}</span>
            <DisposalStatusBadge status={data.status} />
          </h1>
          {data.lossValue !== null && <p className="text-2xl font-bold tabular-nums">Tổn thất {formatMoney(data.lossValue)}</p>}
          <p className="text-sm">
            Lập {formatDateTime(data.createdAt)} · {data.createdByName}
          </p>
          {data.decidedAt && (
            <p className="text-sm">
              {data.status === 'approved' ? 'Duyệt' : 'Từ chối'} {formatDateTime(data.decidedAt)} · {data.decidedByName}
            </p>
          )}
          {data.rejectReason && <p className="text-sm font-semibold">Lý do từ chối: {data.rejectReason}</p>}
          {data.note && <p className="text-sm text-ink-muted">Ghi chú: {data.note}</p>}
          {data.status === 'pending' && role !== 'owner' && <p className="text-sm font-semibold text-warn">Đang chờ chủ tiệm duyệt; tồn chưa bị trừ.</p>}
        </section>

        {error && (
          <p role="alert" className="font-semibold text-bad">
            {error}
          </p>
        )}

        {canDecide && !rejecting && (
          <div className="grid grid-cols-[1fr_auto] gap-2">
            <button
              type="button"
              disabled={approve.isPending}
              onClick={() => {
                setError(null)
                approve.mutate(undefined, { onError })
              }}
              className="h-12 rounded-lg bg-brand-600 px-4 font-semibold text-white active:bg-brand-700 disabled:opacity-60"
            >
              {approve.isPending ? 'Đang duyệt…' : 'Duyệt và trừ tồn'}
            </button>
            <button type="button" onClick={() => setRejecting(true)} className="h-12 rounded-lg border border-bad px-4 font-semibold text-bad active:bg-bad/10">
              Từ chối
            </button>
          </div>
        )}

        {canDecide && rejecting && (
          <form
            noValidate
            onSubmit={handleSubmit(({ reason }) => {
              setError(null)
              reject.mutate(reason, { onSuccess: () => setRejecting(false), onError })
            })}
            className="flex flex-col gap-2 rounded-xl border-2 border-bad/50 bg-surface p-3"
          >
            <label htmlFor="reject-reason" className="text-sm font-semibold">
              Lý do từ chối
            </label>
            <input
              id="reject-reason"
              autoComplete="off"
              placeholder="Hàng còn dùng được…"
              aria-invalid={errors.reason ? true : undefined}
              className="h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40 aria-invalid:border-bad"
              {...register('reason')}
            />
            {errors.reason && <p className="text-sm font-semibold text-bad">{errors.reason.message}</p>}
            <div className="grid grid-cols-[1fr_auto] gap-2">
              <button type="submit" disabled={reject.isPending} className="h-12 rounded-lg bg-bad font-semibold text-white disabled:opacity-60">
                {reject.isPending ? 'Đang lưu…' : 'Xác nhận từ chối'}
              </button>
              <button type="button" onClick={() => setRejecting(false)} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
                Thôi
              </button>
            </div>
          </form>
        )}
        <div className="hidden lg:block">{backLink}</div>
      </div>

      <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface lg:col-start-2 lg:row-start-1">
        {data.lines.map((line) => (
          <DisposalLineItem key={line.id} line={line} />
        ))}
      </ul>
      <div className="lg:hidden">{backLink}</div>
    </div>
  )
}

function DisposalLineItem({ line }: { line: DisposalLineResponse }) {
  return (
    <li className="flex flex-col gap-0.5 border-b border-line px-3 py-2.5 last:border-b-0">
      <div className="flex items-baseline justify-between gap-2">
        <Link to={`/ton-kho/${line.productId}`} className="min-w-0 font-semibold text-ink underline-offset-4 hover:underline">
          {line.productName}
        </Link>
        <span className="shrink-0 font-semibold tabular-nums">
          {line.baseQty} {line.baseUnitName}
        </span>
      </div>
      <p className="text-sm text-ink-muted">
        {formatDisposalReason(line.reason)} · {line.locationName}
        {line.lotId ? ` · ${lotName(line)}` : ' · hệ thống chọn lô hạn sớm nhất'}
        {line.lossValue !== null ? ` · tổn thất ${formatMoney(line.lossValue)}` : ''}
      </p>
      {line.note && <p className="text-sm">{line.note}</p>}
      {line.lots.length > 0 && <p className="text-sm text-ink-muted">Đã trừ: {line.lots.map((lot) => `${lot.qty} từ ${lotName(lot)}`).join(', ')}</p>}
    </li>
  )
}
