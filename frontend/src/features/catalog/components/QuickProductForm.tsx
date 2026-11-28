import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import type { ProductResponse } from '../../../api/catalog'
import { ApiError } from '../../../api/types'
import { useCreateProduct } from '../hooks/useProducts'
import { quickProductSchema, type QuickProductValues } from '../schemas/quickProductSchema'

interface QuickProductFormProps {
  barcode: string
  defaultSupplierId?: number | null
  onCreated: (product: ProductResponse) => void
  onCancel: () => void
}

const inputClass =
  'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40 aria-invalid:border-bad'

// Tạo nhanh sản phẩm khi quét ra mã chưa có (UC-01 nhánh tạo nhanh, UC-04 ngoại lệ): tên, đơn vị, giá bán.
// Một đơn vị cơ sở hệ số 1 mang mã vừa quét; lốc / thùng khai báo sau ở danh mục. Chỉ chủ tiệm (API chặn ở server).
export function QuickProductForm({ barcode, defaultSupplierId, onCreated, onCancel }: QuickProductFormProps) {
  const create = useCreateProduct()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<QuickProductValues>({
    resolver: zodResolver(quickProductSchema),
    defaultValues: { name: '', unitName: '', trackExpiry: false },
  })

  const submit = (values: QuickProductValues) =>
    create.mutate(
      {
        name: values.name,
        trackExpiry: values.trackExpiry,
        defaultSupplierId: defaultSupplierId ?? null,
        units: [{ name: values.unitName, factor: 1, barcode, salePrice: values.salePrice, isBase: true, isDefaultPurchase: true }],
      },
      {
        onSuccess: onCreated,
        onError: (caught) => {
          // 400 có lỗi trường tên thì gắn vào ô tên; còn lại (mã trùng 409, mất mạng...) hiện chung.
          const fields = caught instanceof ApiError ? caught.fieldErrors : undefined
          const nameError = fields?.name?.[0]
          if (nameError) setError('name', { message: nameError })
          else setError('root', { message: caught instanceof ApiError ? caught.message : 'Không tạo được sản phẩm.' })
        },
      },
    )

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="flex flex-col gap-3 rounded-xl border-2 border-warn bg-surface p-3">
      <p className="font-semibold">
        Tạo sản phẩm cho mã <span className="tabular-nums">{barcode}</span>
      </p>
      <div className="flex flex-col gap-1">
        <label htmlFor="quick-name" className="text-sm font-semibold">
          Tên sản phẩm
        </label>
        <input id="quick-name" autoComplete="off" aria-invalid={errors.name ? true : undefined} className={inputClass} {...register('name')} />
        {errors.name && <p className="text-sm font-semibold text-bad">{errors.name.message}</p>}
      </div>
      <div className="grid grid-cols-[7rem_1fr] gap-2">
        <div className="flex flex-col gap-1">
          <label htmlFor="quick-unit" className="text-sm font-semibold">
            Đơn vị
          </label>
          <input id="quick-unit" placeholder="lon, chai…" autoComplete="off" aria-invalid={errors.unitName ? true : undefined} className={inputClass} {...register('unitName')} />
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="quick-price" className="text-sm font-semibold">
            Giá bán (đồng)
          </label>
          <input
            id="quick-price"
            inputMode="numeric"
            aria-invalid={errors.salePrice ? true : undefined}
            className={`${inputClass} tabular-nums`}
            {...register('salePrice', { valueAsNumber: true })}
          />
        </div>
      </div>
      {errors.unitName && <p className="text-sm font-semibold text-bad">{errors.unitName.message}</p>}
      {errors.salePrice && <p className="text-sm font-semibold text-bad">{errors.salePrice.message}</p>}
      <label className="flex min-h-11 items-center gap-3 font-semibold">
        <input type="checkbox" className="size-5 accent-brand-600" {...register('trackExpiry')} />
        Có hạn dùng (quản lý theo lô và hạn)
      </label>
      {errors.root && (
        <p role="alert" className="text-sm font-semibold text-bad">
          {errors.root.message}
        </p>
      )}
      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button type="submit" disabled={create.isPending} className="h-12 rounded-lg bg-brand-600 font-semibold text-white active:bg-brand-700 disabled:opacity-60">
          {create.isPending ? 'Đang tạo…' : 'Tạo và nhập'}
        </button>
        <button type="button" onClick={onCancel} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
