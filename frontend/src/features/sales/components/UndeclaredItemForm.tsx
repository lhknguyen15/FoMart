import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { undeclaredItemSchema, type UndeclaredItemValues } from '../schemas/undeclaredItemSchema'

interface UndeclaredItemFormProps {
  // Mã vừa quét mà chưa có trong danh mục, ghi vào tên để chủ tiệm xử lý sau.
  scannedCode?: string
  onAdd: (values: UndeclaredItemValues) => void
  onCancel: () => void
}

const inputClass =
  'h-12 w-full rounded-lg border border-line bg-surface px-3 text-base focus:border-brand-600 focus:outline-2 focus:outline-brand-500/40 aria-invalid:border-bad'

// Thêm "hàng chưa khai báo" với giá nhập tay (UC-05): bán được ngay, ghi lại trên hóa đơn để chủ tiệm khai báo sau.
export function UndeclaredItemForm({ scannedCode, onAdd, onCancel }: UndeclaredItemFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UndeclaredItemValues>({
    resolver: zodResolver(undeclaredItemSchema),
    defaultValues: { name: scannedCode ? `Hàng mã ${scannedCode}` : '', qty: 1 },
  })

  return (
    <form onSubmit={handleSubmit(onAdd)} noValidate className="flex flex-col gap-3 rounded-xl border border-warn/50 bg-surface p-3">
      <p className="font-semibold">Bán hàng chưa khai báo</p>
      <div className="flex flex-col gap-1">
        <label htmlFor="undeclared-name" className="text-sm font-semibold">
          Tên món
        </label>
        <input id="undeclared-name" aria-invalid={errors.name ? true : undefined} className={inputClass} {...register('name')} />
        {errors.name && <p className="text-sm font-semibold text-bad">{errors.name.message}</p>}
      </div>
      <div className="grid grid-cols-[1fr_6rem] gap-2">
        <div className="flex flex-col gap-1">
          <label htmlFor="undeclared-price" className="text-sm font-semibold">
            Giá (đồng)
          </label>
          <input
            id="undeclared-price"
            inputMode="numeric"
            aria-invalid={errors.unitPrice ? true : undefined}
            className={`${inputClass} tabular-nums`}
            {...register('unitPrice', { valueAsNumber: true })}
          />
          {errors.unitPrice && <p className="text-sm font-semibold text-bad">{errors.unitPrice.message}</p>}
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="undeclared-qty" className="text-sm font-semibold">
            Số lượng
          </label>
          <input
            id="undeclared-qty"
            inputMode="numeric"
            aria-invalid={errors.qty ? true : undefined}
            className={`${inputClass} tabular-nums`}
            {...register('qty', { valueAsNumber: true })}
          />
          {errors.qty && <p className="text-sm font-semibold text-bad">{errors.qty.message}</p>}
        </div>
      </div>
      <div className="grid grid-cols-[1fr_auto] gap-2">
        <button type="submit" className="h-12 rounded-lg bg-brand-600 font-semibold text-white active:bg-brand-700">
          Thêm vào giỏ
        </button>
        <button type="button" onClick={onCancel} className="h-12 rounded-lg border border-line bg-surface px-4 font-semibold">
          Thôi
        </button>
      </div>
    </form>
  )
}
