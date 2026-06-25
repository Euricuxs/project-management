import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { UserResponse, AuthResponse } from '../types/auth.types';
import { createAuthService } from '@features/auth/services/auth.service';
import { createApiClient } from '@core/api/client';

// Inactivity timeout: 30 minutes (in milliseconds)
const INACTIVITY_TIMEOUT = 30 * 60 * 1000;
// Check interval: 1 minute
const INACTIVITY_CHECK_INTERVAL = 60 * 1000;

/**
 * Auth store for managing authentication state.
 */
export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref<UserResponse | null>(null);
  const accessToken = ref<string | null>(localStorage.getItem('auth_access_token'));
  const refreshToken = ref<string | null>(localStorage.getItem('auth_refresh_token'));
  const isLoading = ref(false);
  const isInitialized = ref(false);
  const tokenExpiry = ref<Date | null>(
    localStorage.getItem('auth_token_expiry')
      ? new Date(localStorage.getItem('auth_token_expiry')!)
      : null
  );

  // Inactivity tracking
  const lastActivity = ref<number>(Date.now());
  let inactivityTimer: ReturnType<typeof setInterval> | null = null;

  // Getters
  const isAuthenticated = computed(() => !!accessToken.value && !!user.value);
  const hasToken = computed(() => !!accessToken.value);

  const userFullName = computed(() => {
    if (!user.value) return '';
    return `${user.value.firstName} ${user.value.lastName}`;
  });

  const isTokenExpiringSoon = computed(() => {
    if (!tokenExpiry.value) return false;
    // Consider token expiring if less than 5 minutes remaining
    const fiveMinutesFromNow = new Date(Date.now() + 5 * 60 * 1000);
    return tokenExpiry.value < fiveMinutesFromNow;
  });

  const inactivityMinutesRemaining = computed(() => {
    if (!accessToken.value) return 0;
    const elapsed = Date.now() - lastActivity.value;
    const remaining = Math.max(0, INACTIVITY_TIMEOUT - elapsed);
    return Math.ceil(remaining / 60000); // Convert to minutes
  });

  // Inactivity management functions
  function resetInactivityTimer(): void {
    lastActivity.value = Date.now();
  }

  function startInactivityTimer(): void {
    // Clear existing timer
    if (inactivityTimer) {
      clearInterval(inactivityTimer);
    }

    // Reset activity timestamp
    resetInactivityTimer();

    // Set up periodic check
    inactivityTimer = setInterval(() => {
      if (!accessToken.value) {
        // No active session, stop checking
        stopInactivityTimer();
        return;
      }

      const elapsed = Date.now() - lastActivity.value;
      if (elapsed >= INACTIVITY_TIMEOUT) {
        console.log('[Auth] Inactivity timeout reached, logging out...');
        performLogout('inactivity');
      }
    }, INACTIVITY_CHECK_INTERVAL);

    console.log('[Auth] Inactivity timer started');
  }

  function stopInactivityTimer(): void {
    if (inactivityTimer) {
      clearInterval(inactivityTimer);
      inactivityTimer = null;
      console.log('[Auth] Inactivity timer stopped');
    }
  }

  // Internal logout (doesn't reset timer to avoid recursion)
  function performLogout(reason: string = 'manual'): void {
    const wasAuthenticated = !!accessToken.value;

    user.value = null;
    accessToken.value = null;
    refreshToken.value = null;
    tokenExpiry.value = null;

    localStorage.removeItem('auth_access_token');
    localStorage.removeItem('auth_refresh_token');
    localStorage.removeItem('auth_token_expiry');

    stopInactivityTimer();

    if (wasAuthenticated) {
      console.log(`[Auth] Logged out due to: ${reason}`);
      // Redirect to login if user was authenticated
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
    }
  }

  // Actions
  function setUser(userData: UserResponse | null): void {
    user.value = userData;
    resetInactivityTimer();
  }

  function setTokens(authResponse: AuthResponse): void {
    accessToken.value = authResponse.accessToken;
    refreshToken.value = authResponse.refreshToken;
    tokenExpiry.value = new Date(authResponse.accessTokenExpiry);

    // Store in localStorage
    localStorage.setItem('auth_access_token', authResponse.accessToken);
    localStorage.setItem('auth_refresh_token', authResponse.refreshToken);
    localStorage.setItem('auth_token_expiry', authResponse.accessTokenExpiry);

    // Mark as initialized after successful token set
    isInitialized.value = true;

    // Start inactivity timer
    startInactivityTimer();
  }

  function setAccessToken(token: string, expiry: Date): void {
    accessToken.value = token;
    tokenExpiry.value = expiry;

    localStorage.setItem('auth_access_token', token);
    localStorage.setItem('auth_token_expiry', expiry.toISOString());

    // Reset inactivity timer
    resetInactivityTimer();
  }

  function clearTokens(): void {
    accessToken.value = null;
    refreshToken.value = null;
    tokenExpiry.value = null;

    localStorage.removeItem('auth_access_token');
    localStorage.removeItem('auth_refresh_token');
    localStorage.removeItem('auth_token_expiry');

    stopInactivityTimer();
  }

  function login(authResponse: AuthResponse): void {
    user.value = {
      id: authResponse.userId,
      email: authResponse.email,
      firstName: authResponse.firstName,
      lastName: authResponse.lastName,
      fullName: authResponse.fullName,
      avatarUrl: authResponse.avatarUrl,
      createdAt: new Date().toISOString(),
    };
    setTokens(authResponse);
  }

  function logout(): void {
    performLogout('manual');
  }

  function setLoading(loading: boolean): void {
    isLoading.value = loading;
  }

  function setInitialized(value: boolean): void {
    isInitialized.value = value;
  }

  /**
   * Initialize auth state from storage on app startup.
   */
  async function initializeFromStorage(): Promise<void> {
    const storedAccessToken = localStorage.getItem('auth_access_token');
    const storedRefreshToken = localStorage.getItem('auth_refresh_token');
    const storedExpiry = localStorage.getItem('auth_token_expiry');

    if (storedAccessToken && storedExpiry) {
      const expiryDate = new Date(storedExpiry);

      // Check if token is expired
      if (expiryDate > new Date()) {
        accessToken.value = storedAccessToken;
        refreshToken.value = storedRefreshToken;
        tokenExpiry.value = expiryDate;

        // Start inactivity timer
        startInactivityTimer();

        // Try to fetch user data
        try {
          const apiClient = createApiClient();
          const authService = createAuthService(apiClient);
          const userData = await authService.getCurrentUser();
          user.value = userData;
        } catch {
          // Token exists but /me failed - clear tokens
          performLogout('session_expired');
        }
      } else {
        // Token expired, clear it
        performLogout('token_expired');
      }
    }

    isInitialized.value = true;
  }

  return {
    // State
    user,
    accessToken,
    refreshToken,
    isLoading,
    isInitialized,
    tokenExpiry,
    lastActivity,
    inactivityMinutesRemaining,

    // Getters
    isAuthenticated,
    hasToken,
    userFullName,
    isTokenExpiringSoon,

    // Actions
    setUser,
    setTokens,
    setAccessToken,
    clearTokens,
    login,
    logout,
    setLoading,
    setInitialized,
    initializeFromStorage,
    resetInactivityTimer,
    startInactivityTimer,
    stopInactivityTimer,
  };
});

// Type for store
export type AuthStore = ReturnType<typeof useAuthStore>;

/**
 * Composable for tracking user activity.
 * Call this in components to reset the inactivity timer on user interaction.
 */
export function useActivityTracker() {
  const authStore = useAuthStore();

  const trackActivity = () => {
    if (authStore.accessToken) {
      authStore.resetInactivityTimer();
    }
  };

  // Set up activity listeners
  const setupActivityListeners = (): (() => void) => {
    if (typeof window === 'undefined') {
      return () => {};
    }

    const events = ['mousedown', 'keydown', 'touchstart', 'scroll'];
    events.forEach(event => {
      window.addEventListener(event, trackActivity, { passive: true });
    });

    // Return cleanup function
    return () => {
      events.forEach(event => {
        window.removeEventListener(event, trackActivity);
      });
    };
  };

  return {
    trackActivity,
    setupActivityListeners,
  };
}
