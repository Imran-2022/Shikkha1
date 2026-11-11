import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { classesApi } from '@/api/classes'
import type { CreateClassRequest } from '@/types'

export const classKeys = {
  all: ['classes'] as const,
  detail: (id: string) => ['classes', id] as const,
}

export function useClasses() {
  return useQuery({
    queryKey: classKeys.all,
    queryFn: classesApi.getAll,
    refetchInterval: 5_000, // poll every 5 s for live seat count updates
    staleTime: 2_000,
  })
}

export function useClass(classId: string) {
  return useQuery({
    queryKey: classKeys.detail(classId),
    queryFn: () => classesApi.getById(classId),
    refetchInterval: 5_000,
    staleTime: 2_000,
    enabled: !!classId,
  })
}

export function useCreateClass() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateClassRequest) => classesApi.create(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: classKeys.all })
    },
  })
}

export function usePlaceHold(classId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (studentId: string) => classesApi.placeHold(classId, studentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: classKeys.all })
      qc.invalidateQueries({ queryKey: classKeys.detail(classId) })
    },
  })
}
