<script setup lang="ts">
/**
 * Project detail view with board management.
 */
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useProjectStore } from '@features/projects/stores/project.store';
import { useBoardStore } from '@features/boards/stores/board.store';
import type { BoardListItemResponse } from '@features/boards/types/board.types';

const route = useRoute();
const router = useRouter();
const projectStore = useProjectStore();
const boardStore = useBoardStore();

const projectId = computed(() => route.params.projectId as string);
const project = computed(() => projectStore.currentProject);

// Modal states
const showCreateModal = ref(false);
const showEditModal = ref(false);
const showDeleteConfirm = ref(false);
const selectedBoard = ref<BoardListItemResponse | null>(null);

// Form fields
const newBoardName = ref('');
const newBoardDescription = ref('');
const isCreating = ref(false);

onMounted(async () => {
  await projectStore.fetchProject(projectId.value);
  await boardStore.fetchProjectBoards(projectId.value);
});

async function handleCreateBoard() {
  if (!newBoardName.value.trim()) return;

  isCreating.value = true;
  try {
    const result = await boardStore.createBoard({
      projectId: projectId.value,
      name: newBoardName.value.trim(),
      description: newBoardDescription.value.trim() || undefined,
      type: 'Kanban',
    });

    if (result) {
      showCreateModal.value = false;
      newBoardName.value = '';
      newBoardDescription.value = '';
    }
  } catch (err) {
    console.error('Failed to create board:', err);
  } finally {
    isCreating.value = false;
  }
}

function openBoard(boardId: string) {
  router.push(`/projects/${projectId.value}/boards/${boardId}`);
}

function openEditModal(board: BoardListItemResponse) {
  selectedBoard.value = board;
  newBoardName.value = board.name;
  newBoardDescription.value = board.description || '';
  showEditModal.value = true;
}

async function handleUpdateBoard() {
  if (!selectedBoard.value || !newBoardName.value.trim()) return;

  isCreating.value = true;
  try {
    await boardStore.updateBoard(selectedBoard.value.id, {
      name: newBoardName.value.trim(),
      description: newBoardDescription.value.trim() || undefined,
    });
    showEditModal.value = false;
    selectedBoard.value = null;
    newBoardName.value = '';
    newBoardDescription.value = '';
  } finally {
    isCreating.value = false;
  }
}

function openDeleteConfirm(board: BoardListItemResponse) {
  selectedBoard.value = board;
  showDeleteConfirm.value = true;
}

async function handleDeleteBoard() {
  if (!selectedBoard.value) return;

  const success = await boardStore.deleteBoard(selectedBoard.value.id);
  if (success) {
    showDeleteConfirm.value = false;
    selectedBoard.value = null;
  }
}
</script>

<template>
  <div class="project-detail-view">
    <!-- Header -->
    <div class="page-header">
      <router-link to="/projects" class="back-link">
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M19 12H5M12 19l-7-7 7-7"/>
        </svg>
        Back to Projects
      </router-link>

      <div class="header-content">
        <div class="project-info" v-if="project">
          <div class="project-avatar" :style="{ backgroundColor: project.color }">
            {{ project.name.charAt(0).toUpperCase() }}
          </div>
          <div class="project-meta">
            <h1 class="page-title">{{ project.name }}</h1>
            <p class="project-subtitle">{{ project.key }} • {{ project.status }}</p>
          </div>
        </div>

        <button class="btn-primary" @click="showCreateModal = true">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 5v14M5 12h14"/>
          </svg>
          New Board
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="projectStore.isLoading || boardStore.isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="boardStore.error" class="error-state">
      <p>{{ boardStore.error }}</p>
    </div>

    <!-- Boards Section -->
    <div v-else class="boards-section">
      <div class="section-header">
        <h2 class="section-title">Boards</h2>
        <span class="board-count">{{ boardStore.boards.length }} board{{ boardStore.boards.length > 1 ? 's' : '' }}</span>
      </div>

      <!-- Empty State -->
      <div v-if="boardStore.boards.length === 0" class="empty-state">
        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="3" y="3" width="7" height="7" rx="1"/>
          <rect x="14" y="3" width="7" height="7" rx="1"/>
          <rect x="14" y="14" width="7" height="7" rx="1"/>
          <rect x="3" y="14" width="7" height="7" rx="1"/>
        </svg>
        <h3>No boards yet</h3>
        <p>Create your first board to start organizing tasks</p>
        <button class="btn-primary" @click="showCreateModal = true">
          Create First Board
        </button>
      </div>

      <!-- Board Grid -->
      <div v-else class="boards-grid">
        <div
          v-for="board in boardStore.boards"
          :key="board.id"
          class="board-card"
          @click="openBoard(board.id)"
        >
          <div class="board-card-header">
            <h3 class="board-name">{{ board.name }}</h3>
            <div class="board-actions" @click.stop>
              <button class="btn-action btn-edit" title="Edit Board" @click="openEditModal(board)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                <span>Edit</span>
              </button>
              <button class="btn-action btn-delete" title="Delete Board" @click="openDeleteConfirm(board)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                <span>Delete</span>
              </button>
            </div>
          </div>
          <p v-if="board.description" class="board-description">
            {{ board.description }}
          </p>
          <div class="board-stats">
            <span>{{ board.columnCount }} column{{ board.columnCount > 1 ? 's' : '' }}</span>
            <span>{{ board.taskCount }} task{{ board.taskCount > 1 ? 's' : '' }}</span>
          </div>
          <div class="board-type">
            <span class="type-badge">{{ board.type }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Board Modal -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
        <div class="modal">
          <div class="modal-header">
            <h2>Create Board</h2>
            <button class="modal-close" @click="showCreateModal = false">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 6L6 18M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form @submit.prevent="handleCreateBoard" class="modal-body">
            <div class="form-group">
              <label for="boardName">Board Name *</label>
              <input
                id="boardName"
                v-model="newBoardName"
                type="text"
                placeholder="e.g., Sprint Planning"
                required
              />
            </div>

            <div class="form-group">
              <label for="boardDescription">Description (optional)</label>
              <textarea
                id="boardDescription"
                v-model="newBoardDescription"
                placeholder="What is this board for?"
                rows="3"
              ></textarea>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn-secondary" @click="showCreateModal = false">
                Cancel
              </button>
              <button type="submit" class="btn-primary" :disabled="isCreating || !newBoardName.trim()">
                {{ isCreating ? 'Creating...' : 'Create Board' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Edit Board Modal -->
    <Teleport to="body">
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="modal">
          <div class="modal-header">
            <h2>Edit Board</h2>
            <button class="modal-close" @click="showEditModal = false">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 6L6 18M6 6l12 12"/>
              </svg>
            </button>
          </div>
          <form @submit.prevent="handleUpdateBoard" class="modal-body">
            <div class="form-group">
              <label for="editBoardName">Board Name *</label>
              <input
                id="editBoardName"
                v-model="newBoardName"
                type="text"
                placeholder="Board name"
                required
              />
            </div>
            <div class="form-group">
              <label for="editBoardDescription">Description (optional)</label>
              <textarea
                id="editBoardDescription"
                v-model="newBoardDescription"
                placeholder="What is this board for?"
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
    </Teleport>

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
        <div class="modal modal-delete">
          <div class="delete-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 9v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
          </div>
          <h2 class="delete-title">Delete Board</h2>
          <p class="delete-message">
            Are you sure you want to delete <strong>"{{ selectedBoard?.name }}"</strong>?
          </p>
          <p class="delete-warning">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
            </svg>
            This action cannot be undone. All columns and tasks in this board will be permanently deleted.
          </p>
          <div class="delete-actions">
            <button type="button" class="btn-cancel" @click="showDeleteConfirm = false">
              Cancel
            </button>
            <button type="button" class="btn-delete" @click="handleDeleteBoard">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
              </svg>
              Delete Board
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.project-detail-view {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: var(--spacing-xl);
}

.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--color-gray-500);
  font-size: 0.875rem;
  margin-bottom: var(--spacing-md);
  transition: color var(--transition-fast);
  text-decoration: none;
}

.back-link:hover {
  color: var(--color-primary);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.project-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.project-avatar {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  font-weight: 700;
  color: white;
}

.project-meta {
  display: flex;
  flex-direction: column;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin: 0;
}

.project-subtitle {
  font-size: 0.875rem;
  color: var(--color-gray-500);
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-4xl);
  gap: var(--spacing-md);
  color: var(--color-gray-500);
}

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--color-gray-200);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.boards-section {
  background-color: white;
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
  box-shadow: var(--shadow-sm);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0;
}

.board-count {
  font-size: 0.875rem;
  color: var(--color-gray-500);
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-4xl) var(--spacing-xl);
  text-align: center;
  color: var(--color-gray-500);
}

.empty-state svg {
  margin-bottom: var(--spacing-md);
  opacity: 0.5;
}

.empty-state h3 {
  margin: 0 0 var(--spacing-sm);
  color: var(--color-gray-700);
}

.empty-state p {
  margin: 0 0 var(--spacing-lg);
}

.boards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--spacing-lg);
}

.board-card {
  background: var(--color-gray-50);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  padding: var(--spacing-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.board-card:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

.board-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-sm);
}

.board-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0;
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
  justify-content: center;
  gap: 4px;
  padding: 6px 10px;
  background: white;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
  min-width: 70px;
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

.default-badge {
  font-size: 0.75rem;
  padding: 2px 6px;
  background-color: var(--color-primary);
  color: white;
  border-radius: var(--radius-sm);
}

.board-description {
  font-size: 0.875rem;
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-md);
  line-height: 1.5;
}

.board-stats {
  display: flex;
  gap: var(--spacing-md);
  font-size: 0.75rem;
  color: var(--color-gray-500);
  margin-bottom: var(--spacing-sm);
}

.board-type {
  display: flex;
}

.type-badge {
  font-size: 0.75rem;
  padding: 2px 8px;
  background-color: var(--color-gray-200);
  color: var(--color-gray-600);
  border-radius: var(--radius-sm);
  text-transform: capitalize;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: white;
  border-radius: var(--radius-lg);
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-gray-200);
}

.modal-header h2 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
}

.modal-close {
  background: none;
  border: none;
  padding: var(--spacing-xs);
  cursor: pointer;
  color: var(--color-gray-500);
  border-radius: var(--radius-sm);
  transition: all var(--transition-fast);
}

.modal-close:hover {
  background-color: var(--color-gray-100);
  color: var(--color-gray-700);
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
  margin-bottom: var(--spacing-xs);
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  transition: border-color var(--transition-fast);
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: var(--color-primary);
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-gray-200);
  margin-top: var(--spacing-lg);
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: var(--color-primary);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color var(--transition-fast);
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
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-secondary:hover {
  background-color: var(--color-gray-50);
}

/* Delete Modal Styles */
.modal-delete {
  max-width: 420px;
  padding: var(--spacing-xl);
  text-align: center;
}

.delete-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto var(--spacing-lg);
  background-color: #fef2f2;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #dc2626;
}

.delete-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-md);
}

.delete-message {
  font-size: 0.9375rem;
  color: var(--color-gray-700);
  margin: 0 0 var(--spacing-md);
  line-height: 1.5;
}

.delete-message strong {
  color: var(--color-gray-900);
}

.delete-warning {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  background-color: #fef3c7;
  border-radius: var(--radius-md);
  font-size: 0.8125rem;
  color: #92400e;
  text-align: left;
  margin-bottom: var(--spacing-xl);
}

.delete-warning svg {
  flex-shrink: 0;
  margin-top: 2px;
}

.delete-actions {
  display: flex;
  gap: var(--spacing-md);
  justify-content: center;
}

.btn-cancel,
.btn-delete {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  font-size: 0.8125rem;
  font-weight: 500;
  cursor: pointer;
  transition: all var(--transition-fast);
  min-width: 100px;
}

.btn-cancel {
  background-color: white;
  color: var(--color-gray-700);
  border: 1px solid var(--color-gray-300);
}

.btn-cancel:hover {
  background-color: var(--color-gray-50);
  border-color: var(--color-gray-400);
}

.btn-delete {
  background-color: #dc2626;
  color: white;
  border: none;
}

.btn-delete:hover {
  background-color: #b91c1c;
}
</style>
