import type { AxiosInstance } from 'axios';
import type {
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  AuthResponse,
  UserResponse,
  LogoutResponse,
} from '../types/auth.types';

/**
 * Creates the authentication service.
 */
export function createAuthService(client: AxiosInstance) {
  return {
    /**
     * Login with email and password.
     */
    async login(request: LoginRequest): Promise<AuthResponse> {
      const response = await client.post<{ data: AuthResponse }>('/auth/login', request);
      return response.data.data;
    },

    /**
     * Register a new user.
     */
    async register(request: RegisterRequest): Promise<AuthResponse> {
      const response = await client.post<{ data: AuthResponse }>('/auth/register', request);
      return response.data.data;
    },

    /**
     * Refresh access token.
     */
    async refreshToken(request: RefreshTokenRequest): Promise<AuthResponse> {
      const response = await client.post<{ data: AuthResponse }>('/auth/refresh', request);
      return response.data.data;
    },

    /**
     * Logout and revoke refresh token.
     */
    async logout(refreshToken: string): Promise<LogoutResponse> {
      const response = await client.post<{ data: LogoutResponse }>('/auth/logout', {
        refreshToken,
      });
      return response.data.data;
    },

    /**
     * Get current user information.
     */
    async getCurrentUser(): Promise<UserResponse> {
      const response = await client.get<{ data: UserResponse }>('/auth/me');
      return response.data.data;
    },

    /**
     * Revoke all tokens (sign out from all devices).
     */
    async revokeAllTokens(): Promise<LogoutResponse> {
      const response = await client.post<{ data: LogoutResponse }>('/auth/revoke-all');
      return response.data.data;
    },
  };
}

export type AuthService = ReturnType<typeof createAuthService>;
