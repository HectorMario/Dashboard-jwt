import type { App } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { routes } from './routes'
import { fetchCurrentUser } from '@/api/authentication'

const publicPages = ['login', 'forgot-password']

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Async Navigation Guard for authentication
router.beforeEach(async (to, from, next) => {

  const isPublicPage = publicPages.includes(to.name as string)

  if (!isPublicPage) {

    try {
      const user = await fetchCurrentUser()
      const isLoggedIn = !!user

      if (!isLoggedIn && !isPublicPage) {
        return next({ name: 'login', query: { redirect: to.fullPath } })
      }

      next()
    } catch (error) {
      console.error('Auth check failed:', error)
      if (!isPublicPage) {
        return next({ name: 'login', query: { redirect: to.fullPath } })
      }
      next()
    }
  } else {
    next()
  }
})

export default function installRouter(app: App) {
  app.use(router)
}

export { router }
