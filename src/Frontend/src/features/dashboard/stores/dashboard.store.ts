import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { DashboardResponse, RecentActivityItem, UpcomingTaskItem } from '../types/dashboard.types';
import { createDashboardService } from '../services/dashboard.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';

/**
 * Dashboard store for managing dashboard state.
 */
export const useDashboardStore = defineStore('dashboard', () => {
  // State
  const dashboard = ref<DashboardResponse | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service instance
  const apiClient = createApiClient();
  const dashboardService = createDashboardService(apiClient);

  // Getters
  const activeProjects = () => dashboard.value?.activeProjects ?? 0;
  const tasksDueThisWeek = () => dashboard.value?.tasksDueThisWeek ?? 0;
  const completedTasksThisMonth = () => dashboard.value?.completedTasksThisMonth ?? 0;
  const teamMembers = () => dashboard.value?.teamMembers ?? 0;
  const recentActivities = (): RecentActivityItem[] => dashboard.value?.recentActivities ?? [];
  const upcomingTasks = (): UpcomingTaskItem[] => dashboard.value?.upcomingTasks ?? [];

  // Actions
  async function fetchDashboard(): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      dashboard.value = await dashboardService.getDashboard();
    } catch (err) {
      error.value = 'Failed to fetch dashboard data';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch dashboard data' });
    } finally {
      isLoading.value = false;
    }
  }

  function clearError(): void {
    error.value = null;
  }

  return {
    // State
    dashboard,
    isLoading,
    error,

    // Getters
    activeProjects,
    tasksDueThisWeek,
    completedTasksThisMonth,
    teamMembers,
    recentActivities,
    upcomingTasks,

    // Actions
    fetchDashboard,
    clearError,
  };
});

// Type for store
export type DashboardStore = ReturnType<typeof useDashboardStore>;
