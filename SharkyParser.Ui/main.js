const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const fs = require('fs').promises;
const fsSync = require('fs');
const { spawn } = require('child_process');

// Path to embedded SharkyParser executable
let sharkyPath = null;

function parseEmbeddedOutput(output) {
  const lines = output.trim().split('\n').filter(line => line.includes('|'));
  const entries = [];
  let statistics = null;

  for (const line of lines) {
    if (line.startsWith('STATS|')) {
      const parts = line.split('|');
      statistics = {
        total: parseInt(parts[1]) || 0,
        errors: parseInt(parts[2]) || 0,
        warnings: parseInt(parts[3]) || 0,
        info: parseInt(parts[4]) || 0,
        debug: parseInt(parts[5]) || 0
      };
    } else if (line.startsWith('ENTRY|')) {
      // Split carefully - only split on unescaped pipes
      const parts = [];
      let current = '';
      let i = 0;
      while (i < line.length) {
        if (line[i] === '\\' && line[i + 1] === '|') {
          current += '|';
          i += 2;
        } else if (line[i] === '|') {
          parts.push(current);
          current = '';
          i++;
        } else {
          current += line[i];
          i++;
        }
      }
      parts.push(current);

      if (parts.length >= 9) {
        entries.push({
          timestamp: parts[1] || '',
          level: parts[2] || 'UNKNOWN',
          message: (parts[3] || '').replace(/\\n/g, '\n'),
          source: parts[4] || null,
          stackTrace: parts[5] ? parts[5].replace(/\\n/g, '\n') : null,
          lineNumber: parts[6] ? parseInt(parts[6]) : null,
          filePath: parts[7] || null,
          rawData: parts[8] || null
        });
      }
    }
  }

  return { entries, statistics: statistics || { total: entries.length, errors: 0, warnings: 0, info: 0, debug: 0 } };
}

function findSharky() {
  const exeName = process.platform === 'win32' ? 'SharkyParser.Cli.exe' : 'SharkyParser.Cli';

  const possiblePaths = [
    // В разработке (npm start)
    path.join(__dirname, 'dotnet', exeName),
    // В упакованном .app на Mac
    path.join(process.resourcesPath, 'dotnet', exeName),
    // Еще один вариант для Mac (внутри Resources)
    path.join(path.dirname(process.execPath), '..', 'Resources', 'dotnet', exeName),
    // Для Windows/Linux (рядом с исполняемым файлом)
    path.join(path.dirname(process.execPath), 'dotnet', exeName),
    // Резервный путь через getAppPath
    path.join(app.getAppPath(), 'dotnet', exeName)
  ];

  let errors = [];

  for (const exePath of possiblePaths) {
    if (fsSync.existsSync(exePath)) {
      try {
        // На всякий случай выдаем права на выполнение (только для Mac/Linux)
        if (process.platform !== 'win32') {
          fsSync.chmodSync(exePath, '755');
        }
        sharkyPath = exePath;
        console.log('✅ Found SharkyParser at:', sharkyPath);
        return true;
      } catch (e) {
        errors.push(`Found at ${exePath} but failed to set permissions: ${e.message}`);
      }
    } else {
      errors.push(`Not found: ${exePath}`);
    }
  }

  console.error('❌ SharkyParser not found in any of these locations:', errors);
  // Сохраняем логи ошибок для отладки в UI
  global.sharkyErrors = errors;
  return false;
}

function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1000,
    minHeight: 700,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    },
    titleBarStyle: 'hiddenInset',
    backgroundColor: '#0f0f23'
  });

  mainWindow.loadFile('index.html');

  // ВСЕГДА открываем инструменты разработчика, если есть ошибка инициализации
  if (!sharkyPath) {
    mainWindow.webContents.openDevTools();
  }
}

app.whenReady().then(() => {
  findSharky();
  createWindow();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// IPC handlers
ipcMain.handle('select-file', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile'],
    filters: [
      { name: 'Log Files', extensions: ['log', 'txt', 'out'] },
      { name: 'All Files', extensions: ['*'] }
    ]
  });

  return result.canceled ? null : result.filePaths[0];
});

ipcMain.handle('read-file', async (event, filePath) => {
  try {
    const content = await fs.readFile(filePath, 'utf-8');
    return content;
  } catch (error) {
    throw new Error(`Failed to read file: ${error.message}`);
  }
});

// Parse log file using embedded SharkyParser C# backend
ipcMain.handle('parse-log-csharp', async (event, filePath) => {
  return new Promise((resolve, reject) => {
    if (!sharkyPath) {
      if (!findSharky()) {
        reject(new Error('Embedded SharkyParser not found. Please rebuild the application.'));
        return;
      }
    }

    let stdout = '';
    let stderr = '';

    const proc = spawn(sharkyPath, ['parse', filePath, '--embedded']);

    proc.stdout.on('data', (data) => {
      stdout += data.toString();
    });

    proc.stderr.on('data', (data) => {
      stderr += data.toString();
    });

    proc.on('close', (code) => {
      if (code === 0) {
        try {
          const result = parseEmbeddedOutput(stdout);
          resolve(result);
        } catch (e) {
          reject(new Error(`Failed to parse output: ${e.message}`));
        }
      } else {
        reject(new Error(`SharkyParser exited with code ${code}: ${stderr}`));
      }
    });

    proc.on('error', (err) => {
      reject(new Error(`Failed to start SharkyParser: ${err.message}`));
    });
  });
});

// Check if embedded SharkyParser is available
ipcMain.handle('check-csharp-backend', async () => {
  return !!sharkyPath;
});