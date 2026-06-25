/**
 * Result type for operations that can fail.
 * Similar to Rust's Result type.
 */
export type Result<T, E = AppError> =
  | { success: true; data: T }
  | { success: false; error: E };

/**
 * Application error base interface.
 */
export interface AppError {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}

/**
 * Validation error from API.
 */
export interface ValidationError extends AppError {
  code: 'VALIDATION_ERROR';
  details: Record<string, string[]>;
}

/**
 * Not found error.
 */
export interface NotFoundError {
  code: 'NOT_FOUND';
  message: string;
  details?: string;
}

/**
 * Business rule violation error.
 */
export interface BusinessRuleError extends AppError {
  code: 'BUSINESS_RULE_VIOLATION';
}

/**
 * Network error when API is unreachable.
 */
export interface NetworkError {
  code: 'NETWORK_ERROR';
  message: string;
}

/**
 * Authentication error.
 */
export interface AuthError extends AppError {
  code: 'UNAUTHORIZED' | 'FORBIDDEN';
}

/**
 * Type guard to check if result is successful.
 */
export function isSuccess<T, E>(result: Result<T, E>): result is { success: true; data: T } {
  return result.success === true;
}

/**
 * Type guard to check if error is a validation error.
 */
export function isValidationError(error: AppError): error is ValidationError {
  return error.code === 'VALIDATION_ERROR';
}

/**
 * Type guard to check if error is a network error.
 */
export function isNetworkError(error: unknown): error is NetworkError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    (error as NetworkError).code === 'NETWORK_ERROR'
  );
}
