import axios from 'axios'
import { router } from '@/plugins/router' // Make sure you export the router instance

const api = axios.create({
  baseURL: 'http://localhost:5219/api',
  withCredentials: true, // Enables sending/receiving HttpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
})

// Optional: request interceptor (e.g., to show a loading indicator)
api.interceptors.request.use(
  config => {
    // You can add logic here before the request is sent
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// Response interceptor
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      console.warn('Unauthorized. Redirecting to login...')
      router.push({ name: 'login' }) 
    }

    return Promise.reject(error)
  }
)

export default api
