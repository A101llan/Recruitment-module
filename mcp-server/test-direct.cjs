const request = {
  jsonrpc: "2.0",
  id: "test-123",
  method: "tools/call",
  params: {
    name: "generate-questions",
    arguments: {
      jobTitle: "Software Developer",
      jobDescription: "Looking for a developer with JavaScript, React, and Node.js experience",
      experience: "mid",
      questionTypes: ["Text", "Choice", "Number", "Rating"],
      count: 10
    }
  }
};

console.log("Sending request:", JSON.stringify(request, null, 2));

const { spawn } = require('child_process');
const path = require('path');

const serverPath = path.join(__dirname, 'server.js');
const server = spawn('node', [serverPath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

server.stdin.write(JSON.stringify(request));
server.stdin.end();

let output = '';
server.stdout.on('data', (data) => {
  output += data.toString();
});

server.stderr.on('data', (data) => {
  console.error('Server error:', data.toString());
});

server.on('close', (code) => {
  console.log('=== SERVER RESPONSE ===');
  console.log(output);
  console.log('=== SERVER CLOSED ===');
  console.log('Exit code:', code);
});
