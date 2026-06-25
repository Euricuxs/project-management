import type { AxiosInstance } from 'axios';
import type {
  CreateProjectRequest,
  UpdateProjectRequest,
  ProjectResponse,
  ProjectListItemResponse,
} from '../types/project.types';

/**
 * Creates the project service for API calls.
 */
export function createProjectService(client: AxiosInstance) {
  return {
    /**
     * Get all projects for a workspace.
     */
    async getProjects(workspaceId: string, includeArchived = false): Promise<ProjectListItemResponse[]> {
      const response = await client.get<{ data: ProjectListItemResponse[] }>(
        `/projects/workspace/${workspaceId}`,
        { params: { includeArchived } }
      );
      return response.data.data;
    },

    /**
     * Get a project by ID.
     */
    async getProject(id: string): Promise<ProjectResponse> {
      const response = await client.get<{ data: ProjectResponse }>(`/projects/${id}`);
      return response.data.data;
    },

    /**
     * Create a new project.
     */
    async createProject(data: CreateProjectRequest): Promise<ProjectResponse> {
      const response = await client.post<{ data: ProjectResponse }>('/projects', data);
      return response.data.data;
    },

    /**
     * Update an existing project.
     */
    async updateProject(id: string, data: UpdateProjectRequest): Promise<ProjectResponse> {
      const response = await client.put<{ data: ProjectResponse }>(`/projects/${id}`, data);
      return response.data.data;
    },

    /**
     * Archive a project.
     */
    async archiveProject(id: string): Promise<void> {
      await client.post(`/projects/${id}/archive`);
    },

    /**
     * Restore an archived project.
     */
    async restoreProject(id: string): Promise<void> {
      await client.post(`/projects/${id}/restore`);
    },

    /**
     * Delete a project.
     */
    async deleteProject(id: string): Promise<void> {
      await client.delete(`/projects/${id}`);
    },
  };
}

export type ProjectService = ReturnType<typeof createProjectService>;
