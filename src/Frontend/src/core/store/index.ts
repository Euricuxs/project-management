import { createPinia } from 'pinia';

/**
 * Creates and configures the Pinia store.
 */
export function createStore(): ReturnType<typeof createPinia> {
  const pinia = createPinia();

  // Add Pinia plugins here if needed
  // Example: pinia.use(piniaPluginPersistedstate);

  return pinia;
}
