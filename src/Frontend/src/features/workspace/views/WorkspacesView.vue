<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useWorkspaceStore } from '@features/workspace/stores/workspace.store';

const workspaceStore = useWorkspaceStore();

const showCreateModal = ref(false);
const newWorkspaceName = ref('');
const newWorkspaceDescription = ref('');
const isCreating = ref(false);

onMounted(() => {
  workspaceStore.fetchWorkspaces();
});

async function handleCreateWorkspace() {
  if (!newWorkspaceName.value.trim()) return;

  isCreating.value = true;
  const result = await workspaceStore.createWorkspace({
    name: newWorkspaceName.value.trim(),
    description: newWorkspaceDescription.value.trim() || undefined,
    isPublic: true,
  });

  if (result) {
    showCreateModal.value = false;
    newWorkspaceName.value = '';
    newWorkspaceDescription.value = '';
  }
  isCreating.value = false;
}

function getRoleBadgeClass(role: string): string {
  switch (role) {
    case 'Owner':
      return 'badge-owner';
    case 'Admin':
      return 'badge-admin';
    default:
      return 'badge-member';
  }
}
</script>

<template>
  <div class="workspaces-view">
    <div class="view-header">
      <div>
        <h1 class="view-title">Workspaces</h1>
        <p class="view-subtitle">Manage your workspaces</p>
      </div>
      <button class="btn-primary" @click="showCreateModal = true">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
        Create Workspace
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="workspaceStore.isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading workspaces...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!workspaceStore.hasWorkspaces" class="empty-state">
      <div class="empty-icon">
        <svg width="64" height="64" viewBox="0 0 24 24" fill="none">
          <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M9 22V12h6v10" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <h3>No workspaces yet</h3>
      <p>Create your first workspace to get started</p>
      <button class="btn-primary" @click="showCreateModal = true">
        Create Workspace
      </button>
    </div>

    <!-- Workspaces Grid -->
    <div v-else class="workspaces-grid">
      <div
        v-for="workspace in workspaceStore.workspaces"
        :key="workspace.id"
        class="workspace-card"
      >
        <div class="workspace-icon">
          <span>{{ workspace.name.charAt(0).toUpperCase() }}</span>
        </div>
        <div class="workspace-info">
          <h3 class="workspace-name">{{ workspace.name }}</h3>
          <div class="workspace-meta">
            <span :class="['badge', getRoleBadgeClass(workspace.role)]">
              {{ workspace.role }}
            </span>
            <span class="meta-item">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="2"/>
                <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
              {{ workspace.memberCount }} member{{ workspace.memberCount > 1 ? 's' : '' }}
            </span>
            <span class="meta-item">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2v11z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              {{ workspace.projectCount }} project{{ workspace.projectCount > 1 ? 's' : '' }}
            </span>
          </div>
        </div>
        <div class="workspace-actions">
          <button class="btn-icon" title="Settings">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-2 2 2 2 0 01-2-2v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06a1.65 1.65 0 00.33-1.82 1.65 1.65 0 00-1.51-1H3a2 2 0 01-2-2 2 2 0 012-2h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06a1.65 1.65 0 001.82.33H9a1.65 1.65 0 001-1.51V3a2 2 0 012-2 2 2 0 012 2v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06a1.65 1.65 0 00-.33 1.82V9a1.65 1.65 0 001.51 1H21a2 2 0 012 2 2 2 0 01-2 2h-.09a1.65 1.65 0 00-1.51 1z" stroke="currentColor" stroke-width="2"/>
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Create Workspace Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Create Workspace</h2>
          <button class="btn-icon" @click="showCreateModal = false">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <form class="modal-body" @submit.prevent="handleCreateWorkspace">
          <div class="form-group">
            <label for="workspace-name">Workspace Name</label>
            <input
              id="workspace-name"
              v-model="newWorkspaceName"
              type="text"
              placeholder="My Workspace"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="workspace-description">Description (optional)</label>
            <textarea
              id="workspace-description"
              v-model="newWorkspaceDescription"
              placeholder="Describe your workspace..."
              class="form-input"
              rows="3"
            ></textarea>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="showCreateModal = false">
              Cancel
            </button>
            <button type="submit" class="btn-primary" :disabled="isCreating || !newWorkspaceName.trim()">
              <span v-if="isCreating" class="loading-spinner"></span>
              {{ isCreating ? 'Creating...' : 'Create Workspace' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.workspaces-view {
  max-width: 1200px;
}

.view-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
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

.btn-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  background: transparent;
  border: none;
  border-radius: var(--radius-md);
  color: var(--color-gray-500);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-icon:hover {
  background-color: var(--color-gray-100);
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
  width: 24px;
  height: 24px;
  border: 2px solid var(--color-gray-200);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: var(--spacing-md);
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

.workspaces-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--spacing-lg);
}

.workspace-card {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  background-color: white;
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  transition: box-shadow 0.2s;
}

.workspace-card:hover {
  box-shadow: var(--shadow-md);
}

.workspace-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  background-color: var(--color-primary-light);
  color: var(--color-primary);
  border-radius: var(--radius-md);
  font-size: 1.25rem;
  font-weight: 600;
  flex-shrink: 0;
}

.workspace-info {
  flex: 1;
  min-width: 0;
}

.workspace-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-xs);
}

.workspace-meta {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-sm);
  align-items: center;
}

.badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 500;
}

.badge-owner {
  background-color: #fef3c7;
  color: #92400e;
}

.badge-admin {
  background-color: #dbeafe;
  color: #1e40af;
}

.badge-member {
  background-color: var(--color-gray-100);
  color: var(--color-gray-600);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 0.8125rem;
  color: var(--color-gray-500);
}

.workspace-actions {
  flex-shrink: 0;
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

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
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

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-gray-200);
  margin-top: var(--spacing-lg);
}
</style>
