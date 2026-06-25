import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios';
import { API_CONFIG } from '@common/constants';
import { type AppError, type ValidationError } from '@common/types';
import { useErrorStore } from '@core/store/error.store';
import { useAuthStore } from '@features/auth/stores/auth.store';

// Flag to prevent multiple simultaneous refresh attempts
let isRefreshing = false;
let refreshSubscribers: Array<(token: string) => void> = [];

/**
 * Add a subscriber waiting for token refresh.
 */
function subscribeTokenRefresh(callback: (token: string) => void): void {
  refreshSubscribers.push(callback);
}

/**
 * Notify all subscribers that token has been refreshed.
 */
function onTokenRefreshed(newToken: string): void {
  refreshSubscribers.forEach(callback => callback(newToken));
  refreshSubscribers = [];
}

/**
 * Retry all queued requests.
 */
function retryRequests(error: AxiosError): Promise<unknown> {
  const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

  if (error.response?.status === 401) {
    // Check if token was expired
    const tokenExpired = error.response.headers['token-expired'] === 'true';

    if (tokenExpired && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!isRefreshing) {
        isRefreshing = true;

        const authStore = useAuthStore();

        if (authStore.refreshToken) {
          // Attempt to refresh token
          axios.post(`${API_CONFIG.BASE_URL}/api/auth/refresh`, {
            refreshToken: authStore.refreshToken,
          })
            .then((response: AxiosResponse) => {
              const newAuthResponse = response.data.data;
              authStore.setTokens(newAuthResponse);
              onTokenRefreshed(newAuthResponse.accessToken);
            })
            .catch(() => {
              // Refresh failed, logout
              authStore.logout();
              window.location.href = '/login';
            })
            .finally(() => {
              isRefreshing = false;
            });
        } else {
          // No refresh token, redirect to login
          authStore.logout();
          window.location.href = '/login';
        }
      }

      // Queue this request to be retried after token refresh
      return new Promise(resolve => {
        subscribeTokenRefresh(newToken => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
          }
          resolve(axios(originalRequest));
        });
      });
    }
  }

  return Promise.reject(error);
}

/**
 * Creates and configures the Axios API client.
 */
export function createApiClient(baseURL: string = API_CONFIG.BASE_URL): AxiosInstance {
  const client = axios.create({
    baseURL,
    timeout: API_CONFIG.TIMEOUT,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Request interceptor
  client.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      // Add auth token if available
      const authStore = useAuthStore();
      if (authStore.accessToken && config.headers) {
        config.headers.Authorization = `Bearer ${authStore.accessToken}`;
      }

      // Add correlation ID
      const correlationId = crypto.randomUUID();
      if (config.headers) {
        config.headers['X-Correlation-ID'] = correlationId;
      }

      return config;
    },
    (error) => {
      return Promise.reject(error);
    }
  );

  // Response interceptor
  client.interceptors.response.use(
    (response: AxiosResponse) => response,
    async (error: AxiosError) => {
      const errorStore = useErrorStore();
      const authStore = useAuthStore();

      // Handle network errors
      if (!error.response) {
        const networkError = {
          code: 'NETWORK_ERROR',
          message: 'Unable to connect to the server. Please check your internet connection.',
        };
        errorStore.addError(networkError);
        return Promise.reject(networkError);
      }

      // Handle 401 with token refresh logic
      if (error.response.status === 401) {
        // Skip refresh for auth endpoints
        const originalRequest = error.config as InternalAxiosRequestConfig;
        if (originalRequest.url?.includes('/api/auth/')) {
          const apiError = parseApiError(error);
          errorStore.addError(apiError);
          return Promise.reject(apiError);
        }

        // Try to refresh token and retry request
        return retryRequests(error)
          .then((result) => result as AxiosResponse)
          .catch((refreshError: AxiosError | AppError) => {
            // Refresh failed, redirect to login if not authenticated
            if (!authStore.isAuthenticated) {
              authStore.logout();
              window.location.href = '/login';
            }
            return Promise.reject(refreshError);
          });
      }

      // Handle other API errors
      const apiError = parseApiError(error);
      errorStore.addError(apiError);

      return Promise.reject(apiError);
    }
  );

  return client;
}

/**
 * Parse Axios error into application error.
 */
function parseApiError(error: AxiosError): AppError {
  const response = error.response;
  const data = response?.data as { message?: string; errors?: Array<{ code: string; field?: string; message: string }> } | undefined;

  switch (response?.status) {
    case 400:
      if (data?.errors && data.errors.length > 0) {
        const details: Record<string, string[]> = {};
        for (const err of data.errors) {
          const field = err.field || 'general';
          if (!details[field]) {
            details[field] = [];
          }
          details[field]!.push(err.message);
        }
        return {
          code: 'VALIDATION_ERROR',
          message: data.message || 'Validation failed',
          details,
        } as ValidationError;
      }
      return {
        code: 'BAD_REQUEST',
        message: data?.message || 'Invalid request',
      };

    case 401:
      return {
        code: 'UNAUTHORIZED',
        message: data?.message || 'Authentication required',
      };

    case 403:
      return {
        code: 'FORBIDDEN',
        message: data?.message || 'You do not have permission to perform this action',
      };

    case 404:
      return {
        code: 'NOT_FOUND',
        message: data?.message || 'The requested resource was not found',
      };

    case 409:
      return {
        code: 'CONFLICT',
        message: data?.message || 'A conflict occurred',
      };

    case 422:
      return {
        code: 'BUSINESS_RULE_VIOLATION',
        message: data?.message || 'Business rule violation',
      };

    case 500:
    default:
      return {
        code: 'INTERNAL_ERROR',
        message: data?.message || 'An unexpected error occurred. Please try again later.',
      };
  }
}

/**
 * Transform API response to Result type.
 */
export function toResult<T>(response: { data: { success: boolean; data: T | null; errors: unknown[] } }): {
  success: boolean;
  data: T | null;
  error: AppError | null;
} {
  if (response.data.success) {
    return { success: true, data: response.data.data, error: null };
  }

  const errors = response.data.errors as Array<{ code: string; field?: string; message: string }>;
  const firstError = errors?.[0];

  return {
    success: false,
    data: null,
    error: {
      code: firstError?.code || 'UNKNOWN_ERROR',
      message: firstError?.message || 'An error occurred',
    },
  };
}

export { type AxiosInstance, type AxiosError };