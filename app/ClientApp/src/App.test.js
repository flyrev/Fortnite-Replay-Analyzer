import React from 'react';
import ReactDOM from 'react-dom';
import { MemoryRouter } from 'react-router-dom';
import App from './App';

jest.mock('axios', () => ({
  get: jest.fn(),
  post: jest.fn()
}));

it('renders without crashing', () => {
  const div = document.createElement('div');
  ReactDOM.render(
    <MemoryRouter>
      <App />
    </MemoryRouter>, div);
  ReactDOM.unmountComponentAtNode(div);
});
