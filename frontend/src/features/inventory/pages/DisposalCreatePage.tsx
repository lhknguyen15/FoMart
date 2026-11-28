import { zodResolver } from '@hookform/resolvers/zod'
import { TrashIcon, WarningIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import type { ProductResponse } from '../../../api/catalog'
import { ApiError } from '../../../api/types'
import { ScanPanel } from '../../../components/ui/ScanPanel'
import { scanFeedback } from '../../../lib/feedback'
import { formatDisposalReason } from '../../../lib/format'
import { useAuthStore } from '../../../stores/useAuthStore'
import { ProductPicker } from '../../catalog/components/ProductPicker'
import { useBarcodeLookup, useProduct, useProductLoader } from '../../catalog/hooks/useProducts'
import { DisposalLineEditor } from '../components/DisposalLineEditor'
import { useCreateDisposal } from '../hooks/useDisposals'
import { disposalSchema, type DisposalLineValues, type DisposalValues } from '../schemas/disposalSchema'

// Dòng đang soạn: sản phẩm + giá trị gợi ý; key đổi mỗi lần mở để form con tạo lại.
interface Editing {
  key: number
  product: ProductResponse
  initial?: Partial<DisposalLineValues>
}

const reasons = ['expired', 'broken', 'damaged', 'other'] as const
const asReason = (value: string | null) => (reasons as readonly string[]).includes(value ?? '') ? (value as DisposalLineValues['reason']) : undefined
const positive = (value: string | null) => (value && Number(value) > 0 ? Number(value) : undefined)

// Lập phiếu xuất hủy (UC-08, SC-12): quét / tìm hàng bể, hư, hoặc mở từ màn hình Hạn dùng (điền sẵn lô, số lượng, lý do).
// Nhiều dòng trong một phiếu (React Hook Form useFieldArray). Chủ tiệm gửi thì trừ tồn ngay; nhân viên gửi thì chờ duyệt (BR-21).
export function DisposalCreatePage() {
  const role = useAuthStore((state) => state.session?.user.role)
  const navigate = useNavigate()
  const lookup = useBarcodeLookup()
  const loadProduct = useProductLoader()
  const create = useCreateDisposal()

  // Mở từ màn hình Hạn dùng: /xuat-huy/moi?productId=&locationId=&lotId=&qty=&reason=expired
  const [params] = useSearchParams()
  const prefillProductId = positive(params.get('productId')) ?? 0
  const prefillProduct = useProduct(prefillProductId)
  const [prefillDone, setPrefillDone] = useState(prefillProductId === 0)

  const [editing, setEditing] = useState<Editing | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<DisposalValues>({ resolver: zodResolver(disposalSchema), defaultValues: { note: '', lines: [] } })
  // useFieldArray: danh sách dòng nằm trong form, có id ổn định cho key; append / remove không phải tự chép mảng.
  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })

  // Dòng điền sẵn từ màn hình Hạn dùng hiện ngay khi tải xong sản phẩm (suy ra từ dữ liệu, không cần effect).
  const shownEditing: Editing | null =
    editing ??
    (!prefillDone && prefillProduct.data
      ? {
          key: 0,
          product: prefillProduct.data,
          initial: {
            locationId: positive(params.get('locationId')),
            lotId: positive(params.get('lotId')) ?? 0,
            unitId: prefillProduct.data.units.find((unit) => unit.isBase)?.id,
            qty: positive(params.get('qty')),
            reason: asReason(params.get('reason')),
          },
        }
      : null)

  const open = (product: ProductResponse, unitId?: number) => {
    setNotice(null)
    setPrefillDone(true)
    setEditing({ key: Date.now(), product, initial: unitId ? { unitId } : undefined })
  }
  const closeEditor = () => {
    setPrefillDone(true)
    setEditing(null)
  }

  // Quét mã hàng bể / hư (camera, máy quét bàn phím, ảnh, gõ mã): tra mã → mở dòng với đúng đơn vị vừa quét.
  const handleCode = async (raw: string) => {
    try {
      const item = await lookup(raw)
      const product = await loadProduct(item.productId)
      scanFeedback('ok')
      open(product, item.unitId)
    } catch (caught) {
      scanFeedback('warn')
      setNotice(caught instanceof ApiError && caught.status === 404 ? `Mã ${raw.trim()} chưa có trong danh mục.` : 'Không tra được mã.')
    }
  }

  const submit = (values: DisposalValues) => {
    setSubmitError(null)
    create.mutate(
      {
        note: values.note || null,
        lines: values.lines.map((line) => ({
          productId: line.productId,
          locationId: line.locationId,
          lotId: line.lotId,
          baseQty: line.baseQty,
          reason: line.reason,
          note: line.note || null,
        })),
      },
      {
        onSuccess: ({ disposal }) => void navigate(`/xuat-huy/${disposal.id}`, { replace: true }),
        onError: (caught) => setSubmitError(caught instanceof ApiError ? caught.message : 'Không lưu được phiếu hủy.'),
      },
    )
  }

  return (
    <div className="grid gap-4 pb-[calc(8rem+env(safe-area-inset-bottom))] lg:grid-cols-[minmax(0,1fr)_26rem] lg:gap-6 lg:pb-4 xl:grid-cols-[minmax(0,1fr)_30rem]">
      <div className="flex min-w-0 flex-col gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-bold">Lập phiếu xuất hủy</h1>
          <p className="text-sm text-ink-muted">
            Quét hoặc tìm hàng bể, hư, hết hạn; chọn lô và số lượng. {role === 'owner' ? 'Phiếu của chủ tiệm trừ tồn ngay khi lưu.' : 'Phiếu sẽ chờ chủ tiệm duyệt rồi mới trừ tồn.'}
          </p>
        </div>

        {shownEditing && (
          <DisposalLineEditor
            key={shownEditing.key}
            product={shownEditing.product}
            initial={shownEditing.initial}
            onCancel={closeEditor}
            onAdd={(line) => {
              append(line)
              closeEditor()
            }}
          />
        )}
        {!shownEditing && (
          <>
            <ScanPanel onCode={(code) => void handleCode(code)} />
            {notice && (
              <p role="alert" className="flex items-center gap-2 rounded-xl border-2 border-warn bg-warn/10 px-4 py-3 font-semibold text-warn">
                <WarningIcon size="1.25rem" weight="bold" className="shrink-0" aria-hidden="true" />
                {notice}
              </p>
            )}
            <ProductPicker
              inputId="disposal-search"
              onBarcode={(code) => void handleCode(code)}
              describeItem={(item) => `${item.baseUnitName} · tồn ${item.totalStock}`}
              describeUnit={(unit) => (unit.factor > 1 ? `${unit.factor} đơn vị cơ sở` : 'đơn vị cơ sở')}
              onPick={(product, unit) => open(product, unit.id)}
            />
          </>
        )}
        {shownEditing === null && prefillProductId > 0 && !prefillDone && prefillProduct.isPending && <p className="text-ink-muted">Đang tải sản phẩm…</p>}
      </div>

      {/* Chỉ cột phải là <form> của phiếu; form con soạn dòng ở cột trái là <form> riêng (HTML không cho lồng form). */}
      <form onSubmit={handleSubmit(submit)} noValidate className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-6 lg:self-start">
        <h2 className="text-lg font-semibold">Hàng cần hủy · {fields.length} dòng</h2>
        {fields.length === 0 ? (
          <p className="rounded-xl border border-dashed border-line bg-surface px-4 py-6 text-center text-ink-muted">Chưa có dòng nào.</p>
        ) : (
          <ul className="flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
            {fields.map((field, index) => (
              <li key={field.id} className="flex items-start gap-2 border-b border-line px-3 py-2.5 last:border-b-0">
                <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <span className="font-semibold">{field.productName}</span>
                  <span className="text-sm text-ink-muted tabular-nums">
                    {field.qtyLabel} · {field.locationName} · {field.lotLabel}
                  </span>
                  <span className="text-sm">
                    {formatDisposalReason(field.reason)}
                    {field.note ? ` · ${field.note}` : ''}
                  </span>
                </div>
                <button
                  type="button"
                  onClick={() => remove(index)}
                  aria-label={`Bỏ dòng ${field.productName}`}
                  className="flex size-11 shrink-0 items-center justify-center rounded-lg border border-line text-bad active:bg-bad/10"
                >
                  <TrashIcon size="1.25rem" aria-hidden="true" />
                </button>
              </li>
            ))}
          </ul>
        )}
        {errors.lines?.message && <p className="text-sm font-semibold text-bad">{errors.lines.message}</p>}

        <div className="flex flex-col gap-1">
          <label htmlFor="disposal-note" className="text-sm font-semibold">
            Ghi chú phiếu (nếu có)
          </label>
          <input
            id="disposal-note"
            autoComplete="off"
            placeholder="Dọn kệ cuối tuần…"
            className="h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40"
            {...register('note')}
          />
        </div>

        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-line bg-surface px-[max(1rem,env(safe-area-inset-left))] pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] lg:static lg:rounded-xl lg:border lg:p-4">
          <div className="mx-auto flex max-w-3xl flex-col gap-2 lg:max-w-none">
            {submitError && (
              <p role="alert" className="text-sm font-semibold text-bad">
                {submitError}
              </p>
            )}
            <div className="grid grid-cols-[1fr_auto] gap-2">
              <button
                type="submit"
                disabled={create.isPending || fields.length === 0 || shownEditing !== null}
                className="h-12 rounded-lg bg-brand-600 px-4 font-semibold whitespace-nowrap text-white active:bg-brand-700 disabled:opacity-60"
              >
                {create.isPending ? 'Đang lưu…' : role === 'owner' ? 'Lưu và trừ tồn' : 'Gửi chủ tiệm duyệt'}
              </button>
              <Link to="/xuat-huy" className="flex h-12 items-center rounded-lg border border-line bg-surface px-4 font-semibold">
                Thôi
              </Link>
            </div>
            {shownEditing && <p className="text-sm text-ink-muted">Thêm hoặc bỏ dòng đang soạn trước khi lưu.</p>}
          </div>
        </div>
      </form>
    </div>
  )
}
