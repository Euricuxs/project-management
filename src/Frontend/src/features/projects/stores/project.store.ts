import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  ProjectListItemResponse,
  ProjectResponse,
  CreateProjectRequest,
  UpdateProjectRequest,
} from '../types/project.types';
import { createProjectService } from '../services/project.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';

/**
 * Project store for managing project state.
 */
export const useProjectStore = defineStore('project', () => {
  // State
  const projects = ref<ProjectListItemResponse[]>([]);
  const currentProject = ref<ProjectResponse | null>(null);
  const selectedWorkspaceId = ref<string | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service instance
  const apiClient = createApiClient();
  const projectService = createProjectService(apiClient);

  // Getters
  const hasProjects = computed(() => projects.value.length > 0);
  const projectCount = computed(() => projects.value.length);
  const activeProjects = computed(() => projects.value.filter(p => p.status !== 'Archived'));

  // Actions
  async function fetchProjects(workspaceId: string, includeArchived = false): Promise<void> {
    isLoading.value = true;
    error.value = null;
    selectedWorkspaceId.value = workspaceId;

    try {
      projects.value = await projectService.getProjects(workspaceId, includeArchived);
    } catch (err) {
      error.value = 'Failed to fetch projects';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch projects' });
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchProject(id: string): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      currentProject.value = await projectService.getProject(id);
    } catch (err) {
      error.value = 'Failed to fetch project';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch project' });
    } finally {
      isLoading.value = false;
    }
  }

  async function createProject(data: CreateProjectRequest): Promise<ProjectResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      const project = await projectService.createProject(data);
      projects.value.push({
        id: project.id,
        workspaceId: project.workspaceId,
        name: project.name,
        key: project.key,
        status: project.status,
        color: project.color,
        boardCount: project.boardCount,
        taskCount: project.taskCount,
        createdAt: project.createdAt,
      });
      return project;
    } catch (err) {
      error.value = 'Failed to create project';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function updateProject(id: string, data: UpdateProjectRequest): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      const project = await projectService.updateProject(id, data);

      // Update in list
      const index = projects.value.findIndex((p) => p.id === id);
      const existingProject = projects.value[index];
      if (index !== -1 && existingProject) {
        // Create new object with all required fields
        const updatedProject: ProjectListItemResponse = {
          id: existingProject.id,
          workspaceId: existingProject.workspaceId,
          name: project.name,
          key: project.key,
          status: project.status,
          color: project.color,
          boardCount: existingProject.boardCount,
          taskCount: existingProject.taskCount,
          createdAt: existingProject.createdAt,
        };
        projects.value[index] = updatedProject;
      }

      // Update current project
      if (currentProject.value?.id === id) {
        currentProject.value = project;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to update project';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function archiveProject(id: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await projectService.archiveProject(id);

      // Update in list
      const index = projects.value.findIndex((p) => p.id === id);
      const projectToArchive = projects.value[index];
      if (index !== -1 && projectToArchive) {
        projectToArchive.status = 'Archived';
      }

      if (currentProject.value?.id === id && currentProject.value) {
        currentProject.value.status = 'Archived';
      }

      return true;
    } catch (err) {
      error.value = 'Failed to archive project';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function restoreProject(id: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await projectService.restoreProject(id);

      // Update in list
      const index = projects.value.findIndex((p) => p.id === id);
      const projectToRestore = projects.value[index];
      if (index !== -1 && projectToRestore) {
        projectToRestore.status = 'Active';
      }

      if (currentProject.value?.id === id && currentProject.value) {
        currentProject.value.status = 'Active';
      }

      return true;
    } catch (err) {
      error.value = 'Failed to restore project';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteProject(id: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await projectService.deleteProject(id);
      projects.value = projects.value.filter((p) => p.id !== id);

      if (currentProject.value?.id === id) {
        currentProject.value = null;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to delete project';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  function setCurrentProject(project: ProjectResponse | null): void {
    currentProject.value = project;
  }

  function clearError(): void {
    error.value = null;
  }

  return {
    // State
    projects,
    currentProject,
    selectedWorkspaceId,
    isLoading,
    error,

    // Getters
    hasProjects,
    projectCount,
    activeProjects,

    // Actions
    fetchProjects,
    fetchProject,
    createProject,
    updateProject,
    archiveProject,
    restoreProject,
    deleteProject,
    setCurrentProject,
    clearError,
  };
});

// Type for store
export type ProjectStore = ReturnType<typeof useProjectStore>;
