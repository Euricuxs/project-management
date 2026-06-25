/**
 * Common constants used throughout the application.
 */
export const APP_NAME = 'Project Management';
export const APP_VERSION = '1.0.0';

/**
 * API configuration.
 */
export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
  TIMEOUT: 30000,
  RETRY_ATTEMPTS: 3,
  RETRY_DELAY: 1000,
} as const;

/**
 * Pagination defaults.
 */
export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 10,
  MAX_PAGE_SIZE: 100,
  PAGE_SIZES: [10, 20, 50, 100],
} as const;

/**
 * Date format patterns.
 */
export const DATE_FORMATS = {
  DISPLAY_DATE: 'MMM dd, yyyy',
  DISPLAY_DATETIME: 'MMM dd, yyyy HH:mm',
  API_DATE: 'yyyy-MM-dd',
  API_DATETIME: "yyyy-MM-dd'T'HH:mm:ss'Z'",
} as const;

/**
 * Storage keys for localStorage.
 */
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  REFRESH_TOKEN: 'refresh_token',
  USER_PREFERENCES: 'user_preferences',
  THEME: 'theme',
} as const;

/**
 * Route names for type-safe routing.
 */
export const ROUTE_NAMES = {
  HOME: 'home',
  LOGIN: 'login',
  REGISTER: 'register',
  DASHBOARD: 'dashboard',
  WORKSPACES: 'workspaces',
  PROJECTS: 'projects',
  PROJECT_DETAIL: 'project-detail',
  PROJECT_BOARDS: 'project-boards',
  KANBAN_BOARD: 'kanban-board',
  USERS: 'users',
  SETTINGS: 'settings',
} as const;

/**
 * HTTP status codes.
 */
export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  UNPROCESSABLE_ENTITY: 422,
  INTERNAL_SERVER_ERROR: 500,
} as const;
