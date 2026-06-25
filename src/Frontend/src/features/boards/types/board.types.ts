/**
 * Types for board data.
 */

export type BoardType = 'Kanban' | 'List' | 'Timeline';

// Label type for board tasks
export interface BoardLabel {
  id: string;
  projectId: string;
  name: string;
  color: string;
  taskCount: number;
}

// Task type used within board/column context
export type BoardTask = TaskResponse;

export interface TaskResponse {
  id: string;
  columnId: string;
  title: string;
  description?: string;
  taskKey?: string;
  status: string;
  priority: string;
  position: number;
  dueDate?: string;
  completedAt?: string;
  assigneeId?: string;
  assigneeName?: string;
  labels?: BoardLabel[];
  createdAt: string;
}

export interface ColumnResponse {
  id: string;
  boardId: string;
  name: string;
  position: number;
  color?: string;
  wipLimit: number;
  taskCount: number;
  tasks: TaskResponse[];
}

export interface BoardResponse {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  type: BoardType;
  position: number;
  isDefault: boolean;
  createdAt: string;
  updatedAt?: string;
  columns: ColumnResponse[];
  taskCount: number;
}

export interface BoardListItemResponse {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  type: BoardType;
  position: number;
  isDefault: boolean;
  createdAt: string;
  columnCount: number;
  taskCount: number;
}

export interface CreateBoardRequest {
  projectId: string;
  name: string;
  description?: string;
  type?: BoardType;
  position?: number;
  isDefault?: boolean;
  defaultColumns?: string[];
}

export interface UpdateBoardRequest {
  name?: string;
  description?: string;
  type?: BoardType;
  position?: number;
  isDefault?: boolean;
}
