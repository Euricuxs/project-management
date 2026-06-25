/**
 * Types for task data.
 */

// Task priority levels
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

// Task status values
export type TaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Done' | 'Cancelled';

export interface TaskResponse {
  id: string;
  columnId: string;
  title: string;
  description?: string;
  taskKey?: string;
  status: TaskStatus;
  priority: TaskPriority;
  position: number;
  dueDate?: string;
  completedAt?: string;
  assigneeId?: string;
  assigneeName?: string;
  reporterId: string;
  reporterName?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface TaskListItemResponse {
  id: string;
  columnId: string;
  title: string;
  taskKey?: string;
  status: TaskStatus;
  priority: TaskPriority;
  position: number;
  dueDate?: string;
  assigneeId?: string;
  assigneeName?: string;
  createdAt: string;
}

export interface CreateTaskRequest {
  columnId: string;
  title: string;
  description?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
  dueDate?: string;
  position?: number;
  assigneeId?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
  dueDate?: string | null;
  position?: number;
  columnId?: string;
  assigneeId?: string | null;
}

export interface MoveTaskRequest {
  targetColumnId: string;
  targetPosition: number;
}
