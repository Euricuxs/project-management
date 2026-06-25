<script setup lang="ts">
import { onMounted, computed, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useDashboardStore } from '@features/dashboard/stores/dashboard.store';

const route = useRoute();
const dashboardStore = useDashboardStore();

onMounted(async () => {
  await dashboardStore.fetchDashboard();
});

// Refresh dashboard when navigating back to it
watch(
  () => route.name,
  async (newName, oldName) => {
    if (newName === 'dashboard' && oldName !== 'dashboard') {
      await dashboardStore.fetchDashboard();
    }
  }
);

const stats = computed(() => ({
  activeProjects: dashboardStore.dashboard?.activeProjects ?? 0,
  completedProjects: dashboardStore.dashboard?.completedProjects ?? 0,
  tasksDue: dashboardStore.dashboard?.tasksDueThisWeek ?? 0,
  completedTasks: dashboardStore.dashboard?.completedTasksThisMonth ?? 0,
  teamMembers: dashboardStore.dashboard?.teamMembers ?? 0,
}));

const recentActivities = computed(() => dashboardStore.dashboard?.recentActivities ?? []);
const upcomingTasks = computed(() => dashboardStore.dashboard?.upcomingTasks ?? []);

function formatDate(dateString: string): string {
  // Parse as UTC and convert to local time for display
  const date = new Date(dateString + 'Z');
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString();
}

function formatDueDate(dateString: string): string {
  const date = new Date(dateString);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const dueDate = new Date(date);
  dueDate.setHours(0, 0, 0, 0);

  const diffMs = dueDate.getTime() - today.getTime();
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Tomorrow';
  if (diffDays < 0) return `${Math.abs(diffDays)}d overdue`;
  if (diffDays < 7) return `In ${diffDays} days`;

  return date.toLocaleDateString();
}

function getActivityIcon(type: string): string {
  switch (type) {
    case 'project_created':
      return 'folder-plus';
    case 'task_completed':
      return 'check-circle';
    case 'task_created':
      return 'plus';
    case 'comment_added':
      return 'message';
    default:
      return 'activity';
  }
}

function getTaskStatusColor(status: string): string {
  switch (status) {
    case 'Todo':
      return 'status-todo';
    case 'InProgress':
      return 'status-inprogress';
    case 'InReview':
      return 'status-review';
    case 'Done':
      return 'status-done';
    default:
      return 'status-todo';
  }
}
</script>

<template>
  <div class="dashboard-view">
    <div class="welcome-section">
      <h1 class="welcome-title">Welcome back!</h1>
      <p class="welcome-subtitle">Here's what's happening with your projects.</p>
    </div>

    <!-- Loading State -->
    <div v-if="dashboardStore.isLoading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Loading dashboard...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="dashboardStore.error" class="error-state">
      <div class="error-icon">
        <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
          <path d="M12 8v4m0 4h.01" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
        </svg>
      </div>
      <p>{{ dashboardStore.error }}</p>
      <button class="btn-secondary" @click="dashboardStore.fetchDashboard">Try Again</button>
    </div>

    <!-- Dashboard Content -->
    <template v-else>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon projects">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-value">{{ stats.activeProjects }}</span>
            <span class="stat-label">Active Projects</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon completed-projects">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-value">{{ stats.completedProjects }}</span>
            <span class="stat-label">Completed Projects</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon tasks">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-value">{{ stats.tasksDue }}</span>
            <span class="stat-label">Tasks Due This Week</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon completed">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-value">{{ stats.completedTasks }}</span>
            <span class="stat-label">Completed This Month</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon team">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2M9 11a4 4 0 100-8 4 4 0 000 8zM23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-value">{{ stats.teamMembers }}</span>
            <span class="stat-label">Team Members</span>
          </div>
        </div>
      </div>

      <div class="dashboard-sections">
        <section class="dashboard-section">
          <h2 class="section-title">Recent Activity</h2>
          <div v-if="recentActivities.length === 0" class="empty-state">
            <div class="empty-icon">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
                <path d="M12 8v4l2 2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
              </svg>
            </div>
            <p>No recent activity</p>
            <span class="empty-hint">Create a project to see activity here</span>
          </div>
          <div v-else class="activity-list">
            <div v-for="activity in recentActivities" :key="activity.id" class="activity-item">
              <div class="activity-icon" :class="activity.type">
                <svg v-if="getActivityIcon(activity.type) === 'folder-plus'" width="16" height="16" viewBox="0 0 24 24" fill="none">
                  <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                  <path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                <svg v-else-if="getActivityIcon(activity.type) === 'check-circle'" width="16" height="16" viewBox="0 0 24 24" fill="none">
                  <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                </svg>
                <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                </svg>
              </div>
              <div class="activity-content">
                <p class="activity-description">{{ activity.description }}</p>
                <span class="activity-time">{{ formatDate(activity.createdAt) }}</span>
              </div>
            </div>
          </div>
        </section>
        <section class="dashboard-section">
          <h2 class="section-title">Upcoming Tasks</h2>
          <div v-if="upcomingTasks.length === 0" class="empty-state">
            <div class="empty-icon">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
                <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <p>No upcoming tasks</p>
            <span class="empty-hint">Tasks with due dates will appear here</span>
          </div>
          <div v-else class="tasks-list">
            <div v-for="task in upcomingTasks" :key="task.id" class="task-item">
              <div class="task-info">
                <p class="task-title">{{ task.title }}</p>
                <span class="task-project" v-if="task.projectName">{{ task.projectName }}</span>
              </div>
              <div class="task-meta">
                <span :class="['task-status', getTaskStatusColor(task.status)]">{{ task.status }}</span>
                <span class="task-due">{{ formatDueDate(task.dueDate) }}</span>
              </div>
            </div>
          </div>
        </section>
      </div>
    </template>
  </div>
</template>

<style scoped>
.dashboard-view {
  max-width: 1200px;
  margin: 0 auto;
}

.welcome-section {
  margin-bottom: var(--spacing-xl);
}

.welcome-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-sm);
}

.welcome-subtitle {
  color: var(--color-gray-500);
  font-size: 1.125rem;
}

.loading-state,
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xxl);
  text-align: center;
  color: var(--color-gray-500);
  gap: var(--spacing-md);
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--color-gray-200);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-icon {
  color: var(--color-error);
  margin-bottom: var(--spacing-md);
  opacity: 0.7;
}

.btn-secondary {
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: white;
  color: var(--color-gray-700);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-secondary:hover {
  background-color: var(--color-gray-50);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

@media (max-width: 480px) {
  .stats-grid {
    grid-template-columns: 1fr 1fr;
    gap: var(--spacing-md);
  }
}

.stat-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  background-color: white;
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon.projects {
  background-color: var(--color-primary-light);
  color: var(--color-primary);
}

.stat-icon.completed-projects {
  background-color: #dbeafe;
  color: #2563eb;
}

.stat-icon.tasks {
  background-color: #fef3c7;
  color: var(--color-warning);
}

.stat-icon.completed {
  background-color: #d1fae5;
  color: var(--color-success);
}

.stat-icon.team {
  background-color: #ede9fe;
  color: #8b5cf6;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
}

.stat-label {
  font-size: 0.875rem;
  color: var(--color-gray-500);
}

.dashboard-sections {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: var(--spacing-xl);
}

@media (max-width: 768px) {
  .dashboard-sections {
    grid-template-columns: 1fr;
  }
}

.dashboard-section {
  background-color: white;
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-gray-200);
}

.empty-state {
  padding: var(--spacing-xl);
  text-align: center;
  color: var(--color-gray-400);
}

.empty-icon {
  margin-bottom: var(--spacing-md);
  opacity: 0.5;
}

.empty-hint {
  display: block;
  margin-top: var(--spacing-xs);
  font-size: 0.8125rem;
  color: var(--color-gray-400);
}

/* Activity List */
.activity-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.activity-item {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) 0;
}

.activity-item:not(:last-child) {
  border-bottom: 1px solid var(--color-gray-100);
}

.activity-icon {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background-color: var(--color-gray-100);
  color: var(--color-gray-600);
}

.activity-icon.project_created {
  background-color: #dbeafe;
  color: #2563eb;
}

.activity-icon.task_completed {
  background-color: #d1fae5;
  color: #059669;
}

.activity-content {
  flex: 1;
  min-width: 0;
}

.activity-description {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-gray-700);
  line-height: 1.4;
}

.activity-time {
  font-size: 0.75rem;
  color: var(--color-gray-400);
}

/* Tasks List */
.tasks-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.task-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  background-color: var(--color-gray-50);
  gap: var(--spacing-md);
}

.task-info {
  flex: 1;
  min-width: 0;
}

.task-title {
  margin: 0;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-900);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.task-project {
  font-size: 0.75rem;
  color: var(--color-gray-500);
}

.task-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  flex-shrink: 0;
}

.task-status {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: 0.6875rem;
  font-weight: 500;
  text-transform: uppercase;
}

.task-status.status-todo {
  background-color: var(--color-gray-200);
  color: var(--color-gray-700);
}

.task-status.status-inprogress {
  background-color: #dbeafe;
  color: #2563eb;
}

.task-status.status-review {
  background-color: #fef3c7;
  color: #d97706;
}

.task-status.status-done {
  background-color: #d1fae5;
  color: #059669;
}

.task-due {
  font-size: 0.75rem;
  color: var(--color-gray-500);
  white-space: nowrap;
}
</style>
