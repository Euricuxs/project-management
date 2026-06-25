/**
 * Types for dashboard data.
 */

export interface RecentActivityItem {
  id: string;
  type: string;
  description: string;
  projectName?: string;
  projectId?: string;
  createdAt: string;
}

export interface UpcomingTaskItem {
  id: string;
  title: string;
  projectName?: string;
  projectId?: string;
  dueDate: string;
  status: string;
}

export interface DashboardResponse {
  activeProjects: number;
  completedProjects: number;
  tasksDueThisWeek: number;
  completedTasksThisMonth: number;
  teamMembers: number;
  recentActivities: RecentActivityItem[];
  upcomingTasks: UpcomingTaskItem[];
}
