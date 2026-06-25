/**
 * Types for label data.
 */

export interface LabelResponse {
  id: string;
  projectId: string;
  name: string;
  color: string;
  taskCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface LabelListItemResponse {
  id: string;
  projectId: string;
  name: string;
  color: string;
  taskCount: number;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

export interface UpdateLabelRequest {
  name?: string;
  color?: string;
}

export interface AddLabelsToTaskRequest {
  labelIds: string[];
}
