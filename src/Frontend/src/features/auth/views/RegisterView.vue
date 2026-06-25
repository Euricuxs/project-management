<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@features/auth/stores/auth.store';
import { createAuthService } from '@features/auth/services/auth.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';
import { ROUTE_NAMES } from '@common/constants';

const router = useRouter();
const authStore = useAuthStore();
const errorStore = useErrorStore();

// API client and service
const apiClient = createApiClient();
const authService = createAuthService(apiClient);

// Form state
const firstName = ref('');
const lastName = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const isLoading = ref(false);
const errors = ref<Record<string, string[]>>({});

// Password strength indicator
const passwordStrength = ref<{ level: number; label: string; color: string }>({
  level: 0,
  label: '',
  color: '',
});

function checkPasswordStrength(pwd: string) {
  let score = 0;

  if (pwd.length >= 8) score++;
  if (pwd.length >= 12) score++;
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd)) score++;
  if (/\d/.test(pwd)) score++;
  if (/[^a-zA-Z0-9]/.test(pwd)) score++;

  if (score <= 1) {
    passwordStrength.value = { level: 1, label: 'Weak', color: '#ef4444' };
  } else if (score <= 2) {
    passwordStrength.value = { level: 2, label: 'Fair', color: '#f59e0b' };
  } else if (score <= 3) {
    passwordStrength.value = { level: 3, label: 'Good', color: '#3b82f6' };
  } else {
    passwordStrength.value = { level: 4, label: 'Strong', color: '#22c55e' };
  }
}

function validateForm(): boolean {
  errors.value = {};

  // First name validation
  if (!firstName.value.trim()) {
    errors.value.firstName = ['First name is required'];
  } else if (firstName.value.length > 100) {
    errors.value.firstName = ['First name must not exceed 100 characters'];
  }

  // Last name validation
  if (!lastName.value.trim()) {
    errors.value.lastName = ['Last name is required'];
  } else if (lastName.value.length > 100) {
    errors.value.lastName = ['Last name must not exceed 100 characters'];
  }

  // Email validation
  if (!email.value) {
    errors.value.email = ['Email is required'];
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    errors.value.email = ['Invalid email format'];
  }

  // Password validation
  if (!password.value) {
    errors.value.password = ['Password is required'];
  } else {
    const passwordErrors: string[] = [];
    if (password.value.length < 8) {
      passwordErrors.push('Password must be at least 8 characters');
    }
    if (!/[A-Z]/.test(password.value)) {
      passwordErrors.push('Password must contain at least one uppercase letter');
    }
    if (!/[a-z]/.test(password.value)) {
      passwordErrors.push('Password must contain at least one lowercase letter');
    }
    if (!/\d/.test(password.value)) {
      passwordErrors.push('Password must contain at least one number');
    }
    if (!/[^a-zA-Z0-9]/.test(password.value)) {
      passwordErrors.push('Password must contain at least one special character');
    }
    if (passwordErrors.length > 0) {
      errors.value.password = passwordErrors;
    }
  }

  // Confirm password validation
  if (!confirmPassword.value) {
    errors.value.confirmPassword = ['Please confirm your password'];
  } else if (password.value !== confirmPassword.value) {
    errors.value.confirmPassword = ['Passwords do not match'];
  }

  return Object.keys(errors.value).length === 0;
}

async function handleSubmit() {
  // Clear previous errors
  errors.value = {};
  errorStore.clearErrors();

  // Validate form
  if (!validateForm()) {
    return;
  }

  isLoading.value = true;

  try {
    const response = await authService.register({
      firstName: firstName.value.trim(),
      lastName: lastName.value.trim(),
      email: email.value.trim(),
      password: password.value,
      confirmPassword: confirmPassword.value,
    });

    // Store auth data
    authStore.login(response);

    // Redirect to dashboard
    router.push({ name: ROUTE_NAMES.DASHBOARD });
  } catch (error: unknown) {
    // Error is already handled by API interceptor
    console.error('Registration failed:', error);
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="register-view">
    <div class="register-header">
      <h2 class="view-title">Create Account</h2>
      <p class="view-subtitle">Get started with your free account</p>
    </div>

    <form class="register-form" @submit.prevent="handleSubmit">
      <!-- Name Fields -->
      <div class="form-row">
        <div class="form-group">
          <label for="firstName">First Name</label>
          <input
            id="firstName"
            v-model="firstName"
            type="text"
            placeholder="John"
            class="form-input"
            :class="{ 'input-error': errors.firstName }"
            autocomplete="given-name"
            :disabled="isLoading"
          />
          <div v-if="errors.firstName" class="error-message">
            {{ errors.firstName[0] }}
          </div>
        </div>
        <div class="form-group">
          <label for="lastName">Last Name</label>
          <input
            id="lastName"
            v-model="lastName"
            type="text"
            placeholder="Doe"
            class="form-input"
            :class="{ 'input-error': errors.lastName }"
            autocomplete="family-name"
            :disabled="isLoading"
          />
          <div v-if="errors.lastName" class="error-message">
            {{ errors.lastName[0] }}
          </div>
        </div>
      </div>

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
          placeholder="Create a strong password"
          class="form-input"
          :class="{ 'input-error': errors.password }"
          autocomplete="new-password"
          :disabled="isLoading"
          @input="checkPasswordStrength(password)"
        />
        <div v-if="password && passwordStrength.level > 0" class="password-strength">
          <div class="strength-bar">
            <div
              class="strength-fill"
              :style="{ width: `${passwordStrength.level * 25}%`, backgroundColor: passwordStrength.color }"
            ></div>
          </div>
          <span class="strength-label" :style="{ color: passwordStrength.color }">
            {{ passwordStrength.label }}
          </span>
        </div>
        <div v-if="errors.password" class="error-list">
          <div v-for="(err, index) in errors.password" :key="index" class="error-message">
            {{ err }}
          </div>
        </div>
      </div>

      <!-- Confirm Password Field -->
      <div class="form-group">
        <label for="confirmPassword">Confirm Password</label>
        <input
          id="confirmPassword"
          v-model="confirmPassword"
          type="password"
          placeholder="Confirm your password"
          class="form-input"
          :class="{ 'input-error': errors.confirmPassword }"
          autocomplete="new-password"
          :disabled="isLoading"
        />
        <div v-if="errors.confirmPassword" class="error-message">
          {{ errors.confirmPassword[0] }}
        </div>
      </div>

      <!-- Error Alert -->
      <div v-if="errorStore.errors.length > 0" class="alert alert-error">
        <div v-for="(err, index) in errorStore.errors" :key="index" class="error-item">
          {{ err.error.message }}
        </div>
      </div>

      <!-- Terms Agreement -->
      <p class="terms-text">
        By creating an account, you agree to our
        <a href="#">Terms of Service</a> and
        <a href="#">Privacy Policy</a>.
      </p>

      <!-- Submit Button -->
      <button
        type="submit"
        class="btn-primary w-full"
        :disabled="isLoading"
      >
        <span v-if="isLoading" class="loading-spinner"></span>
        <span>{{ isLoading ? 'Creating account...' : 'Create Account' }}</span>
      </button>
    </form>

    <p class="login-link">
      Already have an account?
      <router-link to="/login">Sign in</router-link>
    </p>
  </div>
</template>

<style scoped>
.register-view {
  width: 100%;
  max-width: 450px;
}

.register-header {
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

.register-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

@media (max-width: 480px) {
  .form-row {
    grid-template-columns: 1fr;
  }
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

.password-strength {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 0.25rem;
}

.strength-bar {
  flex: 1;
  height: 4px;
  background-color: var(--color-gray-200);
  border-radius: 2px;
  overflow: hidden;
}

.strength-fill {
  height: 100%;
  transition: width 0.3s, background-color 0.3s;
}

.strength-label {
  font-size: 0.75rem;
  font-weight: 500;
}

.error-message {
  color: var(--color-error);
  font-size: 0.8125rem;
}

.error-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
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

.terms-text {
  font-size: 0.8125rem;
  color: var(--color-gray-500);
  text-align: center;
  line-height: 1.5;
}

.terms-text a {
  color: var(--color-primary);
  text-decoration: none;
}

.terms-text a:hover {
  text-decoration: underline;
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

.login-link {
  margin-top: var(--spacing-lg);
  text-align: center;
  color: var(--color-gray-500);
  font-size: 0.875rem;
}

.login-link a {
  color: var(--color-primary);
  font-weight: 500;
  text-decoration: none;
}

.login-link a:hover {
  text-decoration: underline;
}
</style>