import { useQuery } from '@tanstack/react-query'
import { getLocations } from '../../../api/catalog'

// Query key của chủ đề Location.
export const locationKeys = {
  all: ['locations'] as const,
}

// Vị trí đang dùng (kệ bán, kho sau...). Tiệm có vài vị trí, ít đổi: tải một lần, tươi 5 phút.
export function useLocations() {
  return useQuery({
    queryKey: locationKeys.all,
    queryFn: ({ signal }) => getLocations(signal),
    staleTime: 5 * 60_000,
  })
}
