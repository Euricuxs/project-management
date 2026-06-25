import { beforeAll } from 'vitest';
import { configure } from '@testing-library/vue';
import { cleanup } from '@testing-library/vue';
import { afterEach } from 'vitest';

// Configure Vue Testing Library
configure({
  testIdAttribute: 'data-testid',
});

// Global setup
beforeAll(() => {
  // Set up any global mocks here
});

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Global matchers
expect.extend({
  toBeWithinRange(received: number, floor: number, ceiling: number) {
    const pass = received >= floor && received <= ceiling;
    return {
      pass,
      message: () => `expected ${received} ${pass ? 'not ' : ''}to be within range ${floor} - ${ceiling}`,
    };
  },
});

// Type declarations for custom matchers
declare global {
  namespace Vi {
    interface Jest {
      toBeWithinRange: (received: number, floor: number, ceiling: number) => object;
    }
  }
}
