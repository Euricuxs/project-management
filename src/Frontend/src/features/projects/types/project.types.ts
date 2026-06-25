/**
 * Project types for TypeScript type safety.
 */

export interface CreateProjectRequest {
  workspaceId: string;
  name: string;
  description?: string;
  key?: string;
  color?: string;
  startDate?: string;
  endDate?: string;
}

export interface UpdateProjectRequest {
  name: string;
  description?: string;
  key?: string;
  color?: string;
  status: string;
  startDate?: string;
  endDate?: string;
}

export interface ProjectResponse {
  id: string;
  workspaceId: string;
  name: string;
  description?: string;
  key?: string;
  status: string;
  color: string;
  startDate?: string;
  endDate?: string;
  iconUrl?: string;
  boardCount: number;
  taskCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface ProjectListItemResponse {
  id: string;
  workspaceId: string;
  name: string;
  key?: string;
  status: string;
  color: string;
  boardCount: number;
  taskCount: number;
  createdAt: string;
}

export interface ProjectState {
  projects: ProjectListItemResponse[];
  currentProject: ProjectResponse | null;
  selectedWorkspaceId: string | null;
  isLoading: boolean;
  error: string | null;
}

export const PROJECT_STATUSES = [
  { value: 'Planning', label: 'Planning', color: '#6b7280' },
  { value: 'Active', label: 'Active', color: '#10b981' },
  { value: 'OnHold', label: 'On Hold', color: '#f59e0b' },
  { value: 'Completed', label: 'Completed', color: '#3b82f6' },
  { value: 'Archived', label: 'Archived', color: '#94a3b8' },
] as const;

export type ProjectStatus = typeof PROJECT_STATUSES[number]['value'];
