<script setup lang="ts">
/**
 * Users view placeholder.
 */
</script>

<template>
  <div class="users-view">
    <div class="page-header">
      <h1 class="page-title">Users</h1>
      <button class="btn-primary">Invite User</button>
    </div>

    <div class="users-content">
      <div class="empty-state">
        <div class="empty-icon">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75"/>
          </svg>
        </div>
        <h2>No users yet</h2>
        <p>Invite team members to collaborate.</p>
        <button class="btn-primary">Invite User</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.users-view {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xl);
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
}

.users-content {
  background-color: white;
  border-radius: var(--radius-lg);
  padding: var(--spacing-2xl);
  box-shadow: var(--shadow-sm);
}

.empty-state {
  text-align: center;
  padding: var(--spacing-2xl);
}

.empty-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 80px;
  height: 80px;
  background-color: var(--color-gray-100);
  border-radius: var(--radius-full);
  color: var(--color-gray-400);
  margin-bottom: var(--spacing-lg);
}

.empty-state h2 {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-sm);
}

.empty-state p {
  color: var(--color-gray-500);
  margin-bottom: var(--spacing-lg);
}
</style>
