import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { ROUTE_NAMES } from '@common/constants';
import { useAuthStore } from '@features/auth/stores/auth.store';

/**
 * Route meta interface for type-safe route metadata.
 */
export interface RouteMeta {
  title?: string;
  navLabel?: string; // Label shown in sidebar navigation
  requiresAuth?: boolean;
  layout?: 'default' | 'auth' | 'empty';
  breadcrumbs?: Breadcrumb[];
  hidden?: boolean; // Hide from navigation menus
}

export interface Breadcrumb {
  label: string;
  to?: string;
}

/**
 * Auth routes.
 */
const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: ROUTE_NAMES.LOGIN,
    component: () => import('@features/auth/views/LoginView.vue'),
    meta: {
      title: 'Login',
      layout: 'auth',
    },
  },
  {
    path: '/register',
    name: ROUTE_NAMES.REGISTER,
    component: () => import('@features/auth/views/RegisterView.vue'),
    meta: {
      title: 'Register',
      layout: 'auth',
    },
  },
];

/**
 * App routes.
 */
const appRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: ROUTE_NAMES.HOME,
    redirect: { name: ROUTE_NAMES.DASHBOARD },
  },
  {
    path: '/dashboard',
    name: ROUTE_NAMES.DASHBOARD,
    component: () => import('@features/dashboard/views/DashboardView.vue'),
    meta: {
      title: 'Dashboard',
      navLabel: 'Dashboard',
      requiresAuth: true,
    },
  },
  {
    path: '/workspaces',
    name: ROUTE_NAMES.WORKSPACES,
    component: () => import('@features/workspace/views/WorkspacesView.vue'),
    meta: {
      title: 'Workspaces',
      navLabel: 'Workspaces',
      requiresAuth: true,
    },
  },
  {
    path: '/projects',
    name: ROUTE_NAMES.PROJECTS,
    component: () => import('@features/projects/views/ProjectsView.vue'),
    meta: {
      title: 'Projects',
      navLabel: 'Projects',
      requiresAuth: true,
    },
  },
  {
    path: '/projects/:projectId',
    name: ROUTE_NAMES.PROJECT_DETAIL,
    component: () => import('@features/projects/views/ProjectDetailView.vue'),
    meta: {
      title: 'Project Details',
      requiresAuth: true,
    },
  },
  {
    // Redirect /projects/:projectId/boards to /projects/:projectId
    path: '/projects/:projectId/boards',
    redirect: to => ({ name: ROUTE_NAMES.PROJECT_DETAIL, params: { projectId: to.params.projectId } }),
  },
  {
    path: '/projects/:projectId/boards/:boardId',
    name: ROUTE_NAMES.KANBAN_BOARD,
    component: () => import('@features/boards/views/KanbanBoardView.vue'),
    meta: {
      title: 'Kanban Board',
      requiresAuth: true,
    },
  },
];

// Module-level promise for auth initialization - shared between router and App.vue
let authInitPromise: Promise<void> | null = null;

/**
 * Get or create the auth initialization promise.
 * This ensures auth is initialized only once.
 */
export function getAuthInitPromise(): Promise<void> {
  if (!authInitPromise) {
    const authStore = useAuthStore();
    authInitPromise = authStore.initializeFromStorage();
  }
  return authInitPromise;
}

/**
 * Create and configure the router.
 */
export function createAppRouter(): ReturnType<typeof createRouter> {
  const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [...authRoutes, ...appRoutes],
    scrollBehavior(to, _from, savedPosition) {
      if (savedPosition) {
        return savedPosition;
      }
      if (to.hash) {
        return { el: to.hash };
      }
      return { top: 0 };
    },
  });

  // Navigation guard for authentication
  router.beforeEach(async (to) => {
    const authStore = useAuthStore();
    const requiresAuth = (to.meta as RouteMeta)?.requiresAuth;

    // Wait for auth initialization if not done yet
    if (!authStore.isInitialized) {
      await getAuthInitPromise();
    }

    // Now check auth
    if (requiresAuth && !authStore.accessToken) {
      return { name: ROUTE_NAMES.LOGIN, query: { redirect: to.fullPath } };
    }

    if ((to.name === ROUTE_NAMES.LOGIN || to.name === ROUTE_NAMES.REGISTER) && authStore.accessToken) {
      return { name: ROUTE_NAMES.DASHBOARD };
    }

    return true;
  });

  // Update document title on navigation
  router.afterEach((to) => {
    const title = (to.meta as RouteMeta)?.title;
    document.title = title ? `${title} | Project Management` : 'Project Management';
  });

  return router;
}

// Page transition name for Vue Router
export const PAGE_TRANSITION_NAME = 'page';

export type AppRouter = ReturnType<typeof createAppRouter>;
