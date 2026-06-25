import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { AppError, ValidationError } from '@common/types';

/**
 * Error severity levels.
 */
export type ErrorSeverity = 'info' | 'warning' | 'error' | 'critical';

/**
 * Stored error with metadata.
 */
export interface StoredError {
  id: string;
  error: AppError;
  severity: ErrorSeverity;
  timestamp: Date;
  dismissed: boolean;
}

/**
 * Error store for centralized error management.
 */
export const useErrorStore = defineStore('error', () => {
  // State
  const errors = ref<StoredError[]>([]);
  const maxErrors = ref(50);

  // Getters
  const activeErrors = computed(() =>
    errors.value.filter((e) => !e.dismissed)
  );

  const validationErrors = computed(() =>
    activeErrors.value.filter((e) => e.error.code === 'VALIDATION_ERROR')
  );

  const hasErrors = computed(() => activeErrors.value.length > 0);

  const hasValidationErrors = computed(() => validationErrors.value.length > 0);

  const criticalErrors = computed(() =>
    activeErrors.value.filter((e) => e.severity === 'critical')
  );

  // Actions
  function addError(error: AppError, severity: ErrorSeverity = 'error'): string {
    const id = crypto.randomUUID();

    errors.value.push({
      id,
      error,
      severity,
      timestamp: new Date(),
      dismissed: false,
    });

    // Trim old errors if exceeding max
    if (errors.value.length > maxErrors.value) {
      errors.value = errors.value.slice(-maxErrors.value);
    }

    return id;
  }

  function dismissError(id: string): void {
    const error = errors.value.find((e) => e.id === id);
    if (error) {
      error.dismissed = true;
    }
  }

  function dismissAllErrors(): void {
    errors.value.forEach((e) => {
      e.dismissed = true;
    });
  }

  function clearErrors(): void {
    errors.value = [];
  }

  function getErrorsByCode(code: string): StoredError[] {
    return activeErrors.value.filter((e) => e.error.code === code);
  }

  function getFieldErrors(field: string): string[] {
    const validationError = validationErrors.value[0];
    if (!validationError) return [];

    const error = validationError.error as ValidationError;
    return error.details?.[field] || [];
  }

  return {
    // State
    errors,
    maxErrors,

    // Getters
    activeErrors,
    validationErrors,
    hasErrors,
    hasValidationErrors,
    criticalErrors,

    // Actions
    addError,
    dismissError,
    dismissAllErrors,
    clearErrors,
    getErrorsByCode,
    getFieldErrors,
  };
});
