import type { AxiosInstance } from 'axios';
import type {
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
  WorkspaceResponse,
  WorkspaceListItemResponse,
} from '../types/workspace.types';

/**
 * Creates the workspace service for API calls.
 */
export function createWorkspaceService(client: AxiosInstance) {
  return {
    /**
     * Get all workspaces for the current user.
     */
    async getWorkspaces(): Promise<WorkspaceListItemResponse[]> {
      const response = await client.get<{ data: WorkspaceListItemResponse[] }>('/workspaces');
      return response.data.data;
    },

    /**
     * Get a workspace by ID.
     */
    async getWorkspace(id: string): Promise<WorkspaceResponse> {
      const response = await client.get<{ data: WorkspaceResponse }>(`/workspaces/${id}`);
      return response.data.data;
    },

    /**
     * Create a new workspace.
     */
    async createWorkspace(data: CreateWorkspaceRequest): Promise<WorkspaceResponse> {
      const response = await client.post<{ data: WorkspaceResponse }>('/workspaces', data);
      return response.data.data;
    },

    /**
     * Update an existing workspace.
     */
    async updateWorkspace(id: string, data: UpdateWorkspaceRequest): Promise<WorkspaceResponse> {
      const response = await client.put<{ data: WorkspaceResponse }>(`/workspaces/${id}`, data);
      return response.data.data;
    },

    /**
     * Delete a workspace (soft delete).
     */
    async deleteWorkspace(id: string): Promise<void> {
      await client.delete(`/workspaces/${id}`);
    },
  };
}

export type WorkspaceService = ReturnType<typeof createWorkspaceService>;