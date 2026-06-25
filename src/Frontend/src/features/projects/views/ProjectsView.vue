<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useProjectStore } from '@features/projects/stores/project.store';
import { useWorkspaceStore } from '@features/workspace/stores/workspace.store';
import { PROJECT_STATUSES } from '@features/projects/types/project.types';

const router = useRouter();
const projectStore = useProjectStore();
const workspaceStore = useWorkspaceStore();

const showCreateModal = ref(false);
const showEditModal = ref(false);
const showDeleteConfirm = ref(false);
const selectedProject = ref<any>(null);
const showArchived = ref(false);

const selectedWorkspaceId = ref<string | null>(null);

const newProjectName = ref('');
const newProjectDescription = ref('');
const newProjectColor = ref('#3b82f6');
const isCreating = ref(false);

const editProjectName = ref('');
const editProjectDescription = ref('');
const editProjectColor = ref('#3b82f6');
const editProjectStatus = ref('Planning');
const isUpdating = ref(false);

const colorOptions = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6',
  '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1',
];

// Load workspaces and select the first one on mount
onMounted(async () => {
  if (workspaceStore.workspaces.length === 0) {
    await workspaceStore.fetchWorkspaces();
  }

  // Auto-select first workspace if available
  if (workspaceStore.workspaces.length > 0 && !selectedWorkspaceId.value) {
    selectedWorkspaceId.value = workspaceStore.workspaces[0]?.id ?? null;
  }
});

// Watch for workspace selection changes and load projects
watch(selectedWorkspaceId, async (newId) => {
  if (newId) {
    console.log('[ProjectsView] Fetching projects for workspace:', newId);
    await projectStore.fetchProjects(newId, showArchived.value);
    console.log('[ProjectsView] Projects loaded:', projectStore.projects.length);
    console.log('[ProjectsView] store.projects:', projectStore.projects);
  }
});

// Re-fetch when showArchived changes
watch(showArchived, async (includeArchived) => {
  if (selectedWorkspaceId.value) {
    console.log('[ProjectsView] showArchived changed, re-fetching with includeArchived:', includeArchived);
    await projectStore.fetchProjects(selectedWorkspaceId.value, includeArchived);
  }
});

const selectedWorkspace = computed(() => {
  return workspaceStore.workspaces.find(w => w.id === selectedWorkspaceId.value);
});

const displayedProjects = computed(() => {
  // Explicitly access the ref to ensure reactivity tracking
  const projects = projectStore.projects;
  const activeProjects = projectStore.activeProjects;
  console.log('[ProjectsView] displayedProjects computed - projects:', projects.length, 'active:', activeProjects.length);
  return showArchived.value ? projects : activeProjects;
});

async function handleCreateProject() {
  if (!newProjectName.value.trim() || !selectedWorkspaceId.value) return;

  isCreating.value = true;
  const result = await projectStore.createProject({
    workspaceId: selectedWorkspaceId.value,
    name: newProjectName.value.trim(),
    description: newProjectDescription.value.trim() || undefined,
    color: newProjectColor.value,
  });

  if (result) {
    showCreateModal.value = false;
    newProjectName.value = '';
    newProjectDescription.value = '';
    newProjectColor.value = '#3b82f6';
  }
  isCreating.value = false;
}

function openEditModal(project: any) {
  selectedProject.value = project;
  editProjectName.value = project.name;
  editProjectDescription.value = project.description || '';
  editProjectColor.value = project.color;
  editProjectStatus.value = project.status;
  showEditModal.value = true;
}

async function handleUpdateProject() {
  if (!editProjectName.value.trim() || !selectedProject.value) return;

  isUpdating.value = true;
  const success = await projectStore.updateProject(selectedProject.value.id, {
    name: editProjectName.value.trim(),
    description: editProjectDescription.value.trim() || undefined,
    color: editProjectColor.value,
    status: editProjectStatus.value,
  });

  if (success) {
    showEditModal.value = false;
    selectedProject.value = null;
  }
  isUpdating.value = false;
}

function openDeleteConfirm(project: any) {
  console.log('[ProjectsView] openDeleteConfirm called, project:', project);
  selectedProject.value = project;
  showDeleteConfirm.value = true;
  console.log('[ProjectsView] showDeleteConfirm set to:', showDeleteConfirm.value);
}

async function handleDeleteProject() {
  if (!selectedProject.value) return;

  const success = await projectStore.deleteProject(selectedProject.value.id);
  if (success) {
    showDeleteConfirm.value = false;
    selectedProject.value = null;
  }
}

async function handleArchiveProject(project: any) {
  await projectStore.archiveProject(project.id);
}

async function handleRestoreProject(project: any) {
  await projectStore.restoreProject(project.id);
}

function getStatusInfo(status: string) {
  return PROJECT_STATUSES.find(s => s.value === status) || PROJECT_STATUSES[0];
}

function getStatusBadgeClass(status: string): string {
  switch (status) {
    case 'Planning': return 'status-planning';
    case 'Active': return 'status-active';
    case 'OnHold': return 'status-onhold';
    case 'Completed': return 'status-completed';
    case 'Archived': return 'status-archived';
    default: return 'status-planning';
  }
}

function openProjectDetail(projectId: string) {
  router.push(`/projects/${projectId}`);
}
</script>

<template>
  <div class="projects-view">
    <div class="view-header">
      <div>
        <h1 class="view-title">Projects</h1>
        <p class="view-subtitle">
          {{ selectedWorkspace ? `in ${selectedWorkspace.name}` : 'Select a workspace' }}
        </p>
      </div>
      <div class="header-actions">
        <label class="checkbox-label">
          <input v-model="showArchived" type="checkbox" />
          <span>Show archived</span>
        </label>
        <button
          class="btn-primary"
          :disabled="!selectedWorkspaceId"
          @click="showCreateModal = true"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
            <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Project
        </button>
      </div>
    </div>

    <!-- Workspace Selector -->
    <div v-if="workspaceStore.workspaces.length > 0" class="workspace-selector">
      <select
        v-model="selectedWorkspaceId"
        class="form-select"
      >
        <option :value="null" disabled>Select a workspace</option>
        <option v-for="ws in workspaceStore.workspaces" :key="ws.id" :value="ws.id">
          {{ ws.name }}
        </option>
      </select>
    </div>

    <!-- Projects Empty State (workspace selected but no projects) -->
    <div
      v-else-if="selectedWorkspaceId && displayedProjects.length === 0"
      class="empty-state"
    >
      <div class="empty-icon">
        <svg width="64" height="64" viewBox="0 0 24 24" fill="none">
          <path d="M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2v11z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M12 11v6M9 14h6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
        </svg>
      </div>
      <h3>No projects yet</h3>
      <p>Create your first project to start organizing your work.</p>
      <button class="btn-primary" @click="showCreateModal = true">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
        New Project
      </button>
    </div>

    <!-- Projects Grid -->
    <div class="projects-grid">
      <div
        v-for="project in displayedProjects"
        :key="project.id"
        class="project-card"
        :class="{ 'is-archived': project.status === 'Archived' }"
        @click="openProjectDetail(project.id)"
      >
        <div class="project-color-bar" :style="{ backgroundColor: project.color }"></div>
        <div class="project-content">
          <div class="project-header">
            <div class="project-icon" :style="{ backgroundColor: project.color + '20', color: project.color }">
              {{ project.name.charAt(0).toUpperCase() }}
            </div>
            <div class="project-info">
              <h3 class="project-name">{{ project.name }}</h3>
              <span v-if="project.key" class="project-key">{{ project.key }}</span>
            </div>
            <span :class="['status-badge', getStatusBadgeClass(project.status)]">
              {{ getStatusInfo(project.status).label }}
            </span>
            <svg class="click-indicator" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 18l6-6-6-6"/>
            </svg>
          </div>
          <div class="project-meta">
            <span class="meta-item">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2"/>
                <path d="M3 9h18M9 21V9" stroke="currentColor" stroke-width="2"/>
              </svg>
              {{ project.boardCount }} board{{ project.boardCount > 1 ? 's' : '' }}
            </span>
            <span class="meta-item">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              {{ project.taskCount }} task{{ project.taskCount > 1 ? 's' : '' }}
            </span>
          </div>
          <div class="project-actions">
            <button class="btn-icon" title="Edit" @click.stop="openEditModal(project)">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>Edit</span>
            </button>
            <button
              v-if="project.status === 'Archived'"
              class="btn-icon"
              title="Restore"
              @click.stop="handleRestoreProject(project)"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <path d="M3 12a9 9 0 109-9 9.75 9.75 0 00-6.74 2.74L3 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M3 3v5h5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>Restore</span>
            </button>
            <button
              v-else
              class="btn-icon"
              title="Archive"
              @click.stop="handleArchiveProject(project)"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <path d="M21 8v13H3V8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M1 3h22v5H1V3z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M10 12h4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
              <span>Archive</span>
            </button>
            <button class="btn-icon btn-danger" title="Delete" @click.stop="openDeleteConfirm(project)">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>Delete</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Project Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Create Project</h2>
          <button class="btn-icon" @click="showCreateModal = false">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <form class="modal-body" @submit.prevent="handleCreateProject">
          <div class="form-group">
            <label for="project-name">Project Name</label>
            <input
              id="project-name"
              v-model="newProjectName"
              type="text"
              placeholder="My Project"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="project-description">Description (optional)</label>
            <textarea
              id="project-description"
              v-model="newProjectDescription"
              placeholder="Describe your project..."
              class="form-input"
              rows="3"
            ></textarea>
          </div>
          <div class="form-group">
            <label>Color</label>
            <div class="color-picker">
              <button
                v-for="color in colorOptions"
                :key="color"
                type="button"
                class="color-option"
                :class="{ selected: newProjectColor === color }"
                :style="{ backgroundColor: color }"
                @click="newProjectColor = color"
              ></button>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="showCreateModal = false">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="isCreating || !newProjectName.trim()">
              <span v-if="isCreating" class="loading-spinner"></span>
              {{ isCreating ? 'Creating...' : 'Create Project' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Edit Project Modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Edit Project</h2>
          <button class="btn-icon" @click="showEditModal = false">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <form class="modal-body" @submit.prevent="handleUpdateProject">
          <div class="form-group">
            <label for="edit-name">Project Name</label>
            <input
              id="edit-name"
              v-model="editProjectName"
              type="text"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="edit-description">Description</label>
            <textarea
              id="edit-description"
              v-model="editProjectDescription"
              class="form-input"
              rows="3"
            ></textarea>
          </div>
          <div class="form-group">
            <label for="edit-status">Status</label>
            <select id="edit-status" v-model="editProjectStatus" class="form-select">
              <option v-for="status in PROJECT_STATUSES" :key="status.value" :value="status.value">
                {{ status.label }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Color</label>
            <div class="color-picker">
              <button
                v-for="color in colorOptions"
                :key="color"
                type="button"
                class="color-option"
                :class="{ selected: editProjectColor === color }"
                :style="{ backgroundColor: color }"
                @click="editProjectColor = color"
              ></button>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="showEditModal = false">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="isUpdating || !editProjectName.trim()">
              <span v-if="isUpdating" class="loading-spinner"></span>
              {{ isUpdating ? 'Saving...' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
      <div class="modal modal-sm modal-danger">
        <div class="modal-header">
          <div class="modal-header-content">
            <div class="modal-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                <path d="M12 9v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <h2>Delete Project</h2>
          </div>
          <button class="btn-close" @click="showDeleteConfirm = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to delete <strong>{{ selectedProject?.name }}</strong>?</p>
          <p class="modal-warning">This action cannot be undone.</p>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn-secondary" @click="showDeleteConfirm = false">Cancel</button>
          <button type="button" class="btn-danger" @click="handleDeleteProject">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
              <path d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Delete Project
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.projects-view {
  max-width: 1200px;
}

.view-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-lg);
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.view-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-xs);
}

.view-subtitle {
  color: var(--color-gray-500);
}

.header-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: 0.875rem;
  color: var(--color-gray-600);
  cursor: pointer;
}

.workspace-selector {
  margin-bottom: var(--spacing-lg);
}

.form-select {
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 1rem;
  background-color: white;
  min-width: 200px;
}

.form-select:focus {
  outline: none;
  border-color: var(--color-primary);
}

.no-workspace-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xxl);
  text-align: center;
  background-color: white;
  border-radius: var(--radius-lg);
}

.no-workspace-state p {
  color: var(--color-gray-500);
  margin-bottom: var(--spacing-lg);
}

.btn-primary {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: var(--color-primary);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--color-primary-dark);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: white;
  color: var(--color-gray-700);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-secondary:hover {
  background-color: var(--color-gray-50);
}

.btn-danger {
  background-color: var(--color-error);
  color: white;
  border: none;
}

.btn-danger:hover {
  background-color: #dc2626;
}

.btn-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 4px 8px;
  min-width: 60px;
  background: transparent;
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  color: var(--color-gray-700);
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.75rem;
}

.btn-icon:hover {
  background-color: var(--color-gray-100);
  color: var(--color-gray-900);
}

.btn-icon.btn-danger {
  border-color: var(--color-error);
  color: var(--color-error);
}

.btn-icon.btn-danger:hover {
  background-color: #fef2f2;
  color: #dc2626;
}

.btn-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background: transparent;
  border: none;
  border-radius: var(--radius-md);
  color: var(--color-gray-500);
  cursor: pointer;
  transition: all 0.2s;
  margin-left: auto;
}

.btn-close:hover {
  background-color: rgba(0, 0, 0, 0.05);
  color: var(--color-gray-700);
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xxl);
  color: var(--color-gray-500);
}

.loading-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-200);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-right: var(--spacing-sm);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xxl);
  text-align: center;
  background-color: white;
  border-radius: var(--radius-lg);
}

.empty-icon {
  color: var(--color-gray-300);
  margin-bottom: var(--spacing-lg);
}

.empty-state h3 {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-sm);
}

.empty-state p {
  color: var(--color-gray-500);
  margin-bottom: var(--spacing-lg);
}

.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-lg);
}

.project-card {
  background-color: white;
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  transition: box-shadow 0.2s, transform 0.2s;
  cursor: pointer;
}

.project-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.project-card.is-archived {
  opacity: 0.7;
}

.project-color-bar {
  height: 4px;
}

.project-content {
  padding: var(--spacing-lg);
}

.project-header {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.project-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  font-size: 1.125rem;
  font-weight: 600;
  flex-shrink: 0;
}

.project-info {
  flex: 1;
  min-width: 0;
}

.project-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.click-indicator {
  color: var(--color-gray-400);
  opacity: 0;
  transition: opacity 0.2s, transform 0.2s;
  flex-shrink: 0;
}

.project-card:hover .click-indicator {
  opacity: 1;
  transform: translateX(2px);
}

.project-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: 2px;
}

.project-key {
  font-size: 0.75rem;
  color: var(--color-gray-500);
}

.status-badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 500;
  flex-shrink: 0;
}

.status-planning { background-color: #f3f4f6; color: #374151; }
.status-active { background-color: #d1fae5; color: #065f46; }
.status-onhold { background-color: #fef3c7; color: #92400e; }
.status-completed { background-color: #dbeafe; color: #1e40af; }
.status-archived { background-color: #f1f5f9; color: #64748b; }

.project-meta {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 0.8125rem;
  color: var(--color-gray-500);
}

.project-actions {
  display: flex;
  gap: var(--spacing-sm);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-gray-100);
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: var(--spacing-lg);
}

.modal {
  width: 100%;
  max-width: 480px;
  background-color: white;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
}

.modal-sm {
  max-width: 400px;
}

.modal-danger .modal-header {
  background-color: #fef2f2;
  border-bottom-color: #fecaca;
}

.modal-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  background-color: #fee2e2;
  border-radius: 50%;
  color: #dc2626;
  flex-shrink: 0;
}

.modal-danger .modal-header-content {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  flex: 1;
}

.modal-danger .modal-header h2 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
}

.modal-danger .modal-body {
  padding: var(--spacing-xl);
  text-align: center;
}

.modal-danger .modal-body p {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--color-gray-700);
  font-size: 0.9375rem;
  line-height: 1.5;
}

.modal-warning {
  color: var(--color-gray-500);
  font-size: 0.875rem;
}

.modal-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-gray-200);
}

.modal-header h2 {
  font-size: 1.25rem;
  font-weight: 600;
}

.modal-body {
  padding: var(--spacing-lg);
}

.form-group {
  margin-bottom: var(--spacing-lg);
}

.form-group label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-700);
  margin-bottom: var(--spacing-sm);
}

.form-input {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 1rem;
  transition: border-color 0.2s;
}

.form-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-light);
}

.color-picker {
  display: flex;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.color-option {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-md);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.2s;
}

.color-option:hover {
  transform: scale(1.1);
}

.color-option.selected {
  border-color: var(--color-gray-900);
  box-shadow: 0 0 0 2px white, 0 0 0 4px var(--color-gray-900);
}

.modal-footer {
  display: flex;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  border-top: 1px solid var(--color-gray-200);
}

.modal-footer button {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.modal-footer .btn-secondary {
  background-color: white;
  border: 1px solid var(--color-gray-300);
  color: var(--color-gray-700);
}

.modal-footer .btn-secondary:hover {
  background-color: var(--color-gray-50);
  border-color: var(--color-gray-400);
}

.modal-footer .btn-danger {
  background-color: #dc2626;
  border: none;
  color: white;
}

.modal-footer .btn-danger:hover {
  background-color: #b91c1c;
}

/* Responsive */
@media (max-width: 640px) {
  .modal-overlay {
    padding: var(--spacing-md);
    align-items: flex-end;
  }

  .modal {
    max-width: 100%;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  }

  .modal-footer {
    flex-direction: column;
  }

  .modal-footer button {
    width: 100%;
    justify-content: center;
  }
}
</style>
