import { spawn } from 'node:child_process';

const command = process.platform === 'win32' ? 'npx.cmd' : 'npx';
const port = process.env.PORT ?? '3000';

console.log('Starting the development server');

const child = spawn(command, ['vite', '--host', 'localhost', '--port', port, '--strictPort'], {
  stdio: 'inherit'
});

child.on('error', (error) => {
  console.error(error);
  process.exit(1);
});

child.on('exit', (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }

  process.exit(code ?? 0);
});
