import { useQuery } from '@tanstack/react-query'
import { searchSuppliers } from '../../../api/catalog'

// Query key của chủ đề Supplier.
export const supplierKeys = {
  all: ['suppliers'] as const,
}

// Mọi nhà cung cấp đang hợp tác (tiệm nhỏ chỉ có vài chục): tải một lần, lọc tại chỗ. Ít đổi nên tươi 5 phút.
export function useSuppliers() {
  return useQuery({
    queryKey: supplierKeys.all,
    queryFn: ({ signal }) => searchSuppliers(undefined, signal),
    staleTime: 5 * 60_000,
  })
}
