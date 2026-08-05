import axios, { type AxiosError } from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import type { ApiResponse } from '@/types/api'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/admin/v1',
  timeout: 15000
})

let redirectingToLogin = false

http.interceptors.request.use(config => {
  const token = localStorage.getItem('lemon.accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

http.interceptors.response.use(
  response => {
    const payload = response.data as ApiResponse
    if (payload && payload.code && payload.code !== '0') return Promise.reject(new Error(payload.message))
    return payload?.data ?? response.data
  },
  (error: AxiosError<ApiResponse>) => {
    if (error.response?.status === 401 && !redirectingToLogin) {
      redirectingToLogin = true
      localStorage.removeItem('lemon.accessToken')
      localStorage.removeItem('lemon.auth')
      const loginUrl = router.resolve('/login').href
      window.location.replace(loginUrl)
    }

    const message = error.response?.data?.message || error.message || '请求失败'
    if (error.response?.status !== 401) ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default http
