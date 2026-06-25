import type { AxiosInstance } from 'axios';
import type {
  LabelResponse,
  LabelListItemResponse,
  CreateLabelRequest,
  UpdateLabelRequest,
  AddLabelsToTaskRequest,
} from '../types/label.types';

/**
 * Creates the label service for API calls.
 */
export function createLabelService(client: AxiosInstance) {
  return {
    /**
     * Get all labels for a project.
     */
    async getProjectLabels(projectId: string): Promise<LabelListItemResponse[]> {
      const response = await client.get<{ data: LabelListItemResponse[] }>(
        `/projects/${projectId}/labels`
      );
      return response.data.data;
    },

    /**
     * Get a single label by ID.
     */
    async getLabel(projectId: string, labelId: string): Promise<LabelResponse> {
      const response = await client.get<{ data: LabelResponse }>(
        `/projects/${projectId}/labels/${labelId}`
      );
      return response.data.data;
    },

    /**
     * Create a new label.
     */
    async createLabel(projectId: string, data: CreateLabelRequest): Promise<LabelResponse> {
      const response = await client.post<{ data: LabelResponse }>(
        `/projects/${projectId}/labels`,
        data
      );
      return response.data.data;
    },

    /**
     * Update an existing label.
     */
    async updateLabel(
      projectId: string,
      labelId: string,
      data: UpdateLabelRequest
    ): Promise<LabelResponse> {
      const response = await client.put<{ data: LabelResponse }>(
        `/projects/${projectId}/labels/${labelId}`,
        data
      );
      return response.data.data;
    },

    /**
     * Delete a label.
     */
    async deleteLabel(projectId: string, labelId: string): Promise<void> {
      await client.delete(`/projects/${projectId}/labels/${labelId}`);
    },

    /**
     * Get all labels for a task.
     */
    async getTaskLabels(taskId: string): Promise<LabelListItemResponse[]> {
      const response = await client.get<{ data: LabelListItemResponse[] }>(
        `/tasks/${taskId}/labels`
      );
      return response.data.data;
    },

    /**
     * Add labels to a task.
     */
    async addLabelsToTask(taskId: string, data: AddLabelsToTaskRequest): Promise<void> {
      await client.post(`/tasks/${taskId}/labels`, data);
    },

    /**
     * Remove a label from a task.
     */
    async removeLabelFromTask(taskId: string, labelId: string): Promise<void> {
      await client.delete(`/tasks/${taskId}/labels/${labelId}`);
    },
  };
}

export type LabelService = ReturnType<typeof createLabelService>;
