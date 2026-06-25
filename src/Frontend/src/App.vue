<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { RouterView } from 'vue-router';
import DefaultLayout from './layouts/DefaultLayout.vue';
import AuthLayout from './layouts/AuthLayout.vue';
import { useRoute } from 'vue-router';
import { computed } from 'vue';
import { useAuthStore } from '@features/auth/stores/auth.store';
import { getAuthInitPromise } from '@core/router';
import { useActivityTracker } from '@features/auth/stores/auth.store';

const route = useRoute();
const authStore = useAuthStore();
const isReady = ref(false);

// Activity tracker setup
const { setupActivityListeners } = useActivityTracker();
let cleanupActivityListeners: (() => void) | null = null;

const layout = computed(() => {
  const layoutName = (route.meta as { layout?: string })?.layout || 'default';
  switch (layoutName) {
    case 'auth':
      return AuthLayout;
    case 'empty':
      return 'div';
    default:
      return DefaultLayout;
  }
});

// Wait for auth initialization
// The auth init promise is shared with router guard via getAuthInitPromise()
if (authStore.isInitialized) {
  isReady.value = true;
} else {
  getAuthInitPromise().then(() => {
    isReady.value = true;
  }).catch(() => {
    // On error, still allow rendering (show login page)
    isReady.value = true;
  });
}

// Set up activity listeners when user is authenticated
onMounted(() => {
  cleanupActivityListeners = setupActivityListeners();
});

onUnmounted(() => {
  if (cleanupActivityListeners) {
    cleanupActivityListeners();
  }
});
</script>

<template>
  <div v-if="!isReady" class="app-loading">
    <div class="app-loading-content">
      <div class="app-logo">
        <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
          <rect width="48" height="48" rx="12" fill="var(--color-primary)"/>
          <path d="M14 18h20M14 24h20M14 30h12" stroke="white" stroke-width="3" stroke-linecap="round"/>
        </svg>
      </div>
      <p class="app-loading-text">Loading...</p>
      <div class="app-loading-spinner"></div>
    </div>
  </div>
  <component v-else :is="layout">
    <RouterView />
  </component>
</template>

<style scoped>
.app-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  min-height: 100dvh;
  background: linear-gradient(135deg, var(--color-gray-50) 0%, #e5e7eb 100%);
}

.app-loading-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-lg);
}

.app-logo {
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.8; transform: scale(0.95); }
}

.app-loading-text {
  font-size: 1rem;
  font-weight: 500;
  color: var(--color-gray-600);
  margin: 0;
}

.app-loading-spinner {
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
</style>
