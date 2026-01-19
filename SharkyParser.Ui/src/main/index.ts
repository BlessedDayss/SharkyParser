import { app, shell, BrowserWindow, ipcMain, dialog, nativeImage } from 'electron'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'
import { optimizer, is } from '@electron-toolkit/utils'
import { spawn } from 'child_process'
import * as fs from 'fs'
import * as path from 'path'
import * as https from 'https'
import pkg from 'electron-updater'
const { autoUpdater } = pkg

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

let splashWindow: BrowserWindow | null = null
let mainWindow: BrowserWindow | null = null

function createSplashScreen(): BrowserWindow {
    const splash = new BrowserWindow({
        width: 400,
        height: 300,
        transparent: true,
        frame: false,
        alwaysOnTop: true,
        icon: join(__dirname, '../../resources/icon.ico'),
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true
        }
    })

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
      </script>
    </body>
    </html>
    `)}`)

    return splash
}

function createMainWindow(): BrowserWindow {
    const iconPath = join(__dirname, '../../resources/icon.ico')
    const icon = nativeImage.createFromPath(iconPath)

    const main = new BrowserWindow({
        width: 1200,
        height: 850,
        show: false,
        autoHideMenuBar: true,
        titleBarStyle: 'hiddenInset',
        backgroundColor: '#05060f',
        icon: icon,
        webPreferences: {
            preload: join(__dirname, '../preload/index.mjs'),
            sandbox: false,
            contextIsolation: true,
            nodeIntegration: false
        }
    })

    main.on('ready-to-show', () => {
        if (splashWindow) {
            splashWindow.close()
            splashWindow = null
        }
        main.show()
    })

    main.webContents.setWindowOpenHandler((details) => {
        shell.openExternal(details.url)
        return { action: 'deny' }
    })

    if (is.dev && process.env['ELECTRON_RENDERER_URL']) {
        main.loadURL(process.env['ELECTRON_RENDERER_URL'])
    } else {
        main.loadFile(join(__dirname, '../renderer/index.html'))
    }

    return main
}

async function checkBackend(): Promise<boolean> {
    const sharkyPath = findSharky()
    console.log('Checking backend at:', sharkyPath)
    return fs.existsSync(sharkyPath)
}

app.whenReady().then(async () => {
    if (process.platform === 'win32') {
        app.setAppUserModelId('com.sharkyparser.app')
    }

    app.on('browser-window-created', (_, window) => {
        optimizer.watchWindowShortcuts(window)
    })

    // Show splash screen
    splashWindow = createSplashScreen()

    // Check backend
    const backendReady = await checkBackend()

    if (!backendReady) {
        dialog.showErrorBox(
            'Backend Not Found',
            'SharkyParser backend is not available. Please rebuild the project.'
        )
        app.quit()
        return
    }

    // Show splash for at least 3 seconds
    await new Promise(resolve => setTimeout(resolve, 3000))

    // IPC Handlers
    ipcMain.handle('select-file', async () => {
        const result = await dialog.showOpenDialog({
            properties: ['openFile'],
            filters: [{ name: 'Log Files', extensions: ['log', 'txt', 'csv'] }]
        })
        return result.canceled ? null : result.filePaths[0]
    })

    ipcMain.handle('check-csharp-backend', () => {
        return backendReady
    })

    ipcMain.handle('get-app-version', () => {
        return app.getVersion()
    })


    ipcMain.handle('parse-log-csharp', async (_, filePath: string) => {
        const sharkyPath = findSharky()
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(sharkyPath, ['parse', filePath, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.on('close', (code) => {
                if (code === 0) resolve(parseEmbeddedOutput(output))
                else reject(new Error(`Exit code ${code}`))
            })
        })
    })

    ipcMain.handle('analyze-log-csharp', async (_, filePath: string) => {
        const sharkyPath = findSharky()
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(sharkyPath, ['analyze', filePath, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.on('close', (code) => {
                if (code === 0) {
                    const line = output.trim().split('\n').find(l => l.startsWith('ANALYSIS|'))
                    if (line) {
                        const p = line.split('|')
                        resolve({
                            total: parseInt(p[1]),
                            errors: parseInt(p[2]),
                            warnings: parseInt(p[3]),
                            info: parseInt(p[4]),
                            debug: parseInt(p[5]),
                            status: p[6],
                            extendedData: p[7] || ''
                        })
                    } else {
                        reject(new Error("No analysis data found"))
                    }
                } else reject(new Error(`Exit code ${code}`))
            })
        })
    })

    // Setup auto-update system
    setupAutoUpdater()

    // Create main window (it will auto-show and close splash)
    mainWindow = createMainWindow()

    app.on('activate', function () {
        if (BrowserWindow.getAllWindows().length === 0) {
            mainWindow = createMainWindow()
        }
    })
})

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit()
})

function findSharky(): string {
    const isDev = !app.isPackaged
    const appPath = app.getAppPath()
    const exeName = process.platform === 'win32' ? 'SharkyParser.Cli.exe' : 'SharkyParser.Cli'

    if (isDev) {
        const paths = [
            path.join(appPath, 'dotnet', exeName),
            path.join(appPath, '..', 'SharkyParser.Cli', 'bin', 'Debug', 'net8.0', exeName)
        ]
        for (const p of paths) if (fs.existsSync(p)) return p
    }
    return path.join(process.resourcesPath, 'dotnet', exeName)
}

function parseEmbeddedOutput(output: string) {
    const lines = output.trim().split('\n').filter(line => line.includes('|'))
    const entries: any[] = []
    let statistics = { total: 0, errors: 0, warnings: 0, info: 0, debug: 0 }

    for (const line of lines) {
        if (line.startsWith('STATS|')) {
            const parts = line.split('|')
            statistics = {
                total: parseInt(parts[1]) || 0,
                errors: parseInt(parts[2]) || 0,
                warnings: parseInt(parts[3]) || 0,
                info: parseInt(parts[4]) || 0,
                debug: parseInt(parts[5]) || 0
            }
        } else if (line.startsWith('ENTRY|')) {
            const parts: string[] = []
            let current = ''
            let escaped = false
            for (let i = 6; i < line.length; i++) {
                if (line[i] === '\\' && !escaped) { escaped = true; continue; }
                if (line[i] === '|' && !escaped) { parts.push(current); current = ''; continue; }
                current += line[i]; escaped = false;
            }
            parts.push(current)
            entries.push({
                timestamp: parts[0] || '',
                level: parts[1] || 'INFO',
                message: parts[2] || '',
                source: parts[3] || '',
                stackTrace: parts[4] || '',
                lineNumber: parts[5] || '',
                filePath: parts[6] || '',
                rawData: parts[7] || ''
            })
        }
    }
    return { entries, statistics }
}


// Auto-Update System (like Cursor/VS Code)
function setupAutoUpdater() {
    // Configure auto-updater
    autoUpdater.autoDownload = true
    autoUpdater.autoInstallOnAppQuit = true

    // Enable updates in dev mode for testing
    if (is.dev) {
        autoUpdater.forceDevUpdateConfig = true
    }

    // For GitHub releases
    autoUpdater.setFeedURL({
        provider: 'github',
        owner: 'BlessedDayss',
        repo: 'SharkyParser'
    })

    // Update events
    autoUpdater.on('checking-for-update', () => {
        console.log('🔍 Checking for updates...')
    })

    autoUpdater.on('update-available', (info) => {
        console.log('🎉 Update available:', info.version)
        mainWindow?.webContents.send('update-available', info.version)
    })

    autoUpdater.on('update-not-available', () => {
        console.log('✅ App is up to date')
        mainWindow?.webContents.send('update-not-available')
    })

    autoUpdater.on('download-progress', (progress) => {
        console.log(`⏬ Downloading: ${Math.round(progress.percent)}%`)
        mainWindow?.webContents.send('download-progress', Math.round(progress.percent))
    })

    autoUpdater.on('update-downloaded', (info) => {
        console.log('✅ Update downloaded, will install on quit')
        mainWindow?.webContents.send('update-downloaded', info.version)
    })

    // Install and restart
    ipcMain.handle('install-update', () => {
        if (manualDownloadPath) {
            shell.openPath(manualDownloadPath)
            setTimeout(() => app.quit(), 1000)
        } else {
            autoUpdater.quitAndInstall(false, true)
        }
    })

    autoUpdater.on('error', async (err) => {
        console.error('❌ Update error:', err)

        // Smart Fallback: Download manually if metadata is missing (e.g. no latest-mac.yml)
        if (err.message.includes('latest-mac.yml') || err.message.includes('404') || err.message.includes('ENOENT')) {
            console.log('⚠️ Standard mechanism failed, switching to Smart Native Download...')
            try {
                await startSmartDownload(mainWindow)
            } catch (manualErr: any) {
                console.error('Smart download failed', manualErr)
                mainWindow?.webContents.send('update-error', 'Update failed: ' + manualErr.message)
            }
            return
        }

        mainWindow?.webContents.send('update-error', err.message)
    })

    // Manual check handler
    ipcMain.handle('check-for-updates', async () => {
        try {
            const result = await autoUpdater.checkForUpdates()
            return {
                available: result?.updateInfo && result.updateInfo.version !== app.getVersion(),
                version: result?.updateInfo.version || null
            }
        } catch (error: any) {
            // If fallback mechanism handles this, don't report error to UI
            if (error.message && (error.message.includes('latest-mac.yml') || error.message.includes('404') || error.message.includes('ENOENT'))) {
                console.log('⚠️ Update check error swallowed (Smart Download active)')
                return { available: false, fallbackStarted: true }
            }
            console.error('Update check failed:', error)
            return { available: false, error: String(error) }
        }
    })
}

// Global variable to store manually downloaded update path
let manualDownloadPath: string | null = null

async function startSmartDownload(win: BrowserWindow | null) {
    // If win arg is null, try global mainWindow
    if (!win) win = mainWindow

    // If still null, wait for it (startup race condition)
    if (!win) {
        console.log('⏳ Smart Download: Waiting for main window to be ready...')
        let attempts = 0
        while (!mainWindow && attempts < 20) { // Wait up to 10s
            await new Promise(r => setTimeout(r, 500))
            attempts++
        }
        win = mainWindow
        if (!win) {
            console.error('❌ Smart Download: Main window never became ready')
            return
        }
    }

    console.log('🚀 Smart Download: Fetching releases from GitHub API...')

    // 1. Fetch latest release info manually
    const release = await new Promise<any>((resolve, reject) => {
        const req = https.get({
            hostname: 'api.github.com',
            path: '/repos/BlessedDayss/SharkyParser/releases/latest',
            headers: { 'User-Agent': 'SharkyParser' }
        }, (res) => {
            let data = ''
            res.on('data', c => data += c)
            res.on('end', () => {
                try { resolve(JSON.parse(data)) } catch (e) { reject(e) }
            })
        })
        req.on('error', reject)
    })

    if (!release.tag_name) throw new Error('Failed to fetch release info')

    // 2. Find correct asset
    const isMac = process.platform === 'darwin'
    const extension = isMac ? '.dmg' : '.exe'
    const asset = release.assets.find((a: any) => a.name.endsWith(extension))

    if (!asset) throw new Error(`No ${extension} installer found in release`)

    // Notify UI: Update Available
    win.webContents.send('update-available', release.tag_name)

    // 3. Download
    const tempPath = app.getPath('temp')
    const filePath = path.join(tempPath, asset.name)
    manualDownloadPath = filePath

    const file = fs.createWriteStream(filePath)

    await new Promise<void>((resolve, reject) => {
        const req = https.get(asset.browser_download_url, {
            headers: { 'User-Agent': 'SharkyParser' }
        }, (res) => {
            // Handle redirects (GitHub uses 302 for downloads)
            if (res.statusCode === 302 && res.headers.location) {
                https.get(res.headers.location, (redirectRes) => {
                    const total = parseInt(redirectRes.headers['content-length'] || '0', 10)
                    let current = 0

                    redirectRes.pipe(file)

                    redirectRes.on('data', (chunk) => {
                        current += chunk.length
                        if (total > 0) {
                            const percent = Math.round((current / total) * 100)
                            win.webContents.send('download-progress', percent)
                        }
                    })

                    file.on('finish', () => {
                        file.close()
                        resolve()
                    })
                }).on('error', reject)
                return
            }

            // Direct download (unlikely for GitHub releases but safe to handle)
            const total = parseInt(res.headers['content-length'] || '0', 10)
            let current = 0
            res.pipe(file)

            res.on('data', (chunk) => {
                current += chunk.length
                if (total > 0) {
                    const percent = Math.round((current / total) * 100)
                    win.webContents.send('download-progress', percent)
                }
            })
            file.on('finish', () => { file.close(); resolve() })
        })
        req.on('error', reject)
    })

    // 4. Notify Ready
    win.webContents.send('update-downloaded', release.tag_name)
}
