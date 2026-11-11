import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { holdsApi } from '@/api/holds'
import { classKeys } from './useClasses'

export const holdKeys = {
  byId: (id: string) => ['holds', id] as const,
  byStudent: (studentId: string) => ['holds', 'student', studentId] as const,
}

export function useHold(holdId: string) {
  return useQuery({
    queryKey: holdKeys.byId(holdId),
    queryFn: () => holdsApi.getById(holdId),
    enabled: !!holdId,
    refetchInterval: 3_000,
    staleTime: 1_000,
  })
}

export function useStudentHolds(studentId: string) {
  return useQuery({
    queryKey: holdKeys.byStudent(studentId),
    queryFn: () => holdsApi.getMyHolds(studentId),
    enabled: /^\d{17}$/.test(studentId),
    refetchInterval: 5_000,
    staleTime: 2_000,
  })
}

export function useConfirmHold() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ holdId, studentId }: { holdId: string; studentId: string }) =>
      holdsApi.confirm(holdId, studentId),
    onSuccess: (_data, { holdId }) => {
      qc.invalidateQueries({ queryKey: holdKeys.byId(holdId) })
      qc.invalidateQueries({ queryKey: ['holds', 'student'] })
      qc.invalidateQueries({ queryKey: classKeys.all })
    },
  })
}

export function useCancelHold() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ holdId, studentId }: { holdId: string; studentId: string }) =>
      holdsApi.cancel(holdId, studentId),
    onSuccess: (_data, { holdId }) => {
      qc.invalidateQueries({ queryKey: holdKeys.byId(holdId) })
      qc.invalidateQueries({ queryKey: ['holds', 'student'] })
      qc.invalidateQueries({ queryKey: classKeys.all })
    },
  })
}
