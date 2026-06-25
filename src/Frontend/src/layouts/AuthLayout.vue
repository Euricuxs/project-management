<script setup lang="ts">
/**
 * Auth layout for login, register pages.
 */
import { RouterView } from 'vue-router';
</script>

<template>
  <div class="auth-layout">
    <div class="auth-container">
      <div class="auth-brand">
        <div class="brand-logo">
          <svg width="56" height="56" viewBox="0 0 48 48" fill="none">
            <rect width="48" height="48" rx="12" fill="var(--color-primary)"/>
            <path d="M14 18h20M14 24h20M14 30h12" stroke="white" stroke-width="3" stroke-linecap="round"/>
          </svg>
        </div>
        <h1 class="brand-title">Project Management</h1>
        <p class="brand-subtitle">Organize your work, achieve more</p>
      </div>
      <div class="auth-card">
        <RouterView />
      </div>
      <footer class="auth-footer">
        <p>&copy; 2026 Project Management. All rights reserved.</p>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.auth-layout {
  display: flex;
  min-height: 100vh;
  min-height: 100dvh;
  background: linear-gradient(135deg, var(--color-gray-50) 0%, #e5e7eb 100%);
}

.auth-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: var(--spacing-lg);
  max-width: 480px;
  margin: 0 auto;
}

.auth-brand {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.brand-logo {
  margin-bottom: var(--spacing-lg);
}

.brand-logo svg {
  filter: drop-shadow(0 4px 6px rgba(59, 130, 246, 0.3));
}

.brand-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-xs);
}

.brand-subtitle {
  font-size: 1rem;
  color: var(--color-gray-500);
  margin: 0;
}

.auth-card {
  width: 100%;
  background-color: white;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  padding: var(--spacing-xl);
}

.auth-footer {
  margin-top: var(--spacing-xl);
  text-align: center;
  color: var(--color-gray-400);
  font-size: 0.8125rem;
}

/* Mobile adjustments */
@media (max-width: 480px) {
  .auth-container {
    padding: var(--spacing-md);
  }

  .brand-logo svg {
    width: 48px;
    height: 48px;
  }

  .brand-title {
    font-size: 1.5rem;
  }

  .brand-subtitle {
    font-size: 0.9375rem;
  }

  .auth-card {
    padding: var(--spacing-lg);
    border-radius: var(--radius-lg);
    box-shadow: none;
  }
}
</style>