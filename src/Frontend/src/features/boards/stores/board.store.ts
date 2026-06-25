import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  BoardResponse,
  BoardListItemResponse,
  CreateBoardRequest,
  UpdateBoardRequest,
} from '../types/board.types';
import { createBoardService } from '../services/board.service';
import { createApiClient } from '@core/api/client';
import { useErrorStore } from '@core/store/error.store';

/**
 * Board store for managing board state.
 */
export const useBoardStore = defineStore('board', () => {
  // State
  const boards = ref<BoardListItemResponse[]>([]);
  const currentBoard = ref<BoardResponse | null>(null);
  const selectedProjectId = ref<string | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Service instance
  const apiClient = createApiClient();
  const boardService = createBoardService(apiClient);

  // Getters
  const hasBoards = computed(() => boards.value.length > 0);
  const boardCount = computed(() => boards.value.length);

  // Actions
  async function fetchProjectBoards(projectId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    selectedProjectId.value = projectId;

    try {
      boards.value = await boardService.getProjectBoards(projectId);
    } catch (err) {
      error.value = 'Failed to fetch boards';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch boards' });
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchBoard(boardId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;

    try {
      currentBoard.value = await boardService.getBoard(boardId);
    } catch (err) {
      error.value = 'Failed to fetch board';
      const errorStore = useErrorStore();
      errorStore.addError({ code: 'FETCH_ERROR', message: 'Failed to fetch board' });
    } finally {
      isLoading.value = false;
    }
  }

  async function createBoard(data: CreateBoardRequest): Promise<BoardResponse | null> {
    isLoading.value = true;
    error.value = null;

    try {
      console.log('[BoardStore] Creating board:', data);
      const board = await boardService.createBoard(data);
      console.log('[BoardStore] Board created:', board);
      boards.value.push({
        id: board.id,
        projectId: board.projectId,
        name: board.name,
        description: board.description,
        type: board.type,
        position: board.position,
        isDefault: board.isDefault,
        createdAt: board.createdAt,
        columnCount: board.columns.length,
        taskCount: board.taskCount,
      });
      return board;
    } catch (err) {
      console.error('[BoardStore] Failed to create board:', err);
      error.value = 'Failed to create board';
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function updateBoard(boardId: string, data: UpdateBoardRequest): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      const board = await boardService.updateBoard(boardId, data);

      // Update in list
      const index = boards.value.findIndex((b) => b.id === boardId);
      if (index !== -1) {
        const existingBoard = boards.value[index]!;
        boards.value[index] = {
          id: existingBoard.id,
          projectId: existingBoard.projectId,
          name: board.name,
          description: board.description,
          type: board.type,
          position: board.position,
          isDefault: board.isDefault,
          createdAt: existingBoard.createdAt,
          columnCount: board.columns.length,
          taskCount: board.taskCount,
        };
      }

      // Update current board
      if (currentBoard.value?.id === boardId) {
        currentBoard.value = board;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to update board';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteBoard(boardId: string): Promise<boolean> {
    isLoading.value = true;
    error.value = null;

    try {
      await boardService.deleteBoard(boardId);
      boards.value = boards.value.filter((b) => b.id !== boardId);

      if (currentBoard.value?.id === boardId) {
        currentBoard.value = null;
      }

      return true;
    } catch (err) {
      error.value = 'Failed to delete board';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  function setCurrentBoard(board: BoardResponse | null): void {
    currentBoard.value = board;
  }

  function clearError(): void {
    error.value = null;
  }

  return {
    // State
    boards,
    currentBoard,
    selectedProjectId,
    isLoading,
    error,

    // Getters
    hasBoards,
    boardCount,

    // Actions
    fetchProjectBoards,
    fetchBoard,
    createBoard,
    updateBoard,
    deleteBoard,
    setCurrentBoard,
    clearError,
  };
});

// Type for store
export type BoardStore = ReturnType<typeof useBoardStore>;
