import axios, { AxiosError } from 'axios'
import type { ApiError } from '@/types'

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080'

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15_000,
})

// ── Response interceptor — normalise errors ───────────────────────────────────
apiClient.interceptors.response.use(
  (res) => res,
  (error: AxiosError<ApiError>) => {
    if (error.response?.data) {
      return Promise.reject(error.response.data as ApiError)
    }
    if (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK') {
      const apiError: ApiError = {
        title: 'Connection Error',
        detail: 'Unable to reach the server. Please check your connection and try again.',
        status: 0,
      }
      return Promise.reject(apiError)
    }
    return Promise.reject({
      title: 'Unexpected Error',
      detail: error.message || 'Something went wrong.',
      status: 500,
    } satisfies ApiError)
  }
)

export function withStudentId(studentId: string) {
  return { headers: { 'X-Student-Id': studentId } }
}
