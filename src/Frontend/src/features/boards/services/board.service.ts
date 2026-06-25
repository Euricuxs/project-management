import type { AxiosInstance } from 'axios';
import type {
  BoardResponse,
  BoardListItemResponse,
  CreateBoardRequest,
  UpdateBoardRequest,
} from '../types/board.types';

/**
 * Creates the board service for API calls.
 */
export function createBoardService(client: AxiosInstance) {
  return {
    /**
     * Get all boards for a project.
     */
    async getProjectBoards(projectId: string): Promise<BoardListItemResponse[]> {
      const response = await client.get<{ data: BoardListItemResponse[] }>(
        `/boards/project/${projectId}`
      );
      return response.data.data;
    },

    /**
     * Get a single board by ID.
     */
    async getBoard(boardId: string): Promise<BoardResponse> {
      const response = await client.get<{ data: BoardResponse }>(`/boards/${boardId}`);
      return response.data.data;
    },

    /**
     * Create a new board.
     */
    async createBoard(data: CreateBoardRequest): Promise<BoardResponse> {
      const response = await client.post<{ data: BoardResponse }>('/boards', data);
      return response.data.data;
    },

    /**
     * Update an existing board.
     */
    async updateBoard(boardId: string, data: UpdateBoardRequest): Promise<BoardResponse> {
      const response = await client.put<{ data: BoardResponse }>(`/boards/${boardId}`, data);
      return response.data.data;
    },

    /**
     * Delete a board.
     */
    async deleteBoard(boardId: string): Promise<void> {
      await client.delete(`/boards/${boardId}`);
    },
  };
}

export type BoardService = ReturnType<typeof createBoardService>;
