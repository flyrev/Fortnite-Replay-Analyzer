import { act } from 'react';
import { createRoot } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import App from './App';

jest.mock('axios', () => ({
  get: jest.fn(),
  post: jest.fn()
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
