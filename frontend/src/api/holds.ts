import { apiClient, withStudentId } from './client'
import type {
  HoldStatusResponse,
  ConfirmHoldResponse,
  StudentHoldResponse,
} from '@/types'

export const holdsApi = {
  getById: async (holdId: string): Promise<HoldStatusResponse> => {
    const { data } = await apiClient.get<HoldStatusResponse>(`/holds/${holdId}`)
    return data
  },

  confirm: async (holdId: string, studentId: string): Promise<ConfirmHoldResponse> => {
    const { data } = await apiClient.post<ConfirmHoldResponse>(
      `/holds/${holdId}/confirm`,
      {},
      withStudentId(studentId)
    )
    return data
  },

  cancel: async (holdId: string, studentId: string): Promise<void> => {
    await apiClient.delete(`/holds/${holdId}`, withStudentId(studentId))
  },

  getMyHolds: async (studentId: string): Promise<StudentHoldResponse[]> => {
    const { data } = await apiClient.get<StudentHoldResponse[]>(
      '/me/holds',
      withStudentId(studentId)
    )
    return data
  },
}
