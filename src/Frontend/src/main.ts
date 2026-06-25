import { createApp } from 'vue';
import App from './App.vue';
import { createAppRouter } from '@core/router';
import { createStore } from '@core/store';
import { createApiClient } from '@core/api/client';
import { clickOutside } from '@shared/directives/clickOutside';
import '@assets/styles/main.css';

// Create app instance
const app = createApp(App);

// Initialize Pinia FIRST (required for router guard to work)
const pinia = createStore();
app.use(pinia);

// Create router AFTER Pinia is set up
const router = createAppRouter();

const apiClient = createApiClient();

// Provide API client to all components
app.provide('api', apiClient);

// Register global directives
app.directive('click-outside', clickOutside);

// Use router
app.use(router);

// CRITICAL: Wait for router's initial navigation to complete
// before mounting the app. This ensures:
// 1. Router guard runs before first render
// 2. Initial route is resolved correctly
// 3. No flash of wrong layout/page
router.isReady().then(() => {
  app.mount('#app');
});

// Export for type inference
export { app, router, pinia, apiClient };
