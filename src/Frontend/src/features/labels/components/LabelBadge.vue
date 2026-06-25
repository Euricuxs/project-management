<script setup lang="ts">
import type { LabelListItemResponse } from '../types/label.types';

defineProps<{
  label: LabelListItemResponse;
  removable?: boolean;
  clickable?: boolean;
  size?: 'sm' | 'md';
}>();

const emit = defineEmits<{
  click: [];
  remove: [labelId: string];
}>();
</script>

<template>
  <span
    class="label-badge"
    :class="{
      clickable,
      removable,
      'label-badge-sm': size === 'sm',
      'label-badge-md': size === 'md' || !size,
    }"
    :style="{
      backgroundColor: label.color + '20',
      color: label.color,
      borderColor: label.color + '40',
    }"
    :title="label.name"
    @click="clickable && emit('click')"
  >
    <span
      class="label-dot"
      :style="{ backgroundColor: label.color }"
    ></span>
    <span class="label-name">{{ label.name }}</span>
    <button
      v-if="removable"
      class="remove-btn"
      type="button"
      @click.stop="emit('remove', label.id)"
      :title="`Remove ${label.name}`"
    >
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
        <path
          d="M18 6L6 18M6 6l12 12"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
        />
      </svg>
    </button>
  </span>
</template>

<style scoped>
.label-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border-radius: 9999px;
  border: 1px solid;
  font-weight: 500;
  transition: all 0.15s ease;
  user-select: none;
}

.label-badge-sm {
  padding: 2px 8px;
  font-size: 0.6875rem;
}

.label-badge-md {
  padding: 4px 10px;
  font-size: 0.75rem;
}

.label-badge.clickable {
  cursor: pointer;
}

.label-badge.clickable:hover {
  filter: brightness(0.95);
  transform: translateY(-1px);
}

.label-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;
}

.label-name {
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.remove-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  margin-left: 2px;
  background: none;
  border: none;
  cursor: pointer;
  opacity: 0.6;
  transition: opacity 0.15s ease;
  color: inherit;
}

.remove-btn:hover {
  opacity: 1;
}
</style>
