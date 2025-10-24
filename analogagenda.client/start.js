#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');

// Get the PORT from environment variable, default to 4200
const port = process.env.PORT || 4200;

console.log(`Starting Angular app on port ${port}`);

// Start the Angular development server
const ngServe = spawn('ng', ['serve', '--host=0.0.0.0', `--port=${port}`], {
  stdio: 'inherit',
  shell: true,
  cwd: __dirname
});

ngServe.on('error', (error) => {
  console.error('Failed to start Angular app:', error);
  process.exit(1);
});

ngServe.on('close', (code) => {
  console.log(`Angular app exited with code ${code}`);
  process.exit(code);
});

// Handle process termination
process.on('SIGINT', () => {
  console.log('Shutting down Angular app...');
  ngServe.kill('SIGINT');
});

process.on('SIGTERM', () => {
  console.log('Shutting down Angular app...');
  ngServe.kill('SIGTERM');
});
