import api from '@/api/api'
import { router } from '@/plugins/router'
import { AuthenticatedUser } from '@/types/AuthenticatedUser'

export const login = async (email: string, password: string) => {
  try {
    api.post('/auth/login', { email, password })
    const redirectTo = router.currentRoute.value.query.redirect || '/dashboard'
    console.log(redirectTo)
    router.push(redirectTo as string)
  } catch (error) {
    console.error('Error during login:', error)
    throw error
  }
}

export const logout = async () => {
  try {
    const response = await api.post('/auth/logout')
    return response.data
  } catch (error) {
    console.error('Error during logout:', error)
    throw error
  }
}

export const fetchCurrentUser = async (): Promise<AuthenticatedUser | null> => {
  try {
    const response = await api.get<AuthenticatedUser>('/auth/me')
    return response.data
  } catch (error) {
    if ((error as any).response?.status === 401) {
      return null
    }
    throw error
  }
}

