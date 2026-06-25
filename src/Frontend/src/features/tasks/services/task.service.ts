import type { AxiosInstance } from 'axios';
import type {
  TaskResponse,
  TaskListItemResponse,
  CreateTaskRequest,
  UpdateTaskRequest,
  MoveTaskRequest,
} from '../types/task.types';

/**
 * Creates the task service for API calls.
 */
export function createTaskService(client: AxiosInstance) {
  return {
    /**
     * Get a task by ID.
     */
    async getTask(taskId: string): Promise<TaskResponse> {
      const response = await client.get<{ data: TaskResponse }>(`/tasks/${taskId}`);
      return response.data.data;
    },

    /**
     * Get all tasks for a column.
     */
    async getTasksByColumn(columnId: string): Promise<TaskListItemResponse[]> {
      const response = await client.get<{ data: TaskListItemResponse[] }>(
        `/tasks/column/${columnId}`
      );
      return response.data.data;
    },

    /**
     * Get all tasks for a board.
     */
    async getTasksByBoard(boardId: string): Promise<TaskListItemResponse[]> {
      const response = await client.get<{ data: TaskListItemResponse[] }>(
        `/tasks/board/${boardId}`
      );
      return response.data.data;
    },

    /**
     * Create a new task.
     */
    async createTask(data: CreateTaskRequest): Promise<TaskResponse> {
      const response = await client.post<{ data: TaskResponse }>('/tasks', data);
      return response.data.data;
    },

    /**
     * Update an existing task.
     */
    async updateTask(taskId: string, data: UpdateTaskRequest): Promise<TaskResponse> {
      const response = await client.put<{ data: TaskResponse }>(`/tasks/${taskId}`, data);
      return response.data.data;
    },

    /**
     * Delete a task.
     */
    async deleteTask(taskId: string): Promise<void> {
      await client.delete(`/tasks/${taskId}`);
    },

    /**
     * Move a task to a different column/position.
     */
    async moveTask(taskId: string, data: MoveTaskRequest): Promise<TaskResponse> {
      const response = await client.post<{ data: TaskResponse }>(
        `/tasks/${taskId}/move`,
        data
      );
      return response.data.data;
    },
  };
}

export type TaskService = ReturnType<typeof createTaskService>;
