// Đo thời gian một việc bất đồng bộ (ví dụ một lần gọi API), kể cả khi việc đó lỗi.
export async function measure<T>(work: () => Promise<T>): Promise<{ ok: true; value: T; ms: number } | { ok: false; error: unknown; ms: number }> {
  const startedAt = performance.now()
  try {
    const value = await work()
    return { ok: true, value, ms: Math.round(performance.now() - startedAt) }
  } catch (error) {
    return { ok: false, error, ms: Math.round(performance.now() - startedAt) }
  }
}
