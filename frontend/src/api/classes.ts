import { apiClient, withStudentId } from './client'
import type {
  SchoolClass,
  ClassCreatedResponse,
  CreateClassRequest,
  PlaceHoldResponse,
} from '@/types'

export const classesApi = {
  getAll: async (): Promise<SchoolClass[]> => {
    const { data } = await apiClient.get<SchoolClass[]>('/classes')
    return data
  },

  getById: async (classId: string): Promise<SchoolClass> => {
    const { data } = await apiClient.get<SchoolClass>(`/classes/${classId}`)
    return data
  },

  create: async (payload: CreateClassRequest): Promise<ClassCreatedResponse> => {
    const { data } = await apiClient.post<ClassCreatedResponse>('/classes', payload)
    return data
  },

  placeHold: async (classId: string, studentId: string): Promise<PlaceHoldResponse> => {
    const { data } = await apiClient.post<PlaceHoldResponse>(
      `/classes/${classId}/holds`,
      {},
      withStudentId(studentId)
    )
    return data
  },
}
