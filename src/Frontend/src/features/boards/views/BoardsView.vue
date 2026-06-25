<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useBoardStore } from '@features/boards/stores/board.store';
import { useProjectStore } from '@features/projects/stores/project.store';
import type { BoardResponse } from '@features/boards/types/board.types';

const route = useRoute();
const router = useRouter();
const boardStore = useBoardStore();
const projectStore = useProjectStore();

const showCreateModal = ref(false);
const showEditModal = ref(false);
const showDeleteConfirm = ref(false);
const selectedBoard = ref<BoardResponse | null>(null);
const newBoardName = ref('');
const newBoardDescription = ref('');
const isCreating = ref(false);

// Get project ID from route params
const projectId = computed(() => route.params.projectId as string);

// Load boards on mount
onMounted(async () => {
  if (projectId.value) {
    await boardStore.fetchProjectBoards(projectId.value);
  }
});

// Watch for board changes
watch(() => route.params.projectId, async (newId) => {
  if (newId) {
    await boardStore.fetchProjectBoards(newId as string);
  }
});

const selectedProject = computed(() => {
  if (!projectId.value) return null;
  return projectStore.projects.find(p => p.id === projectId.value);
});

async function handleCreateBoard() {
  if (!newBoardName.value.trim() || !projectId.value) return;

  isCreating.value = true;
  const result = await boardStore.createBoard({
    projectId: projectId.value,
    name: newBoardName.value.trim(),
    description: newBoardDescription.value.trim() || undefined,
  });

  if (result) {
    showCreateModal.value = false;
    newBoardName.value = '';
    newBoardDescription.value = '';
    // Navigate to the new board
    router.push(`/projects/${projectId.value}/boards/${result.id}`);
  }
  isCreating.value = false;
}

function openEditModal(board: BoardResponse) {
  selectedBoard.value = board;
  showEditModal.value = true;
}

async function handleUpdateBoard() {
  if (!selectedBoard.value || !newBoardName.value.trim()) return;

  isCreating.value = true;
  await boardStore.updateBoard(selectedBoard.value.id, {
    name: newBoardName.value.trim(),
    description: newBoardDescription.value.trim() || undefined,
  });

  showEditModal.value = false;
  selectedBoard.value = null;
  newBoardName.value = '';
  newBoardDescription.value = '';
  isCreating.value = false;
}

function openDeleteConfirm(board: BoardResponse) {
  selectedBoard.value = board;
  showDeleteConfirm.value = true;
}

async function handleDeleteBoard() {
  if (!selectedBoard.value) return;

  const success = await boardStore.deleteBoard(selectedBoard.value.id);
  if (success) {
    showDeleteConfirm.value = false;
    selectedBoard.value = null;
    router.push(`/projects/${projectId.value}`);
  }
}

function openBoard(boardId: string) {
  router.push(`/projects/${projectId.value}/boards/${boardId}`);
}
</script>

<template>
  <div class="boards-view">
    <div class="view-header">
      <div>
        <div class="breadcrumb">
          <router-link :to="`/projects/${projectId}`" class="breadcrumb-link">
            {{ selectedProject?.name || 'Project' }}
          </router-link>
          <span class="breadcrumb-sep">/</span>
          <span class="breadcrumb-current">Boards</span>
        </div>
        <h1 class="view-title">Boards</h1>
      </div>
      <button class="btn-primary" @click="showCreateModal = true">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
        New Board
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="boardStore.isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading boards...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!boardStore.hasBoards" class="empty-state">
      <div class="empty-icon">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
          <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2"/>
          <path d="M3 9h18M9 21V9" stroke="currentColor" stroke-width="2"/>
        </svg>
      </div>
      <h3>No boards yet</h3>
      <p>Create your first board to start organizing tasks.</p>
      <button class="btn-primary" @click="showCreateModal = true">
        Create Board
      </button>
    </div>

    <!-- Boards Grid -->
    <div v-else class="boards-grid">
      <div
        v-for="board in boardStore.boards"
        :key="board.id"
        class="board-card"
        @click="openBoard(board.id)"
      >
        <div class="board-header">
          <div class="board-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2"/>
              <path d="M3 9h18M9 21V9" stroke="currentColor" stroke-width="2"/>
            </svg>
          </div>
          <div class="board-actions" @click.stop>
            <button class="btn-action btn-edit" title="Edit Board" @click="openEditModal(board as any)">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>Edit</span>
            </button>
            <button class="btn-action btn-delete" title="Delete Board" @click="openDeleteConfirm(board as any)">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>Delete</span>
            </button>
          </div>
        </div>
        <div class="board-content">
          <h3 class="board-name">{{ board.name }}</h3>
          <p v-if="board.description" class="board-description">{{ board.description }}</p>
        </div>
        <div class="board-meta">
          <span class="meta-item">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="14" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
            </svg>
            {{ board.columnCount }} columns
          </span>
          <span class="meta-item">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            {{ board.taskCount }} tasks
          </span>
        </div>
        <div class="board-footer">
          <span v-if="board.isDefault" class="badge badge-default">Default</span>
          <span class="badge badge-type">{{ board.type }}</span>
        </div>
      </div>
    </div>

    <!-- Create Board Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Create Board</h2>
          <button class="btn-icon" @click="showCreateModal = false">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <form class="modal-body" @submit.prevent="handleCreateBoard">
          <div class="form-group">
            <label for="board-name">Board Name</label>
            <input
              id="board-name"
              v-model="newBoardName"
              type="text"
              placeholder="e.g., Sprint Planning"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="board-description">Description (optional)</label>
            <textarea
              id="board-description"
              v-model="newBoardDescription"
              placeholder="Describe this board..."
              class="form-input"
              rows="3"
            ></textarea>
          </div>
          <div class="form-hint">
            <p>Default columns will be created: To Do, In Progress, Done</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="showCreateModal = false">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="isCreating || !newBoardName.trim()">
              <span v-if="isCreating" class="loading-spinner"></span>
              {{ isCreating ? 'Creating...' : 'Create Board' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Edit Board Modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Edit Board</h2>
          <button class="btn-icon" @click="showEditModal = false">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <form class="modal-body" @submit.prevent="handleUpdateBoard">
          <div class="form-group">
            <label for="edit-board-name">Board Name</label>
            <input
              id="edit-board-name"
              v-model="newBoardName"
              type="text"
              class="form-input"
              required
            />
          </div>
          <div class="form-group">
            <label for="edit-board-description">Description</label>
            <textarea
              id="edit-board-description"
              v-model="newBoardDescription"
              class="form-input"
              rows="3"
            ></textarea>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="showEditModal = false">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="isCreating">
              {{ isCreating ? 'Saving...' : 'Save Changes' }}
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
            <h2>Delete Board</h2>
          </div>
          <button class="btn-close" @click="showDeleteConfirm = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to delete <strong>{{ selectedBoard?.name }}</strong>?</p>
          <p class="modal-warning">This action cannot be undone.</p>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn-secondary" @click="showDeleteConfirm = false">Cancel</button>
          <button type="button" class="btn-danger" @click="handleDeleteBoard">Delete Board</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.boards-view {
  max-width: 1200px;
}

.view-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: 0.875rem;
  margin-bottom: var(--spacing-sm);
}

.breadcrumb-link {
  color: var(--color-gray-500);
  text-decoration: none;
}

.breadcrumb-link:hover {
  color: var(--color-primary);
}

.breadcrumb-sep {
  color: var(--color-gray-300);
}

.breadcrumb-current {
  color: var(--color-gray-700);
}

.view-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-gray-900);
}

.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xxl);
  text-align: center;
  background-color: white;
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
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

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--color-gray-200);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: var(--spacing-md);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.boards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--spacing-lg);
}

.board-card {
  background-color: white;
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  padding: var(--spacing-lg);
  cursor: pointer;
  transition: all 0.2s;
}

.board-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.board-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-md);
}

.board-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  background-color: var(--color-primary-light);
  color: var(--color-primary);
  display: flex;
  align-items: center;
  justify-content: center;
}

.board-actions {
  display: flex;
  gap: var(--spacing-xs);
  opacity: 0;
  transition: opacity 0.2s;
}

.board-card:hover .board-actions {
  opacity: 1;
}

.btn-action {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-edit {
  color: var(--color-gray-600);
}

.btn-edit:hover {
  background-color: var(--color-primary-light);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.btn-delete {
  color: var(--color-gray-500);
}

.btn-delete:hover {
  background-color: #fef2f2;
  border-color: #fecaca;
  color: #dc2626;
}

.btn-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  color: var(--color-gray-600);
  cursor: pointer;
  transition: all 0.2s;
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

.board-content {
  margin-bottom: var(--spacing-md);
}

.board-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-xs);
}

.board-description {
  font-size: 0.875rem;
  color: var(--color-gray-500);
  line-height: 1.4;
}

.board-meta {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-gray-100);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 0.8125rem;
  color: var(--color-gray-500);
}

.board-footer {
  display: flex;
  gap: var(--spacing-sm);
}

.badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 500;
}

.badge-default {
  background-color: var(--color-gray-100);
  color: var(--color-gray-700);
}

.badge-type {
  background-color: var(--color-primary-light);
  color: var(--color-primary);
}

/* Modal styles */
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

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-gray-200);
}

.modal-header h2 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0;
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

.form-hint {
  font-size: 0.875rem;
  color: var(--color-gray-500);
  background-color: var(--color-gray-50);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
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

.modal-danger .modal-header {
  background-color: #fef2f2;
  border-bottom-color: #fecaca;
}

.modal-header-content {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.modal-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background-color: #fee2e2;
  border-radius: 50%;
  color: #dc2626;
}

.modal-danger .modal-body {
  text-align: center;
  padding: var(--spacing-xl);
}

.modal-danger .modal-body p {
  margin: 0 0 var(--spacing-sm) 0;
  color: var(--color-gray-700);
}

.modal-warning {
  color: var(--color-gray-500);
  font-size: 0.875rem;
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

.btn-danger {
  background-color: #dc2626;
  color: white;
  border: none;
}

.btn-danger:hover {
  background-color: #b91c1c;
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
}

.btn-close:hover {
  background-color: rgba(0, 0, 0, 0.05);
  color: var(--color-gray-700);
}
</style>
