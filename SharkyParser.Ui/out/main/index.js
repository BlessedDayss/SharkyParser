import { app, ipcMain, BrowserWindow, dialog, shell } from "electron";
import * as path from "path";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import { spawn } from "child_process";
import * as fs from "fs";
import __cjs_mod__ from "node:module";
const __filename = import.meta.filename;
const __dirname = import.meta.dirname;
const require2 = __cjs_mod__.createRequire(import.meta.url);
const is = {
  dev: !app.isPackaged
};
({
  isWindows: process.platform === "win32",
  isMacOS: process.platform === "darwin",
  isLinux: process.platform === "linux"
});
const optimizer = {
  watchWindowShortcuts(window, shortcutOptions) {
    if (!window)
      return;
    const { webContents } = window;
    const { escToCloseWindow = false, zoom = false } = shortcutOptions || {};
    webContents.on("before-input-event", (event, input) => {
      if (input.type === "keyDown") {
        if (!is.dev) {
          if (input.code === "KeyR" && (input.control || input.meta))
            event.preventDefault();
          if (input.code === "KeyI" && (input.alt && input.meta || input.control && input.shift)) {
            event.preventDefault();
          }
        } else {
          if (input.code === "F12") {
            if (webContents.isDevToolsOpened()) {
              webContents.closeDevTools();
            } else {
              webContents.openDevTools({ mode: "undocked" });
              console.log("Open dev tool...");
            }
          }
        }
        if (escToCloseWindow) {
          if (input.code === "Escape" && input.key !== "Process") {
            window.close();
            event.preventDefault();
          }
        }
        if (!zoom) {
          if (input.code === "Minus" && (input.control || input.meta))
            event.preventDefault();
          if (input.code === "Equal" && input.shift && (input.control || input.meta))
            event.preventDefault();
        }
      }
    });
  },
  registerFramelessWindowIpc() {
    ipcMain.on("win:invoke", (event, action) => {
      const win = BrowserWindow.fromWebContents(event.sender);
      if (win) {
        if (action === "show") {
          win.show();
        } else if (action === "showInactive") {
          win.showInactive();
        } else if (action === "min") {
          win.minimize();
        } else if (action === "max") {
          const isMaximized = win.isMaximized();
          if (isMaximized) {
            win.unmaximize();
          } else {
            win.maximize();
          }
        } else if (action === "close") {
          win.close();
        }
      }
    });
  }
};
const __filename$1 = fileURLToPath(import.meta.url);
const __dirname$1 = dirname(__filename$1);
let splashWindow = null;
function createSplashScreen() {
  const splash = new BrowserWindow({
    width: 400,
    height: 300,
    transparent: true,
    frame: false,
    alwaysOnTop: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    }
  });
  splash.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(`
    <!DOCTYPE html>
    <html>
    <head>
      <style>
        body {
          margin: 0;
          padding: 0;
          background: transparent;
          display: flex;
          align-items: center;
          justify-content: center;
          height: 100vh;
          font-family: 'Inter', -apple-system, sans-serif;
          overflow: hidden;
        }
        .splash-container {
          background: linear-gradient(135deg, #0f1123 0%, #1a1d35 100%);
          border-radius: 24px;
          padding: 40px;
          box-shadow: 0 30px 60px rgba(0,0,0,0.5), 0 0 1px rgba(255,255,255,0.1);
          text-align: center;
          border: 1px solid rgba(255,255,255,0.1);
        }
        .logo {
          font-size: 64px;
          margin-bottom: 20px;
          animation: float 3s ease-in-out infinite;
        }
        @keyframes float {
          0%, 100% { transform: translateY(0px); }
          50% { transform: translateY(-10px); }
        }
        .title {
          font-size: 24px;
          font-weight: 700;
          background: linear-gradient(to right, #fff, #94a3b8);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          margin-bottom: 10px;
        }
        .subtitle {
          font-size: 12px;
          color: #94a3b8;
          margin-bottom: 30px;
        }
        .loader {
          width: 200px;
          height: 4px;
          background: rgba(255,255,255,0.1);
          border-radius: 2px;
          overflow: hidden;
          margin: 0 auto;
        }
        .loader-bar {
          height: 100%;
          background: linear-gradient(90deg, #7000ff, #00f2ff);
          border-radius: 2px;
          animation: loading 1.5s ease-in-out infinite;
        }
        @keyframes loading {
          0% { width: 0%; margin-left: 0%; }
          50% { width: 50%; margin-left: 25%; }
          100% { width: 0%; margin-left: 100%; }
        }
        .status {
          margin-top: 20px;
          font-size: 11px;
          color: #00f2ff;
          height: 16px;
        }
      </style>
    </head>
    <body>
      <div class="splash-container">
        <div class="logo">🦈</div>
        <div class="title">Sharky Parser PRO</div>
        <div class="subtitle">Advanced Log Analysis System</div>
        <div class="loader"><div class="loader-bar"></div></div>
        <div class="status" id="status">Initializing backend...</div>
      </div>
      <script>
        let dots = 0;
        setInterval(() => {
          dots = (dots + 1) % 4;
          const status = document.getElementById('status');
          if (status) status.textContent = 'Initializing backend' + '.'.repeat(dots);
        }, 500);
      <\/script>
    </body>
    </html>
  `)}`);
  return splash;
}
function createMainWindow() {
  const main = new BrowserWindow({
    width: 1200,
    height: 850,
    show: false,
    autoHideMenuBar: true,
    titleBarStyle: "hiddenInset",
    backgroundColor: "#05060f",
    webPreferences: {
      preload: join(__dirname$1, "../preload/index.mjs"),
      sandbox: false,
      contextIsolation: true,
      nodeIntegration: false
    }
  });
  main.on("ready-to-show", () => {
    if (splashWindow) {
      splashWindow.close();
      splashWindow = null;
    }
    main.show();
  });
  main.webContents.setWindowOpenHandler((details) => {
    shell.openExternal(details.url);
    return { action: "deny" };
  });
  if (is.dev && process.env["ELECTRON_RENDERER_URL"]) {
    main.loadURL(process.env["ELECTRON_RENDERER_URL"]);
  } else {
    main.loadFile(join(__dirname$1, "../renderer/index.html"));
  }
  return main;
}
async function checkBackend() {
  const sharkyPath = findSharky();
  console.log("Checking backend at:", sharkyPath);
  return fs.existsSync(sharkyPath);
}
app.whenReady().then(async () => {
  if (process.platform === "win32") {
    app.setAppUserModelId("com.sharkyparser.app");
  }
  app.on("browser-window-created", (_, window) => {
    optimizer.watchWindowShortcuts(window);
  });
  splashWindow = createSplashScreen();
  const backendReady = await checkBackend();
  if (!backendReady) {
    dialog.showErrorBox(
      "Backend Not Found",
      "SharkyParser backend is not available. Please rebuild the project."
    );
    app.quit();
    return;
  }
  await new Promise((resolve) => setTimeout(resolve, 3e3));
  ipcMain.handle("select-file", async () => {
    const result = await dialog.showOpenDialog({
      properties: ["openFile"],
      filters: [{ name: "Log Files", extensions: ["log", "txt", "csv"] }]
    });
    return result.canceled ? null : result.filePaths[0];
  });
  ipcMain.handle("check-csharp-backend", () => {
    return backendReady;
  });
  ipcMain.handle("parse-log-csharp", async (_, filePath) => {
    const sharkyPath = findSharky();
    return new Promise((resolve, reject) => {
      let output = "";
      const proc = spawn(sharkyPath, ["parse", filePath, "--embedded"]);
      proc.stdout.on("data", (data) => output += data.toString());
      proc.on("close", (code) => {
        if (code === 0) resolve(parseEmbeddedOutput(output));
        else reject(new Error(`Exit code ${code}`));
      });
    });
  });
  ipcMain.handle("analyze-log-csharp", async (_, filePath) => {
    const sharkyPath = findSharky();
    return new Promise((resolve, reject) => {
      let output = "";
      const proc = spawn(sharkyPath, ["analyze", filePath, "--embedded"]);
      proc.stdout.on("data", (data) => output += data.toString());
      proc.on("close", (code) => {
        if (code === 0) {
          const line = output.trim().split("\n").find((l) => l.startsWith("ANALYSIS|"));
          if (line) {
            const p = line.split("|");
            resolve({
              total: parseInt(p[1]),
              errors: parseInt(p[2]),
              warnings: parseInt(p[3]),
              info: parseInt(p[4]),
              debug: parseInt(p[5]),
              status: p[6],
              extendedData: p[7] || ""
            });
          } else {
            reject(new Error("No analysis data found"));
          }
        } else reject(new Error(`Exit code ${code}`));
      });
    });
  });
  createMainWindow();
  app.on("activate", function() {
    if (BrowserWindow.getAllWindows().length === 0) {
      createMainWindow();
    }
  });
});
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});
function findSharky() {
  const isDev = !app.isPackaged;
  const appPath = app.getAppPath();
  if (isDev) {
    const paths = [
      path.join(appPath, "dotnet", "SharkyParser.Cli"),
      path.join(appPath, "..", "SharkyParser.Cli", "bin", "Debug", "net8.0", "SharkyParser.Cli")
    ];
    for (const p of paths) if (fs.existsSync(p)) return p;
  }
  return path.join(process.resourcesPath, "dotnet", "SharkyParser.Cli");
}
function parseEmbeddedOutput(output) {
  const lines = output.trim().split("\n").filter((line) => line.includes("|"));
  const entries = [];
  let statistics = { total: 0, errors: 0, warnings: 0, info: 0, debug: 0 };
  for (const line of lines) {
    if (line.startsWith("STATS|")) {
      const parts = line.split("|");
      statistics = {
        total: parseInt(parts[1]) || 0,
        errors: parseInt(parts[2]) || 0,
        warnings: parseInt(parts[3]) || 0,
        info: parseInt(parts[4]) || 0,
        debug: parseInt(parts[5]) || 0
      };
    } else if (line.startsWith("ENTRY|")) {
      const parts = [];
      let current = "";
      let escaped = false;
      for (let i = 6; i < line.length; i++) {
        if (line[i] === "\\" && !escaped) {
          escaped = true;
          continue;
        }
        if (line[i] === "|" && !escaped) {
          parts.push(current);
          current = "";
          continue;
        }
        current += line[i];
        escaped = false;
      }
      parts.push(current);
      entries.push({
        timestamp: parts[0] || "",
        level: parts[1] || "INFO",
        message: parts[2] || "",
        source: parts[3] || "",
        stackTrace: parts[4] || "",
        lineNumber: parts[5] || "",
        filePath: parts[6] || "",
        rawData: parts[7] || ""
      });
    }
  }
  return { entries, statistics };
}
