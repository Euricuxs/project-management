import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  WorkspaceListItemResponse,
  WorkspaceResponse,
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
} from '../types/workspace.types';
import { createWorkspaceService } from '../services/workspace.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';

/**
 * Workspace store for managing workspace state.
 */
export const useWorkspaceStore = defineStore('workspace', () => {
  // State
  const workspaces = ref<WorkspaceListItemResponse[]>([]);
  const currentWorkspace = ref<WorkspaceResponse | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service instance
  const apiClient = createApiClient();
  const workspaceService = createWorkspaceService(apiClient);

  // Getters
  const hasWorkspaces = computed(() => workspaces.value.length > 0);
  const workspaceCount = computed(() => workspaces.value.length);

  // Actions
  async function fetchWorkspaces(): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      workspaces.value = await workspaceService.getWorkspaces();
    } catch (err) {
      error.value = 'Failed to fetch workspaces';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch workspaces' });
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchWorkspace(id: string): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      currentWorkspace.value = await workspaceService.getWorkspace(id);
    } catch (err) {
      error.value = 'Failed to fetch workspace';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch workspace' });
    } finally {
      isLoading.value = false;
    }
  }

  async function createWorkspace(data: CreateWorkspaceRequest): Promise<WorkspaceResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const workspace = await workspaceService.createWorkspace(data);
      workspaces.value.push({
        id: workspace.id,
        name: workspace.name,
        logoUrl: workspace.logoUrl,
        isPublic: workspace.isPublic,
        role: 'Owner',
        memberCount: workspace.memberCount,
        projectCount: workspace.projectCount,
        createdAt: workspace.createdAt,
      });
      return workspace;
    } catch (err) {
      error.value = 'Failed to create workspace';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function updateWorkspace(id: string, data: UpdateWorkspaceRequest): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      const workspace = await workspaceService.updateWorkspace(id, data);

      // Update in list
      const index = workspaces.value.findIndex((w) => w.id === id);
      const existingWorkspace = workspaces.value[index];
      if (index !== -1 && existingWorkspace) {
        // Create new object with all required fields
        const updatedWorkspace: WorkspaceListItemResponse = {
          id: existingWorkspace.id,
          name: workspace.name,
          logoUrl: workspace.logoUrl,
          isPublic: workspace.isPublic,
          role: existingWorkspace.role,
          memberCount: existingWorkspace.memberCount,
          projectCount: existingWorkspace.projectCount,
          createdAt: existingWorkspace.createdAt,
        };
        workspaces.value[index] = updatedWorkspace;
      }

      // Update current workspace
      if (currentWorkspace.value?.id === id) {
        currentWorkspace.value = workspace;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to update workspace';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteWorkspace(id: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await workspaceService.deleteWorkspace(id);
      workspaces.value = workspaces.value.filter((w) => w.id !== id);

      if (currentWorkspace.value?.id === id) {
        currentWorkspace.value = null;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to delete workspace';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  function setCurrentWorkspace(workspace: WorkspaceResponse | null): void {
    currentWorkspace.value = workspace;
  }

  function clearError(): void {
    error.value = null;
  }

  return {
    // State
    workspaces,
    currentWorkspace,
    isLoading,
    error,

    // Getters
    hasWorkspaces,
    workspaceCount,

    // Actions
    fetchWorkspaces,
    fetchWorkspace,
    createWorkspace,
    updateWorkspace,
    deleteWorkspace,
    setCurrentWorkspace,
    clearError,
  };
});

// Type for store
export type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;