<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@features/auth/stores/auth.store';
import { createAuthService } from '@features/auth/services/auth.service';
import { createApiClient } from '@core/api/client';

const router = useRouter();
const authStore = useAuthStore();
const authService = createAuthService(createApiClient());

const isOpen = ref(false);
const isLoggingOut = ref(false);

function toggleMenu() {
  isOpen.value = !isOpen.value;
}

function closeMenu() {
  isOpen.value = false;
}

async function handleLogout() {
  if (isLoggingOut.value) return;

  isLoggingOut.value = true;
  closeMenu();

  try {
    // Revoke refresh token if available
    if (authStore.refreshToken) {
      await authService.logout(authStore.refreshToken);
    }
  } catch {
    // Ignore logout API errors, proceed with local logout
  } finally {
    // Always clear local state
    authStore.logout();
    router.push('/login');
  }
}
</script>

<template>
  <div class="user-menu" v-click-outside="closeMenu">
    <button class="user-menu-trigger" @click="toggleMenu" :disabled="isLoggingOut">
      <div class="user-avatar">
        {{ authStore.user?.firstName?.charAt(0) || 'U' }}
      </div>
      <span class="user-name">{{ authStore.user?.fullName || 'User' }}</span>
      <svg
        class="chevron"
        :class="{ 'chevron-open': isOpen }"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
      >
        <path
          d="M4 6L8 10L12 6"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </button>

    <Transition name="dropdown">
      <div v-if="isOpen" class="user-menu-dropdown">
        <div class="user-info">
          <div class="user-email">{{ authStore.user?.email }}</div>
        </div>
        <div class="dropdown-divider"></div>
        <button class="dropdown-item dropdown-item-danger" @click="handleLogout" :disabled="isLoggingOut">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path
              d="M6 14H3a1 1 0 01-1-1V3a1 1 0 011-1h3M11 11l3-3-3-3M5 8h9"
              stroke="currentColor"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
          {{ isLoggingOut ? 'Signing out...' : 'Sign out' }}
        </button>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.user-menu {
  position: relative;
}

.user-menu-trigger {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-xs) var(--spacing-sm);
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.user-menu-trigger:hover {
  background-color: var(--color-gray-50);
  border-color: var(--color-gray-300);
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background-color: var(--color-primary);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.875rem;
  font-weight: 600;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-700);
}

.chevron {
  color: var(--color-gray-400);
  transition: transform var(--transition-fast);
}

.chevron-open {
  transform: rotate(180deg);
}

.user-menu-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  right: 0;
  min-width: 200px;
  background: white;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  z-index: 100;
  overflow: hidden;
}

.user-info {
  padding: var(--spacing-md);
}

.user-email {
  font-size: 0.875rem;
  color: var(--color-gray-600);
}

.dropdown-divider {
  height: 1px;
  background-color: var(--color-gray-100);
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  background: transparent;
  border: none;
  font-size: 0.875rem;
  color: var(--color-gray-700);
  cursor: pointer;
  transition: background-color var(--transition-fast);
  text-align: left;
}

.dropdown-item:hover {
  background-color: var(--color-gray-50);
}

.dropdown-item:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.dropdown-item-danger {
  color: var(--color-error);
}

.dropdown-item-danger:hover {
  background-color: #fef2f2;
}

/* Dropdown animation */
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.15s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
