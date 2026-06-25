import { defineStore } from 'pinia';
import { ref } from 'vue';
import type {
  TaskResponse,
  TaskListItemResponse,
  CreateTaskRequest,
  UpdateTaskRequest,
  MoveTaskRequest,
} from '../types/task.types';
import { createTaskService } from '../services/task.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';

/**
 * Task store for managing task state within a board context.
 */
export const useTaskStore = defineStore('task', () => {
  // State
  const tasks = ref<TaskListItemResponse[]>([]);
  const currentTask = ref<TaskResponse | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service instance
  const apiClient = createApiClient();
  const taskService = createTaskService(apiClient);

  // Actions
  async function fetchTask(taskId: string): Promise<TaskResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const task = await taskService.getTask(taskId);
      currentTask.value = task;
      return task;
    } catch (err) {
      error.value = 'Failed to fetch task';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch task' });
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchTasksByColumn(columnId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      tasks.value = await taskService.getTasksByColumn(columnId);
    } catch (err) {
      error.value = 'Failed to fetch tasks';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch tasks' });
    } finally {
      isLoading.value = false;
    }
  }

  async function createTask(data: CreateTaskRequest): Promise<TaskResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const task = await taskService.createTask(data);
      // Add to tasks list
      tasks.value.push({
        id: task.id,
        columnId: task.columnId,
        title: task.title,
        taskKey: task.taskKey,
        status: task.status,
        priority: task.priority,
        position: task.position,
        dueDate: task.dueDate,
        assigneeId: task.assigneeId,
        assigneeName: task.assigneeName,
        createdAt: task.createdAt,
      });
      return task;
    } catch (err) {
      error.value = 'Failed to create task';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function updateTask(taskId: string, data: UpdateTaskRequest): Promise<TaskResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const task = await taskService.updateTask(taskId, data);

      // Update in list
      const index = tasks.value.findIndex((t) => t.id === taskId);
      if (index !== -1) {
        tasks.value[index] = {
          id: task.id,
          columnId: task.columnId,
          title: task.title,
          taskKey: task.taskKey,
          status: task.status,
          priority: task.priority,
          position: task.position,
          dueDate: task.dueDate,
          assigneeId: task.assigneeId,
          assigneeName: task.assigneeName,
          createdAt: task.createdAt,
        };
      }

      // Update current task
      if (currentTask.value?.id === taskId) {
        currentTask.value = task;
      }

      return task;
    } catch (err) {
      error.value = 'Failed to update task';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteTask(taskId: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await taskService.deleteTask(taskId);

      // Remove from list
      tasks.value = tasks.value.filter((t) => t.id !== taskId);

      // Clear current task if deleted
      if (currentTask.value?.id === taskId) {
        currentTask.value = null;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to delete task';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function moveTask(taskId: string, data: MoveTaskRequest): Promise<TaskResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const task = await taskService.moveTask(taskId, data);

      // Update in list if exists
      const index = tasks.value.findIndex((t) => t.id === taskId);
      if (index !== -1) {
        tasks.value[index] = {
          id: task.id,
          columnId: task.columnId,
          title: task.title,
          taskKey: task.taskKey,
          status: task.status,
          priority: task.priority,
          position: task.position,
          dueDate: task.dueDate,
          assigneeId: task.assigneeId,
          assigneeName: task.assigneeName,
          createdAt: task.createdAt,
        };
      }

      // Update current task if moved
      if (currentTask.value?.id === taskId) {
        currentTask.value = task;
      }

      return task;
    } catch (err) {
      error.value = 'Failed to move task';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  function clearTasks(): void {
    tasks.value = [];
    currentTask.value = null;
    error.value = null;
  }

  return {
    // State
    tasks,
    currentTask,
    isLoading,
    error,

    // Actions
    fetchTask,
    fetchTasksByColumn,
    createTask,
    updateTask,
    deleteTask,
    moveTask,
    clearTasks,
  };
});
