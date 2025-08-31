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

export const salvaAlfaReport = async (reportData: any) => {
  try {
    const response = await api.post('/tempestive/alfasreports', reportData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      responseType: "blob"
    })
    return response.data
  } catch (error) {
    console.error('Error saving Alfa report:', error)
    throw error
  }
}