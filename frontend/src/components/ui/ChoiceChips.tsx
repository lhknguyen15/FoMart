export interface Choice<T extends string> {
  value: T | '' // '' = "tất cả"
  label: string
}

interface ChoiceChipsProps<T extends string> {
  label: string
  choices: Choice<T>[]
  value: T | ''
  onChange: (value: T | '') => void
}

// Một hàng nút chọn một (thay cho <select>: bấm một chạm trên điện thoại, thấy hết lựa chọn).
// Nút đang chọn có aria-pressed, nền xanh và chữ trắng (không chỉ đổi màu viền).
export function ChoiceChips<T extends string>({ label, choices, value, onChange }: ChoiceChipsProps<T>) {
  return (
    <fieldset className="flex flex-col gap-1">
      <legend className="text-sm font-semibold">{label}</legend>
      <div className="flex flex-wrap gap-2">
        {choices.map((choice) => (
          <button
            key={choice.value || 'all'}
            type="button"
            aria-pressed={choice.value === value}
            onClick={() => onChange(choice.value)}
            className="min-h-11 rounded-lg border border-line bg-surface px-3 text-sm font-semibold aria-pressed:border-brand-600 aria-pressed:bg-brand-600 aria-pressed:text-white"
          >
            {choice.label}
          </button>
        ))}
      </div>
    </fieldset>
  )
}
