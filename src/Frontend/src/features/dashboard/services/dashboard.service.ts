import type { AxiosInstance } from 'axios';
import type { DashboardResponse } from '../types/dashboard.types';

/**
 * Creates the dashboard service for API calls.
 */
export function createDashboardService(client: AxiosInstance) {
  return {
    /**
     * Get dashboard statistics.
     */
    async getDashboard(): Promise<DashboardResponse> {
      const response = await client.get<{ data: DashboardResponse }>('/dashboard');
      return response.data.data;
    },
  };
}

export type DashboardService = ReturnType<typeof createDashboardService>;
