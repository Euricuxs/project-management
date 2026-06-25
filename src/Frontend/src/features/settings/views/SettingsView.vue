<script setup lang="ts">
/**
 * Settings view placeholder.
 */
</script>

<template>
  <div class="settings-view">
    <div class="page-header">
      <h1 class="page-title">Settings</h1>
    </div>

    <div class="settings-content">
      <div class="settings-section">
        <h2 class="section-title">Account Settings</h2>
        <p class="section-description">Manage your account settings.</p>
      </div>

      <div class="settings-section">
        <h2 class="section-title">Notification Settings</h2>
        <p class="section-description">Configure how you receive notifications.</p>
      </div>

      <div class="settings-section">
        <h2 class="section-title">Security Settings</h2>
        <p class="section-description">Manage your password and security options.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.settings-view {
  max-width: 800px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: var(--spacing-xl);
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
}

.settings-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.settings-section {
  background-color: white;
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-sm);
}

.section-description {
  color: var(--color-gray-500);
  font-size: 0.875rem;
}
</style>
