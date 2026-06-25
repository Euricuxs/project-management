<script setup lang="ts">
import { ref, computed } from 'vue';
import { RouterView, useRouter } from 'vue-router';
import UserMenu from '@shared/components/UserMenu.vue';
import type { RouteRecordRaw } from 'vue-router';

const router = useRouter();
const isSidebarOpen = ref(false);

function toggleSidebar() {
  isSidebarOpen.value = !isSidebarOpen.value;
}

function closeSidebar() {
  isSidebarOpen.value = false;
}

// Filter routes to show only non-hidden navigation routes
const navRoutes = computed(() => {
  return router.getRoutes().filter((route: RouteRecordRaw) => {
    // Only show routes with navLabel meta and not hidden
    const meta = route.meta as { navLabel?: string; hidden?: boolean };
    return meta?.navLabel && !meta?.hidden;
  });
});
</script>

<template>
  <div class="default-layout">
    <!-- Mobile Header -->
    <header class="mobile-header">
      <button class="menu-toggle" @click="toggleSidebar" aria-label="Toggle menu">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
          <path d="M3 12h18M3 6h18M3 18h18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
      </button>
      <h1 class="mobile-title">Project Management</h1>
      <div class="mobile-user">
        <UserMenu />
      </div>
    </header>

    <!-- Sidebar Overlay (mobile) -->
    <div v-if="isSidebarOpen" class="sidebar-overlay" @click="closeSidebar"></div>

    <!-- Sidebar -->
    <aside class="sidebar" :class="{ 'sidebar-open': isSidebarOpen }">
      <div class="sidebar-header">
        <div class="sidebar-header-content">
          <h1 class="app-title">Project Management</h1>
          <button class="sidebar-close" @click="closeSidebar" aria-label="Close menu">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      </div>
      <nav class="sidebar-nav">
        <router-link
          v-for="route in navRoutes"
          :key="route.name"
          :to="route.path"
          class="nav-link"
          @click="closeSidebar"
        >
          {{ route.meta.navLabel }}
        </router-link>
      </nav>
    </aside>

    <!-- Main Content -->
    <div class="main-container">
      <header class="main-header">
        <div class="header-content">
          <div class="header-actions desktop-only">
            <UserMenu />
          </div>
        </div>
      </header>
      <main class="main-content">
        <RouterView v-slot="{ Component }">
          <Transition name="page" mode="out-in">
            <component :is="Component" />
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>
</template>

<style scoped>
.default-layout {
  display: flex;
  min-height: 100vh;
}

/* Mobile Header */
.mobile-header {
  display: none;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 56px;
  background-color: white;
  border-bottom: 1px solid var(--color-gray-200);
  padding: 0 var(--spacing-md);
  align-items: center;
  justify-content: space-between;
  z-index: 100;
}

.menu-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background: transparent;
  border: none;
  border-radius: var(--radius-md);
  color: var(--color-gray-700);
  cursor: pointer;
}

.menu-toggle:hover {
  background-color: var(--color-gray-100);
}

.mobile-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
}

.mobile-user {
  display: flex;
  align-items: center;
}

/* Sidebar Overlay */
.sidebar-overlay {
  display: none;
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  z-index: 199;
}

/* Sidebar */
.sidebar {
  width: 256px;
  background-color: var(--color-gray-900);
  color: white;
  padding: var(--spacing-lg);
  flex-shrink: 0;
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  z-index: 200;
  transition: transform 0.3s ease;
}

.sidebar-header {
  padding-bottom: var(--spacing-lg);
  border-bottom: 1px solid var(--color-gray-700);
  margin-bottom: var(--spacing-lg);
}

.sidebar-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-title {
  font-size: 1.25rem;
  font-weight: 600;
}

.sidebar-close {
  display: none;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background: transparent;
  border: none;
  border-radius: var(--radius-md);
  color: var(--color-gray-300);
  cursor: pointer;
}

.sidebar-close:hover {
  background-color: var(--color-gray-800);
  color: white;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.nav-link {
  display: block;
  padding: var(--spacing-sm) var(--spacing-md);
  color: var(--color-gray-300);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
}

.nav-link:hover {
  background-color: var(--color-gray-800);
  color: white;
}

.nav-link.router-link-active {
  background-color: var(--color-primary);
  color: white;
}

/* Main Container */
.main-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  margin-left: 256px;
}

.main-header {
  background-color: white;
  border-bottom: 1px solid var(--color-gray-200);
  padding: var(--spacing-md) var(--spacing-xl);
  position: sticky;
  top: 0;
  z-index: 10;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: var(--spacing-md);
}

.main-content {
  flex: 1;
  padding: var(--spacing-xl);
  background-color: var(--color-gray-50);
}

.desktop-only {
  display: flex;
}

/* Mobile Styles */
@media (max-width: 768px) {
  .mobile-header {
    display: flex;
  }

  .sidebar-overlay {
    display: block;
  }

  .sidebar {
    transform: translateX(-100%);
  }

  .sidebar.sidebar-open {
    transform: translateX(0);
  }

  .sidebar-close {
    display: flex;
  }

  .main-container {
    margin-left: 0;
    padding-top: 56px;
  }

  .main-header {
    display: none;
  }

  .main-content {
    padding: var(--spacing-md);
  }

  .desktop-only {
    display: none;
  }
}

@media (max-width: 480px) {
  .app-title {
    font-size: 1rem;
  }

  .mobile-title {
    font-size: 0.875rem;
  }
}

/* Page transitions */
.page-enter-active,
.page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
