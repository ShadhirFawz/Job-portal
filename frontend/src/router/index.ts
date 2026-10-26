import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/organizations/create',
      name: 'create-organization',
      component: () => import('@/pages/organizations/CreateOrganization.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:id',
      name: 'organization-detail',
      component: () => import('@/pages/organizations/OrganizationDetail.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:id/invite',
      name: 'invite-member',
      component: () => import('@/pages/organizations/InviteMember.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/auth/Login.vue'),
      meta: { guest: true }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/pages/auth/Register.vue'),
      meta: { guest: true }
    },
    {
      path: '/auth/callback',
      name: 'auth-callback',
      component: () => import('@/pages/auth/Callback.vue')
    },
    {
      path: '/profile/complete',
      name: 'profile-complete',
      component: () => import('@/pages/profile/CompleteProfile.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/',
      name: 'home',
      component: () => import('@/pages/Home.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/'
    }
  ]
})

router.beforeEach(async (to, from, next) => {
  const auth = useAuthStore()

  // Wait until auth is initialized
  if (auth.loading) {
    await auth.init()
  }

  const isAuthenticated = auth.isAuthenticated
  const isProfileComplete = auth.user?.is_profile_complete ?? false

  if (to.meta.requiresAuth && !isAuthenticated) {
    return next({ name: 'login', query: { redirect: to.fullPath } })
  }

  if (to.meta.guest && isAuthenticated) {
    return next({ name: 'home' })
  }

  // Force profile completion
  if (
    isAuthenticated &&
    !isProfileComplete &&
    to.name !== 'profile-complete' &&
    to.name !== 'auth-callback'
  ) {
    return next({ name: 'profile-complete' })
  }

  next()
})

export default router