<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useBoardStore } from '@features/boards/stores/board.store';
import { useTaskStore } from '@features/tasks/stores/task.store';
import { useLabelStore } from '@features/labels/stores/label.store';
import LabelPicker from '@features/labels/components/LabelPicker.vue';
import type { ColumnResponse, BoardTask, BoardLabel } from '@features/boards/types/board.types';

const route = useRoute();
const router = useRouter();
const boardStore = useBoardStore();
const taskStore = useTaskStore();
const labelStore = useLabelStore();

const boardId = computed(() => route.params.boardId as string);
const projectId = computed(() => route.params.projectId as string);

const today = computed(() => {
  const now = new Date();
  return now.toISOString().split('T')[0];
});

// Drag and drop state
const draggedTaskId = ref<string | null>(null);
const draggedSourceColumnId = ref<string | null>(null);
const dragOverColumnId = ref<string | null>(null);

// Task creation state
const showCreateTask = ref<string | null>(null);
const newTaskTitle = ref('');
const newTaskDescription = ref('');
const newTaskPriority = ref('Medium');
const newTaskDueDate = ref('');
const isCreatingTask = ref(false);

// Task edit state
const showEditTask = ref<string | null>(null);
const editingTask = ref<any>(null);
const editTaskTitle = ref('');
const editTaskDescription = ref('');
const editTaskPriority = ref('Medium');
const editTaskDueDate = ref('');
const editTaskLabels = ref<BoardLabel[]>([]);
const isUpdatingTask = ref(false);

// Label filter state
const selectedLabelFilter = ref<string | null>(null);

onMounted(async () => {
  if (boardId.value) {
    await boardStore.fetchBoard(boardId.value);
  }
  if (projectId.value) {
    await labelStore.fetchProjectLabels(projectId.value);
  }
});

watch(boardId, async (newId) => {
  if (newId) {
    await boardStore.fetchBoard(newId);
  }
});

const board = computed(() => boardStore.currentBoard);
const columns = computed(() => board.value?.columns ?? []);

const filteredColumns = computed(() => {
  if (!selectedLabelFilter.value) return columns.value;
  return columns.value.map(column => ({
    ...column,
    tasks: column.tasks.filter(task =>
      task.labels?.some(label => label.id === selectedLabelFilter.value)
    )
  }));
});

// ============== Drag and Drop ==============

function onDragStart(event: DragEvent, task: BoardTask, columnId: string) {
  console.log('[Drag] onDragStart', { taskId: task.id, taskTitle: task.title, columnId });
  if (!event.dataTransfer) return;

  draggedTaskId.value = task.id;
  draggedSourceColumnId.value = columnId;
  event.dataTransfer.effectAllowed = 'move';
  event.dataTransfer.setData('application/x-task-id', task.id);
  event.dataTransfer.setData('application/x-source-column', columnId);

  setTimeout(() => {
    const target = document.querySelector(`[data-task-id="${task.id}"]`) as HTMLElement;
    if (target) target.classList.add('dragging');
  }, 0);
}

function onDragEnd(_event: DragEvent) {
  console.log('[Drag] onDragEnd');
  draggedTaskId.value = null;
  draggedSourceColumnId.value = null;
  dragOverColumnId.value = null;
}

function onDragOverColumn(event: DragEvent, columnId: string) {
  event.preventDefault();
  if (!event.dataTransfer) return;
  event.dataTransfer.dropEffect = 'move';
  dragOverColumnId.value = columnId;
}

async function onColumnDrop(event: DragEvent, targetColumnId: string) {
  console.log('[Drag] onColumnDrop', { targetColumnId, draggedTaskId: draggedTaskId.value, sourceColumnId: draggedSourceColumnId.value });
  event.preventDefault();

  const taskId = draggedTaskId.value;
  const sourceColumnId = draggedSourceColumnId.value;

  // Clear state
  draggedTaskId.value = null;
  draggedSourceColumnId.value = null;
  dragOverColumnId.value = null;

  if (!taskId) return;

  // Find source column and task
  const sourceColumn = columns.value.find(c => c.id === sourceColumnId);
  const targetColumn = columns.value.find(c => c.id === targetColumnId);
  if (!sourceColumn || !targetColumn) return;

  const task = sourceColumn.tasks.find(t => t.id === taskId);
  if (!task) return;

  // Move to end of target column
  const targetIndex = targetColumn.tasks.length;
  await moveTask(task, sourceColumnId!, targetColumnId, targetIndex);
}

function isColumnDropTarget(columnId: string): boolean {
  return dragOverColumnId.value === columnId && draggedSourceColumnId.value !== columnId;
}

function onTaskClick(task: BoardTask, columnId: string) {
  if (!draggedTaskId.value) {
    openEditTask(task, columnId);
  }
}

async function moveTask(task: BoardTask, sourceColumnId: string, targetColumnId: string, targetIndex: number) {
  if (isMovingTask.value) return;
  isMovingTask.value = true;

  try {
    updateLocalState(task, sourceColumnId, targetColumnId, targetIndex);

    const result = await taskStore.moveTask(task.id, {
      targetColumnId,
      targetPosition: targetIndex,
    });

    if (!result) {
      rollbackState();
    }
  } catch (err) {
    console.error('Failed to move task:', err);
    rollbackState();
  } finally {
    isMovingTask.value = false;
  }
}

function updateLocalState(task: BoardTask, sourceColumnId: string, targetColumnId: string, targetIndex: number) {
  const sourceColumn = columns.value.find(c => c.id === sourceColumnId);
  const targetColumn = columns.value.find(c => c.id === targetColumnId);
  if (!sourceColumn || !targetColumn) return;

  const taskIndex = sourceColumn.tasks.findIndex(t => t.id === task.id);
  if (taskIndex === -1) return;

  const [movedTask] = sourceColumn.tasks.splice(taskIndex, 1);
  if (!movedTask) return;

  movedTask.columnId = targetColumnId;

  let adjustedIndex = targetIndex;
  if (sourceColumnId === targetColumnId && taskIndex < targetIndex) {
    adjustedIndex--;
  }

  targetColumn.tasks.splice(adjustedIndex, 0, movedTask);
  sourceColumn.taskCount = sourceColumn.tasks.length;
  targetColumn.taskCount = targetColumn.tasks.length;
}

function rollbackState() {
  boardStore.fetchBoard(boardId.value);
}

// ============== Existing Functions ==============

function goBack() {
  router.push(`/projects/${projectId.value}`);
}

function getColumnStyle(column: ColumnResponse) {
  return {
    '--column-color': column.color || '#6b7280'
  };
}

function formatDate(dateString?: string): string {
  if (!dateString) return '';
  const date = new Date(dateString + 'Z');
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function getPriorityClass(priority: string): string {
  switch (priority) {
    case 'High': return 'priority-high';
    case 'Medium': return 'priority-medium';
    case 'Low': return 'priority-low';
    case 'Critical': return 'priority-critical';
    default: return 'priority-none';
  }
}

function isOverdue(dueDate?: string): boolean {
  if (!dueDate) return false;
  return new Date(dueDate) < new Date();
}

// Create task functions
function openCreateTask(columnId: string) {
  showCreateTask.value = columnId;
  newTaskTitle.value = '';
  newTaskDescription.value = '';
  newTaskPriority.value = 'Medium';
  newTaskDueDate.value = '';
}

function closeCreateTask() {
  showCreateTask.value = null;
  newTaskTitle.value = '';
  newTaskDescription.value = '';
  newTaskPriority.value = 'Medium';
  newTaskDueDate.value = '';
}

async function handleCreateTask() {
  if (!newTaskTitle.value.trim() || !showCreateTask.value) return;

  isCreatingTask.value = true;
  try {
    const result = await taskStore.createTask({
      columnId: showCreateTask.value,
      title: newTaskTitle.value.trim(),
      description: newTaskDescription.value.trim() || undefined,
      priority: newTaskPriority.value as any,
      dueDate: newTaskDueDate.value || undefined,
    });

    if (result) {
      await boardStore.fetchBoard(boardId.value);
      closeCreateTask();
    }
  } catch (err) {
    console.error('Failed to create task:', err);
  } finally {
    isCreatingTask.value = false;
  }
}

// Edit task functions
function openEditTask(task: any, columnId: string) {
  showEditTask.value = columnId;
  editingTask.value = task;
  editTaskTitle.value = task.title;
  editTaskDescription.value = task.description || '';
  editTaskPriority.value = task.priority;
  editTaskDueDate.value = task.dueDate ? task.dueDate.split('T')[0] : '';
  editTaskLabels.value = task.labels || [];
}

function closeEditTask() {
  showEditTask.value = null;
  editingTask.value = null;
  editTaskTitle.value = '';
  editTaskDescription.value = '';
  editTaskPriority.value = 'Medium';
  editTaskDueDate.value = '';
  editTaskLabels.value = [];
}

async function handleUpdateTask() {
  if (!editTaskTitle.value.trim() || !editingTask.value) return;

  isUpdatingTask.value = true;
  try {
    const result = await taskStore.updateTask(editingTask.value.id, {
      title: editTaskTitle.value.trim(),
      description: editTaskDescription.value.trim() || undefined,
      priority: editTaskPriority.value as any,
      dueDate: editTaskDueDate.value || null,
    });

    if (result) {
      const currentLabelIds = editingTask.value.labels?.map((l: BoardLabel) => l.id) || [];
      const newLabelIds = editTaskLabels.value.map(l => l.id);

      const toAdd = newLabelIds.filter((id: string) => !currentLabelIds.includes(id));
      const toRemove = currentLabelIds.filter((id: string) => !newLabelIds.includes(id));

      for (const labelId of toAdd) {
        await labelStore.addLabelsToTask(editingTask.value.id, [labelId]);
      }

      for (const labelId of toRemove) {
        await labelStore.removeLabelFromTask(editingTask.value.id, labelId);
      }

      await boardStore.fetchBoard(boardId.value);
      closeEditTask();
    }
  } catch (err) {
    console.error('Failed to update task:', err);
  } finally {
    isUpdatingTask.value = false;
  }
}

// Label filter functions
function filterByLabel(labelId: string) {
  if (selectedLabelFilter.value === labelId) {
    selectedLabelFilter.value = null;
  } else {
    selectedLabelFilter.value = labelId;
  }
}

function clearLabelFilter() {
  selectedLabelFilter.value = null;
}

async function handleDeleteTask(taskId: string) {
  if (!confirm('Are you sure you want to delete this task?')) return;

  try {
    const success = await taskStore.deleteTask(taskId);
    if (success) {
      await boardStore.fetchBoard(boardId.value);
      closeEditTask();
    }
  } catch (err) {
    console.error('Failed to delete task:', err);
  }
}

// Refs for async operations
const isMovingTask = ref(false);
</script>

<template>
  <div class="kanban-view">
    <!-- Header -->
    <div class="kanban-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
            <path d="M19 12H5M12 19l-7-7 7-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <span>Back</span>
        </button>
        <div class="header-info">
          <h1 class="board-title">{{ board?.name || 'Loading...' }}</h1>
          <p v-if="board?.description" class="board-description">{{ board.description }}</p>
        </div>
      </div>
      <div class="header-right">
        <span v-if="board?.isDefault" class="badge badge-default">Default</span>
        <span class="badge badge-type">{{ board?.type }}</span>
      </div>
    </div>

    <!-- Label Filter Bar -->
    <div v-if="labelStore.getProjectLabels(projectId).length > 0" class="label-filter-bar">
      <span class="filter-label">Filter:</span>
      <div class="filter-labels">
        <button
          v-for="label in labelStore.getProjectLabels(projectId)"
          :key="label.id"
          class="filter-label-btn"
          :class="{ active: selectedLabelFilter === label.id }"
          :style="{
            backgroundColor: selectedLabelFilter === label.id ? label.color + '30' : 'transparent',
            borderColor: label.color,
            color: label.color
          }"
          @click="filterByLabel(label.id)"
        >
          {{ label.name }}
        </button>
        <button
          v-if="selectedLabelFilter"
          class="clear-filter-btn"
          @click="clearLabelFilter"
        >
          Clear filter
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="boardStore.isLoading && !board" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading board...</p>
    </div>

    <!-- Kanban Board -->
    <div v-else class="kanban-board">
      <div
        v-for="column in filteredColumns"
        :key="column.id"
        class="kanban-column"
        :style="getColumnStyle(column)"
        @dragover.prevent="onDragOverColumn($event, column.id)"
        @drop.prevent="onColumnDrop($event, column.id)"
      >
        <div class="column-header">
          <div class="column-title-row">
            <div class="column-indicator" :style="{ backgroundColor: column.color }"></div>
            <h3 class="column-title">{{ column.name }}</h3>
            <span class="column-count">{{ column.tasks.length }}</span>
          </div>
          <button class="btn-add-task" @click="openCreateTask(column.id)" title="Add task">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <span>Add</span>
          </button>
        </div>

        <!-- Create Task Form -->
        <div v-if="showCreateTask === column.id" class="task-form">
          <input
            v-model="newTaskTitle"
            type="text"
            placeholder="Task title..."
            class="task-input"
            @keyup.enter="handleCreateTask"
            @keyup.escape="closeCreateTask"
          />
          <textarea
            v-model="newTaskDescription"
            placeholder="Description (optional)"
            class="task-textarea"
            rows="2"
          ></textarea>
          <div class="form-row">
            <select v-model="newTaskPriority" class="task-select">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
            <input
              v-model="newTaskDueDate"
              type="date"
              class="task-date"
              :min="today"
            />
          </div>
          <div class="form-actions">
            <button class="btn-cancel" @click="closeCreateTask">Cancel</button>
            <button
              class="btn-create"
              :disabled="!newTaskTitle.trim() || isCreatingTask"
              @click="handleCreateTask"
            >
              {{ isCreatingTask ? 'Creating...' : 'Create' }}
            </button>
          </div>
        </div>

        <!-- Empty Column State -->
        <div v-if="column.tasks.length === 0 && !showCreateTask" class="empty-column">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.5" stroke-dasharray="4 2"/>
            <path d="M12 10v4m-2-2h4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          <span>No tasks</span>
        </div>

        <!-- Tasks List -->
        <div
          class="tasks-list"
          :class="{ 'drag-over': isColumnDropTarget(column.id) }"
        >
          <div
            v-for="task in column.tasks"
            :key="task.id"
            :data-task-id="task.id"
            class="task-card"
            :class="{ 'dragging': draggedTaskId === task.id }"
            draggable="true"
            @dragstart="onDragStart($event, task, column.id)"
            @dragend="onDragEnd"
            @click.stop="onTaskClick(task, column.id)"
          >
            <div class="task-header">
              <span v-if="task.taskKey" class="task-key">{{ task.taskKey }}</span>
              <span :class="['task-priority', getPriorityClass(task.priority)]">
                {{ task.priority }}
              </span>
            </div>
            <h4 class="task-title">{{ task.title }}</h4>
            <p v-if="task.description" class="task-description">{{ task.description }}</p>
            <div v-if="task.labels && task.labels.length > 0" class="task-labels">
              <span
                v-for="label in task.labels"
                :key="label.id"
                class="task-label-dot"
                :style="{ backgroundColor: label.color }"
                :title="label.name"
              ></span>
            </div>
            <div class="task-footer">
              <span v-if="task.dueDate" :class="['task-due', { overdue: isOverdue(task.dueDate) && task.status !== 'Done' }]">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
                  <rect x="3" y="4" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2"/>
                  <path d="M16 2v4M8 2v4M3 10h18" stroke="currentColor" stroke-width="2"/>
                </svg>
                {{ formatDate(task.dueDate) }}
              </span>
              <span v-if="task.assigneeName" class="task-assignee">
                {{ task.assigneeName }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Task Modal -->
    <Teleport to="body">
      <div v-if="showEditTask" class="modal-overlay" @click.self="closeEditTask">
        <div class="modal">
          <div class="modal-header">
            <h2>Edit Task</h2>
            <button class="modal-close" @click="closeEditTask">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>
          <form @submit.prevent="handleUpdateTask" class="modal-body">
            <div class="form-group">
              <label for="edit-title">Title *</label>
              <input
                id="edit-title"
                v-model="editTaskTitle"
                type="text"
                placeholder="Task title"
                required
              />
            </div>
            <div class="form-group">
              <label for="edit-description">Description</label>
              <textarea
                id="edit-description"
                v-model="editTaskDescription"
                placeholder="Task description"
                rows="3"
              ></textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label for="edit-priority">Priority</label>
                <select id="edit-priority" v-model="editTaskPriority">
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              <div class="form-group">
                <label for="edit-due">Due Date</label>
                <input
                  id="edit-due"
                  v-model="editTaskDueDate"
                  type="date"
                  :min="today"
                />
              </div>
            </div>
            <div class="form-group">
              <label>Labels</label>
              <LabelPicker
                v-if="projectId"
                :project-id="projectId"
                :task-id="editingTask?.id || ''"
                v-model="editTaskLabels"
              />
            </div>
            <div class="modal-footer">
              <button type="button" class="btn-danger-text" @click="handleDeleteTask(editingTask?.id)">
                Delete
              </button>
              <div class="footer-right">
                <button type="button" class="btn-secondary" @click="closeEditTask">Cancel</button>
                <button type="submit" class="btn-primary" :disabled="!editTaskTitle.trim() || isUpdatingTask">
                  {{ isUpdatingTask ? 'Saving...' : 'Save Changes' }}
                </button>
              </div>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.kanban-view {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.kanban-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: var(--spacing-lg);
  background-color: white;
  border-bottom: 1px solid var(--color-gray-200);
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
}

.btn-back {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  color: var(--color-gray-600);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
}

.btn-back:hover {
  background-color: var(--color-gray-100);
  border-color: var(--color-gray-300);
  color: var(--color-gray-800);
}

.header-info {
  display: flex;
  flex-direction: column;
}

.board-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin: 0;
}

.board-description {
  font-size: 0.875rem;
  color: var(--color-gray-500);
  margin: 4px 0 0;
}

.header-right {
  display: flex;
  gap: var(--spacing-sm);
}

.badge {
  padding: 4px 10px;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 500;
}

.badge-default {
  background-color: var(--color-gray-100);
  color: var(--color-gray-600);
}

.badge-type {
  background-color: var(--color-primary-light);
  color: var(--color-primary);
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

.kanban-board {
  display: flex;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  overflow-x: auto;
  flex: 1;
  scroll-snap-type: x mandatory;
}

.kanban-board > .kanban-column {
  scroll-snap-align: start;
}

.kanban-column {
  flex: 0 0 300px;
  background-color: var(--color-gray-100);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  max-height: calc(100vh - 180px);
}

.empty-column {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  color: var(--color-gray-400);
  font-size: 0.8125rem;
  text-align: center;
  min-height: 100px;
}

.empty-column svg {
  margin-bottom: var(--spacing-sm);
  opacity: 0.5;
}

.column-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-gray-200);
}

.column-title-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.column-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.column-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-gray-700);
  margin: 0;
}

.column-count {
  font-size: 0.75rem;
  color: var(--color-gray-500);
  background-color: var(--color-gray-200);
  padding: 2px 6px;
  border-radius: var(--radius-sm);
}

.btn-add-task {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: transparent;
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-sm);
  color: var(--color-gray-500);
  font-size: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-add-task:hover {
  background-color: var(--color-gray-200);
  color: var(--color-gray-700);
}

/* Task Form */
.task-form {
  padding: var(--spacing-md);
  background-color: white;
  border-bottom: 1px solid var(--color-gray-200);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.task-input,
.task-textarea,
.task-select,
.task-date {
  width: 100%;
  padding: var(--spacing-sm);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
}

.task-input:focus,
.task-textarea:focus {
  outline: none;
  border-color: var(--color-primary);
}

.task-textarea {
  resize: none;
}

.form-row {
  display: flex;
  gap: var(--spacing-sm);
}

.task-select {
  flex: 1;
}

.task-date {
  flex: 1;
}

.form-actions {
  display: flex;
  gap: var(--spacing-sm);
  justify-content: flex-end;
}

.btn-cancel,
.btn-create {
  padding: var(--spacing-xs) var(--spacing-md);
  border-radius: var(--radius-md);
  font-size: 0.8125rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel {
  background: transparent;
  border: 1px solid var(--color-gray-300);
  color: var(--color-gray-600);
}

.btn-cancel:hover {
  background-color: var(--color-gray-50);
}

.btn-create {
  background-color: var(--color-primary);
  border: none;
  color: white;
}

.btn-create:hover:not(:disabled) {
  background-color: var(--color-primary-dark);
}

.btn-create:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Tasks List */
.tasks-list {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-md);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  min-height: 100px;
  transition: background-color 0.2s;
  user-select: none;
  -webkit-user-select: none;
}

.tasks-list.drag-over {
  background-color: rgba(59, 130, 246, 0.05);
}

.task-card {
  background-color: white;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  padding: var(--spacing-md);
  cursor: grab;
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
  user-select: none;
  -webkit-user-select: none;
  touch-action: none;
}

.task-card:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.task-card.dragging {
  opacity: 0.5;
  cursor: grabbing;
}

.task-card:active {
  cursor: grabbing;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xs);
}

.task-key {
  font-size: 0.6875rem;
  font-weight: 500;
  color: var(--color-gray-500);
}

.task-priority {
  font-size: 0.6875rem;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  font-weight: 500;
}

.priority-low {
  background-color: #dbeafe;
  color: #1d4ed8;
}

.priority-medium {
  background-color: #fef3c7;
  color: #b45309;
}

.priority-high {
  background-color: #fee2e2;
  color: #dc2626;
}

.priority-critical {
  background-color: #7f1d1d;
  color: white;
}

.task-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-xs);
}

.task-description {
  font-size: 0.75rem;
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-sm);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.task-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.75rem;
  color: var(--color-gray-500);
}

.task-due {
  display: flex;
  align-items: center;
  gap: 4px;
}

.task-due.overdue {
  color: #dc2626;
}

.task-assignee {
  background-color: var(--color-gray-100);
  padding: 2px 6px;
  border-radius: var(--radius-sm);
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
  padding: var(--spacing-lg);
}

.modal {
  width: 100%;
  max-width: 500px;
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
  transition: all 0.2s;
}

.modal-close:hover {
  background-color: var(--color-gray-100);
  color: var(--color-gray-700);
}

.modal-body {
  padding: var(--spacing-lg);
}

.form-group {
  margin-bottom: var(--spacing-md);
}

.form-group label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-700);
  margin-bottom: var(--spacing-xs);
}

.form-group input,
.form-group textarea,
.form-group select {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group textarea:focus,
.form-group select:focus {
  outline: none;
  border-color: var(--color-primary);
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.modal-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-gray-200);
  margin-top: var(--spacing-lg);
}

.footer-right {
  display: flex;
  gap: var(--spacing-sm);
}

.btn-danger-text {
  background: none;
  border: none;
  color: #dc2626;
  font-size: 0.875rem;
  cursor: pointer;
  padding: var(--spacing-sm);
}

.btn-danger-text:hover {
  text-decoration: underline;
}

.btn-primary {
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: var(--color-primary);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
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
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-secondary:hover {
  background-color: var(--color-gray-50);
}

/* Label Filter Bar */
.label-filter-bar {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: var(--color-gray-50);
  border-bottom: 1px solid var(--color-gray-200);
}

.filter-label {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-gray-500);
}

.filter-labels {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.filter-label-btn {
  padding: 4px 10px;
  border: 1px solid;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
}

.filter-label-btn:hover {
  filter: brightness(0.95);
}

.filter-label-btn.active {
  font-weight: 600;
}

.clear-filter-btn {
  padding: 4px 10px;
  background: transparent;
  border: 1px dashed var(--color-gray-300);
  border-radius: 9999px;
  font-size: 0.75rem;
  color: var(--color-gray-500);
  cursor: pointer;
  transition: all 0.15s ease;
}

.clear-filter-btn:hover {
  background-color: var(--color-gray-100);
  color: var(--color-gray-700);
}

/* Task Labels */
.task-labels {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: var(--spacing-xs);
  margin-bottom: var(--spacing-xs);
}

.task-label-dot {
  width: 24px;
  height: 6px;
  border-radius: 3px;
  cursor: pointer;
  transition: transform 0.15s ease;
}

.task-label-dot:hover {
  transform: scaleY(1.5);
}

/* ==================== RESPONSIVE STYLES ==================== */

@media (max-width: 768px) {
  .kanban-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
    padding: var(--spacing-md);
  }

  .header-left {
    width: 100%;
  }

  .header-right {
    width: 100%;
    justify-content: flex-start;
  }

  .board-title {
    font-size: 1.25rem;
  }

  .kanban-board {
    padding: var(--spacing-md);
    gap: var(--spacing-md);
  }

  .kanban-column {
    flex: 0 0 280px;
    max-height: calc(100vh - 220px);
  }

  .label-filter-bar {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
    flex-wrap: nowrap;
  }

  .filter-labels {
    flex-wrap: nowrap;
  }

  .filter-label-btn {
    flex-shrink: 0;
  }

  .modal-overlay {
    padding: 0;
    align-items: flex-end;
  }

  .modal {
    max-height: 85vh;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
    max-width: 100%;
  }
}

@media (max-width: 480px) {
  .btn-back span {
    display: none;
  }

  .btn-back {
    padding: 8px;
  }

  .kanban-column {
    flex: 0 0 260px;
  }

  .column-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }

  .btn-add-task span {
    display: none;
  }

  .btn-add-task {
    padding: 4px;
  }

  .task-card {
    padding: var(--spacing-sm);
  }

  .task-footer {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }

  .modal {
    max-height: 90vh;
  }

  .modal-body {
    padding: var(--spacing-md);
  }

  .form-row {
    flex-direction: column;
  }

  .modal-footer {
    flex-direction: column-reverse;
    gap: var(--spacing-sm);
  }

  .modal-footer .btn-secondary,
  .modal-footer .btn-primary {
    width: 100%;
    justify-content: center;
  }
}
</style>
