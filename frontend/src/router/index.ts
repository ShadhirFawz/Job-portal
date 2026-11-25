import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/organizations/create',
      name: 'create-organization',
      component: () => import('@/views/organizations/CreateOrganization.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug',
      name: 'organization-detail',
      component: () => import('@/views/organizations/OrganizationDetail.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug/edit',
      name: 'edit-organization',
      component: () => import('@/views/organizations/EditOrganization.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug/invite',
      name: 'invite-member',
      component: () => import('@/views/organizations/InviteMember.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/verify-email',
      name: 'verify-organization-email',
      component: () => import('@/views/organizations/VerifyEmail.vue')
    },
    {
      path: '/organizations/:slug/jobs/create',
      name: 'create-job',
      component: () => import('@/views/jobs/CreateJob.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug/questions',
      name: 'question-bank',
      component: () => import('@/views/assessments/QuestionBank.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug/quizzes/create',
      name: 'create-quiz',
      component: () => import('@/views/assessments/QuizBuilder.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/organizations/:slug/assessments',
      name: 'assessments-home',
      component: () => import('@/views/assessments/AssessmentsHome.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/jobs/:slug',
      name: 'job-detail',
      component: () => import('@/views/jobs/JobDetail.vue')
    },
    {
      path: '/jobs/:slug/edit',
      name: 'edit-job',
      component: () => import('@/views/jobs/EditJob.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/jobs/:slug/apply',
      name: 'apply-job',
      component: () => import('@/views/jobs/ApplyJob.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/jobs/:slug/assessments/:processSlug',
      name: 'take-quiz',
      component: () => import('@/views/assessments/TakeQuiz.vue'),
      meta: { requiresAuth: true, hideNavbar: true }
    },
    {
      path: '/jobs/:jobId/assessments/:processId',
      redirect: to => `/jobs/${to.params.jobId}/assessments/${to.params.processId}`
    },
    {
      path: '/jobs/:slug/applications',
      name: 'job-applications',
      component: () => import('@/views/jobs/JobApplications.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/invitations',
      name: 'my-invitations',
      component: () => import('@/views/organizations/MyInvitations.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/resumes',
      name: 'my-resumes',
      component: () => import('@/views/resumes/MyResumes.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/jobs',
      name: 'jobs-list',
      component: () => import('@/views/jobs/JobsList.vue')
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/auth/Login.vue'),
      meta: { guest: true }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/auth/Register.vue'),
      meta: { guest: true }
    },
    {
      path: '/auth/callback',
      name: 'auth-callback',
      component: () => import('@/views/auth/Callback.vue')
    },
    {
      path: '/profile',
      name: 'profile',
      component: () => import('@/views/profile/ViewProfile.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/profile/edit',
      name: 'profile-edit',
      component: () => import('@/views/profile/EditProfile.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/profile/complete',
      name: 'profile-complete',
      component: () => import('@/views/profile/CompleteProfile.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/u/:username',
      name: 'public-profile',
      component: () => import('@/views/profile/PublicProfile.vue')
    },
    {
      path: '/profile/:username',
      redirect: to => `/u/${to.params.username}`
    },
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/Home.vue'),
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

  // Force profile completion during onboarding/initial registration
  if (
    isAuthenticated &&
    !isProfileComplete &&
    to.name !== 'profile-complete' &&
    to.name !== 'auth-callback'
  ) {
    return next({ name: 'profile-complete' })
  }

  // Prevent accessing /profile/complete once profile has already been completed
  if (
    isAuthenticated &&
    isProfileComplete &&
    to.name === 'profile-complete'
  ) {
    return next({ name: 'profile' })
  }

  next()
})

export default router