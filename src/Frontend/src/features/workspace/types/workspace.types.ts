/**
 * Workspace types for TypeScript type safety.
 */

export interface CreateWorkspaceRequest {
  name: string;
  description?: string;
  logoUrl?: string;
  isPublic: boolean;
}

export interface UpdateWorkspaceRequest {
  name: string;
  description?: string;
  logoUrl?: string;
  isPublic: boolean;
}

export interface WorkspaceResponse {
  id: string;
  name: string;
  description?: string;
  logoUrl?: string;
  ownerId: string;
  ownerName: string;
  isPublic: boolean;
  memberCount: number;
  projectCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface WorkspaceListItemResponse {
  id: string;
  name: string;
  logoUrl?: string;
  isPublic: boolean;
  role: string;
  memberCount: number;
  projectCount: number;
  createdAt: string;
}

export interface WorkspaceState {
  workspaces: WorkspaceListItemResponse[];
  currentWorkspace: WorkspaceResponse | null;
  isLoading: boolean;
  error: string | null;
}
