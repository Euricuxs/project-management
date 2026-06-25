/**
 * API Response wrapper matching backend ApiResponse<T> structure.
 */
export interface ApiResponse<T = unknown> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: ApiError[] | null;
  timestamp: string;
}

/**
 * Non-generic API response for operations without data.
 */
export interface ApiResponseBasic {
  success: boolean;
  data: null;
  message: string | null;
  errors: ApiError[] | null;
  timestamp: string;
}

/**
 * API error structure.
 */
export interface ApiError {
  code: string;
  field: string | null;
  message: string;
}

/**
 * Paginated response from API.
 */
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * Pagination query parameters.
 */
export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
  orderBy?: string;
  sortDescending?: boolean;
}

/**
 * Base entity interface matching backend BaseEntity.
 */
export interface BaseEntity {
  id: string;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
  isDeleted: boolean;
}
