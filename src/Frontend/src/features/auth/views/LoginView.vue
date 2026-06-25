<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@features/auth/stores/auth.store';
import { createAuthService } from '@features/auth/services/auth.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';
import { ROUTE_NAMES } from '@common/constants';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const errorStore = useErrorStore();

// API client and service
const apiClient = createApiClient();
const authService = createAuthService(apiClient);

// Form state
const email = ref('');
const password = ref('');
const rememberMe = ref(false);
const isLoading = ref(false);
const errors = ref<Record<string, string[]>>({});

// Redirect URL after login
const redirectUrl = computed(() => route.query.redirect as string || '');

async function handleSubmit() {
  // Clear previous errors
  errors.value = {};
  errorStore.clearErrors();

  // Basic validation
  if (!email.value) {
    errors.value.email = ['Email is required'];
    return;
  }
  if (!password.value) {
    errors.value.password = ['Password is required'];
    return;
  }

  isLoading.value = true;

  try {
    const response = await authService.login({
      email: email.value,
      password: password.value,
      rememberMe: rememberMe.value,
    });

    // Store auth data
    authStore.login(response);

    // Redirect to original destination or dashboard
    if (redirectUrl.value) {
      router.push(redirectUrl.value);
    } else {
      router.push({ name: ROUTE_NAMES.DASHBOARD });
    }
  } catch (error: unknown) {
    // Error is already handled by API interceptor
    console.error('Login failed:', error);
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="login-view">
    <div class="login-header">
      <h2 class="view-title">Welcome Back</h2>
      <p class="view-subtitle">Sign in to your account to continue</p>
    </div>

    <form class="login-form" @submit.prevent="handleSubmit">
      <!-- Email Field -->
      <div class="form-group">
        <label for="email">Email Address</label>
        <input
          id="email"
          v-model="email"
          type="email"
          placeholder="you@example.com"
          class="form-input"
          :class="{ 'input-error': errors.email }"
          autocomplete="email"
          :disabled="isLoading"
        />
        <div v-if="errors.email" class="error-message">
          {{ errors.email[0] }}
        </div>
      </div>

      <!-- Password Field -->
      <div class="form-group">
        <label for="password">Password</label>
        <input
          id="password"
          v-model="password"
          type="password"
          placeholder="Enter your password"
          class="form-input"
          :class="{ 'input-error': errors.password }"
          autocomplete="current-password"
          :disabled="isLoading"
        />
        <div v-if="errors.password" class="error-message">
          {{ errors.password[0] }}
        </div>
      </div>

      <!-- Remember Me & Forgot Password -->
      <div class="form-options">
        <label class="checkbox-label">
          <input
            v-model="rememberMe"
            type="checkbox"
            :disabled="isLoading"
          />
          <span>Remember me</span>
        </label>
        <a href="#" class="forgot-link">Forgot password?</a>
      </div>

      <!-- Error Alert -->
      <div v-if="errorStore.errors.length > 0" class="alert alert-error">
        <div v-for="(err, index) in errorStore.errors" :key="index" class="error-item">
          {{ err.error.message }}
        </div>
      </div>

      <!-- Submit Button -->
      <button
        type="submit"
        class="btn-primary w-full"
        :disabled="isLoading"
      >
        <span v-if="isLoading" class="loading-spinner"></span>
        <span>{{ isLoading ? 'Signing in...' : 'Sign In' }}</span>
      </button>
    </form>

    <p class="register-link">
      Don't have an account?
      <router-link to="/register">Sign up</router-link>
    </p>
  </div>
</template>

<style scoped>
.login-view {
  width: 100%;
  max-width: 400px;
}

.login-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.view-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: var(--spacing-sm);
}

.view-subtitle {
  color: var(--color-gray-500);
  font-size: 0.95rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-700);
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-md);
  font-size: 1rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-light);
}

.form-input.input-error {
  border-color: var(--color-error);
}

.form-input:disabled {
  background-color: var(--color-gray-100);
  cursor: not-allowed;
}

.error-message {
  color: var(--color-error);
  font-size: 0.8125rem;
}

.form-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-gray-600);
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  width: 1rem;
  height: 1rem;
  cursor: pointer;
}

.forgot-link {
  font-size: 0.875rem;
  color: var(--color-primary);
  text-decoration: none;
}

.forgot-link:hover {
  text-decoration: underline;
}

.alert {
  padding: 0.75rem 1rem;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
}

.alert-error {
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  color: #dc2626;
}

.error-item {
  margin-bottom: 0.25rem;
}

.error-item:last-child {
  margin-bottom: 0;
}

.btn-primary {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: var(--spacing-sm) var(--spacing-lg);
  background-color: var(--color-primary);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--color-primary-hover);
}

.btn-primary:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.loading-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.register-link {
  margin-top: var(--spacing-lg);
  text-align: center;
  color: var(--color-gray-500);
  font-size: 0.875rem;
}

.register-link a {
  color: var(--color-primary);
  font-weight: 500;
  text-decoration: none;
}

.register-link a:hover {
  text-decoration: underline;
}
</style>
