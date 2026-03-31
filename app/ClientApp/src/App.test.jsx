import { act } from 'react';
import { createRoot } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { it, vi } from 'vitest';
import App from './App';

vi.mock('axios', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn()
  }
}));

it('renders without crashing', async () => {
  const div = document.createElement('div');
  const root = createRoot(div);

  await act(async () => {
    root.render(
      <MemoryRouter>
        <App />
      </MemoryRouter>
    );
  });

  await act(async () => {
    root.unmount();
  });
});
