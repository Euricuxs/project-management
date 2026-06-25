import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  LabelResponse,
  LabelListItemResponse,
  CreateLabelRequest,
  UpdateLabelRequest,
} from '../types/label.types';
import { createLabelService } from '../services/label.service';
import { createApiClient } from '@core/api/client';

export const useLabelStore = defineStore('label', () => {
  // State - organized by projectId for caching
  const labelsByProject = ref<Map<string, LabelListItemResponse[]>>(new Map());
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service
  const apiClient = createApiClient();
  const labelService = createLabelService(apiClient);

  // Getters
  const getProjectLabels = computed(() => (projectId: string): LabelListItemResponse[] => {
    return labelsByProject.value.get(projectId) || [];
  });

  // Actions
  async function fetchProjectLabels(projectId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      const labels = await labelService.getProjectLabels(projectId);
      labelsByProject.value.set(projectId, labels);
    } catch (err) {
      error.value = 'Failed to fetch labels';
      console.error('Failed to fetch labels:', err);
    } finally {
      isLoading.value = false;
    }
  }

  async function createLabel(
    projectId: string,
    data: CreateLabelRequest
  ): Promise<LabelResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const label = await labelService.createLabel(projectId, data);
      const current = labelsByProject.value.get(projectId) || [];
      labelsByProject.value.set(projectId, [
        ...current,
        {
          id: label.id,
          projectId: label.projectId,
          name: label.name,
          color: label.color,
          taskCount: label.taskCount,
        },
      ]);
      return label;
    } catch (err) {
      error.value = 'Failed to create label';
      console.error('Failed to create label:', err);
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function updateLabel(
    projectId: string,
    labelId: string,
    data: UpdateLabelRequest
  ): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      const label = await labelService.updateLabel(projectId, labelId, data);
      const current = labelsByProject.value.get(projectId) || [];
      const index = current.findIndex((l) => l.id === labelId);
      if (index !== -1) {
        current[index] = {
          id: label.id,
          projectId: label.projectId,
          name: label.name,
          color: label.color,
          taskCount: label.taskCount,
        };
        labelsByProject.value.set(projectId, [...current]);
      }
      return true;
    } catch (err) {
      error.value = 'Failed to update label';
      console.error('Failed to update label:', err);
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteLabel(projectId: string, labelId: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await labelService.deleteLabel(projectId, labelId);
      const current = labelsByProject.value.get(projectId) || [];
      labelsByProject.value.set(
        projectId,
        current.filter((l) => l.id !== labelId)
      );
      return true;
    } catch (err) {
      error.value = 'Failed to delete label';
      console.error('Failed to delete label:', err);
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function addLabelsToTask(taskId: string, labelIds: string[]): Promise<boolean> {
    try {
      await labelService.addLabelsToTask(taskId, { labelIds });
      return true;
    } catch (err) {
      console.error('Failed to add labels to task:', err);
      return false;
    }
  }

  async function removeLabelFromTask(taskId: string, labelId: string): Promise<boolean> {
    try {
      await labelService.removeLabelFromTask(taskId, labelId);
      return true;
    } catch (err) {
      console.error('Failed to remove label from task:', err);
      return false;
    }
  }

  function clearProjectLabels(projectId: string): void {
    labelsByProject.value.delete(projectId);
  }

  return {
    // State
    labelsByProject,
    isLoading,
    error,

    // Getters
    getProjectLabels,

    // Actions
    fetchProjectLabels,
    createLabel,
    updateLabel,
    deleteLabel,
    addLabelsToTask,
    removeLabelFromTask,
    clearProjectLabels,
  };
});

export type LabelStore = ReturnType<typeof useLabelStore>;
