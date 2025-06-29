import api from '@/api/api'
import { router } from '@/plugins/router'
import { AuthenticatedUser } from '@/types/AuthenticatedUser'

export const alfaReports = async () => {
  try {
    const response = await api.get('/tempestive/alfa-reports')
    return response.data
  } catch (error) {
    console.error('Error fetching Alfa reports:', error)
    throw error
  }
}