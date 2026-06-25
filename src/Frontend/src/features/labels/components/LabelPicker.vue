<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useLabelStore } from '../stores/label.store';
import LabelBadge from './LabelBadge.vue';
import type { LabelListItemResponse, CreateLabelRequest } from '../types/label.types';

const props = defineProps<{
  projectId: string;
  taskId: string;
  modelValue: LabelListItemResponse[];
}>();

const emit = defineEmits<{
  'update:modelValue': [labels: LabelListItemResponse[]];
}>();

const labelStore = useLabelStore();

// Local state
const showPicker = ref(false);
const showCreateForm = ref(false);
const searchQuery = ref('');
const selectedLabelIds = ref<Set<string>>(new Set(props.modelValue.map((l) => l.id)));
const newLabelName = ref('');
const newLabelColor = ref('#3b82f6');

// Preset colors for label creation
const presetColors = [
  '#ef4444', // red
  '#f97316', // orange
  '#f59e0b', // amber
  '#eab308', // yellow
  '#84cc16', // lime
  '#22c55e', // green
  '#10b981', // emerald
  '#14b8a6', // teal
  '#06b6d4', // cyan
  '#0ea5e9', // sky
  '#3b82f6', // blue
  '#6366f1', // indigo
  '#8b5cf6', // violet
  '#a855f7', // purple
  '#d946ef', // fuchsia
  '#ec4899', // pink
];

// Get project labels
const projectLabels = computed(() => {
  return labelStore.getProjectLabels(props.projectId);
});

// Filtered labels based on search
const filteredLabels = computed(() => {
  const query = searchQuery.value.toLowerCase();
  if (!query) return projectLabels.value;
  return projectLabels.value.filter((label) =>
    label.name.toLowerCase().includes(query)
  );
});

// Labels that are not yet assigned
const availableLabels = computed(() => {
  return filteredLabels.value.filter((label) => !selectedLabelIds.value.has(label.id));
});

// Assigned labels
const assignedLabels = computed(() => {
  return projectLabels.value.filter((label) => selectedLabelIds.value.has(label.id));
});

// Watch for external model changes
watch(
  () => props.modelValue,
  (newLabels) => {
    selectedLabelIds.value = new Set(newLabels.map((l) => l.id));
  },
  { deep: true }
);

// Fetch labels on mount
watch(
  () => props.projectId,
  (newProjectId) => {
    if (newProjectId) {
      labelStore.fetchProjectLabels(newProjectId);
    }
  },
  { immediate: true }
);

// Fetch labels when picker opens
watch(showPicker, (isOpen) => {
  if (isOpen && props.projectId) {
    labelStore.fetchProjectLabels(props.projectId);
  }
});

// Toggle label selection
function toggleLabel(label: LabelListItemResponse) {
  const newSet = new Set(selectedLabelIds.value);
  if (newSet.has(label.id)) {
    newSet.delete(label.id);
  } else {
    newSet.add(label.id);
  }
  selectedLabelIds.value = newSet;
  emitUpdate();
}

// Remove label from selection
function removeLabel(labelId: string) {
  const newSet = new Set(selectedLabelIds.value);
  newSet.delete(labelId);
  selectedLabelIds.value = newSet;
  emitUpdate();
}

// Emit update to parent
function emitUpdate() {
  const labels = projectLabels.value.filter((label) => selectedLabelIds.value.has(label.id));
  emit('update:modelValue', labels);
}

// Create new label
async function handleCreateLabel() {
  if (!newLabelName.value.trim()) return;

  const data: CreateLabelRequest = {
    name: newLabelName.value.trim(),
    color: newLabelColor.value,
  };

  const label = await labelStore.createLabel(props.projectId, data);
  if (label) {
    // Auto-select the newly created label
    const newSet = new Set(selectedLabelIds.value);
    newSet.add(label.id);
    selectedLabelIds.value = newSet;
    emitUpdate();
  }

  // Reset form
  showCreateForm.value = false;
  newLabelName.value = '';
  newLabelColor.value = '#3b82f6';
}

// Close picker
function closePicker() {
  showPicker.value = false;
  showCreateForm.value = false;
  searchQuery.value = '';
}
</script>

<template>
  <div class="label-picker">
    <!-- Current Labels Display -->
    <div class="current-labels">
      <div v-if="assignedLabels.length > 0" class="labels-row">
        <LabelBadge
          v-for="label in assignedLabels"
          :key="label.id"
          :label="label"
          removable
          size="sm"
          @remove="removeLabel"
        />
      </div>
      <button
        type="button"
        class="add-label-btn"
        @click="showPicker = !showPicker"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
          <path
            d="M12 5v14M5 12h14"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          />
        </svg>
        <span>Labels</span>
      </button>
    </div>

    <!-- Picker Dropdown -->
    <Teleport to="body">
      <div v-if="showPicker" class="picker-overlay" @click.self="closePicker">
        <div class="picker-dropdown">
          <div class="picker-header">
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search labels..."
              class="search-input"
              autofocus
            />
            <button type="button" class="close-btn" @click="closePicker">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path
                  d="M18 6L6 18M6 6l12 12"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                />
              </svg>
            </button>
          </div>

          <!-- Create New Label -->
          <div class="create-section">
            <button
              v-if="!showCreateForm"
              type="button"
              class="create-btn"
              @click="showCreateForm = true"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path
                  d="M12 5v14M5 12h14"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                />
              </svg>
              <span>Create new label</span>
            </button>

            <div v-else class="create-form">
              <div class="form-row">
                <input
                  v-model="newLabelName"
                  type="text"
                  placeholder="Label name"
                  class="label-input"
                  @keyup.enter="handleCreateLabel"
                />
              </div>
              <div class="color-picker">
                <div class="color-grid">
                  <button
                    v-for="color in presetColors"
                    :key="color"
                    type="button"
                    class="color-btn"
                    :class="{ active: newLabelColor === color }"
                    :style="{ backgroundColor: color }"
                    @click="newLabelColor = color"
                  />
                </div>
              </div>
              <div class="form-actions">
                <button type="button" class="cancel-btn" @click="showCreateForm = false">
                  Cancel
                </button>
                <button
                  type="button"
                  class="save-btn"
                  :disabled="!newLabelName.trim()"
                  @click="handleCreateLabel"
                >
                  Create
                </button>
              </div>
            </div>
          </div>

          <!-- Available Labels -->
          <div class="labels-list">
            <div v-if="availableLabels.length === 0 && !showCreateForm" class="no-labels">
              No labels found
            </div>
            <button
              v-for="label in availableLabels"
              :key="label.id"
              type="button"
              class="label-item"
              @click="toggleLabel(label)"
            >
              <span
                class="label-color"
                :style="{ backgroundColor: label.color }"
              ></span>
              <span class="label-name">{{ label.name }}</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.label-picker {
  position: relative;
}

.current-labels {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.labels-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.add-label-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: transparent;
  border: 1px dashed var(--color-gray-300, #d1d5db);
  border-radius: 9999px;
  color: var(--color-gray-500, #6b7280);
  font-size: 0.75rem;
  cursor: pointer;
  transition: all 0.15s ease;
}

.add-label-btn:hover {
  background-color: var(--color-gray-50, #f9fafb);
  border-color: var(--color-gray-400, #9ca3af);
  color: var(--color-gray-700, #374151);
}

/* Dropdown styles */
.picker-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding-top: 100px;
}

.picker-dropdown {
  width: 280px;
  max-height: 400px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.picker-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  border-bottom: 1px solid var(--color-gray-200, #e5e7eb);
}

.search-input {
  flex: 1;
  padding: 6px 10px;
  border: 1px solid var(--color-gray-200, #e5e7eb);
  border-radius: 6px;
  font-size: 0.875rem;
  outline: none;
}

.search-input:focus {
  border-color: var(--color-primary, #3b82f6);
}

.close-btn {
  padding: 4px;
  background: none;
  border: none;
  color: var(--color-gray-400, #9ca3af);
  cursor: pointer;
  border-radius: 4px;
}

.close-btn:hover {
  background-color: var(--color-gray-100, #f3f4f6);
  color: var(--color-gray-600, #4b5563);
}

.create-section {
  padding: 8px 12px;
  border-bottom: 1px solid var(--color-gray-200, #e5e7eb);
}

.create-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px;
  background: none;
  border: none;
  color: var(--color-gray-600, #4b5563);
  font-size: 0.875rem;
  cursor: pointer;
  border-radius: 4px;
  transition: background-color 0.15s;
}

.create-btn:hover {
  background-color: var(--color-gray-100, #f3f4f6);
}

.create-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.label-input {
  width: 100%;
  padding: 6px 10px;
  border: 1px solid var(--color-gray-200, #e5e7eb);
  border-radius: 4px;
  font-size: 0.875rem;
}

.label-input:focus {
  outline: none;
  border-color: var(--color-primary, #3b82f6);
}

.color-picker {
  padding: 4px 0;
}

.color-grid {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 4px;
}

.color-btn {
  width: 24px;
  height: 24px;
  border: 2px solid transparent;
  border-radius: 4px;
  cursor: pointer;
  transition: transform 0.1s;
}

.color-btn:hover {
  transform: scale(1.1);
}

.color-btn.active {
  border-color: var(--color-gray-900, #111827);
}

.form-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}

.cancel-btn,
.save-btn {
  padding: 4px 12px;
  border-radius: 4px;
  font-size: 0.8125rem;
  cursor: pointer;
}

.cancel-btn {
  background: none;
  border: 1px solid var(--color-gray-200, #e5e7eb);
  color: var(--color-gray-600, #4b5563);
}

.save-btn {
  background-color: var(--color-primary, #3b82f6);
  border: none;
  color: white;
}

.save-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.labels-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.no-labels {
  padding: 16px;
  text-align: center;
  color: var(--color-gray-500, #6b7280);
  font-size: 0.875rem;
}

.label-item {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px;
  background: none;
  border: none;
  cursor: pointer;
  border-radius: 4px;
  transition: background-color 0.15s;
}

.label-item:hover {
  background-color: var(--color-gray-100, #f3f4f6);
}

.label-color {
  width: 16px;
  height: 16px;
  border-radius: 4px;
  flex-shrink: 0;
}

.label-name {
  font-size: 0.875rem;
  color: var(--color-gray-700, #374151);
}
</style>
