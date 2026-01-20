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

const __dirname = dirname(fileURLToPath(import.meta.url))

const APP_ID = 'Sharky.Pro.v1'
if (process.platform === 'win32') {
    app.setAppUserModelId(APP_ID)
}

let splashWindow: BrowserWindow | null = null
let mainWindow: BrowserWindow | null = null

function createSplashScreen(): BrowserWindow {
    const resourcesPath = app.isPackaged
        ? path.join(process.resourcesPath, 'assets')
        : path.join(app.getAppPath(), 'resources')

    // Using high-quality PNG for Splash
    const iconPath = path.join(resourcesPath, 'app_icon.png')
    const icon = nativeImage.createFromPath(iconPath)

    const splash = new BrowserWindow({
        width: 440,
        height: 340,
        transparent: true,
        frame: false,
        alwaysOnTop: true,
        skipTaskbar: true, // IMPORTANT: Prevents double icons in taskbar
        icon: icon.isEmpty() ? undefined : icon,
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
            sandbox: false
        }
    })

    splash.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(`
    <!DOCTYPE html>
    <html>
    <head>
      <style>
        body { margin: 0; padding: 0; background: transparent; display: flex; align-items: center; justify-content: center; height: 100vh; font-family: 'Segoe UI', sans-serif; overflow: hidden; }
        .container { background: linear-gradient(135deg, #05060f 0%, #101225 100%); border-radius: 32px; padding: 45px; box-shadow: 0 40px 80px rgba(0,0,0,0.8); text-align: center; border: 1px solid rgba(255,255,255,0.1); width: 310px; position: relative; }
        .logo { font-size: 90px; margin-bottom: 25px; filter: drop-shadow(0 0 25px rgba(0, 242, 255, 0.5)); animation: float 3s ease-in-out infinite; }
        @keyframes float { 0%, 100% { transform: translateY(0) rotate(0); } 50% { transform: translateY(-12px) rotate(5deg); } }
        .title { font-size: 28px; font-weight: 900; background: linear-gradient(to right, #fff, #00f2ff); -webkit-background-clip: text; -webkit-text-fill-color: transparent; margin-bottom: 8px; letter-spacing: -1px; }
        .subtitle { font-size: 13px; color: #64748b; margin-bottom: 35px; text-transform: uppercase; letter-spacing: 3px; font-weight: 600; }
        .loader { width: 230px; height: 4px; background: rgba(255,255,255,0.05); border-radius: 10px; overflow: hidden; margin: 0 auto; }
        .bar { height: 100%; background: linear-gradient(90deg, #7c3aed, #06b6d4); border-radius: 10px; width: 40%; animation: load 1.8s ease-in-out infinite; }
        @keyframes load { 0% { transform: translateX(-100%); } 100% { transform: translateX(250%); } }
        .status { margin-top: 25px; font-size: 11px; color: #00f2ff; opacity: 0.8; font-weight: 700; letter-spacing: 1px; }
      </style>
    </head>
    <body>
      <div class="container">
        <div class="logo">🦈</div>
        <div class="title">SHARKY PARSER</div>
        <div class="subtitle">Ultimate Engine</div>
        <div class="loader"><div class="bar"></div></div>
        <div class="status" id="st">LOADING SYSTEM...</div>
      </div>
      <script>
        let d = 0; setInterval(() => { d = (d + 1) % 4; document.getElementById('st').innerText = 'LOADING SYSTEM' + '.'.repeat(d); }, 500);
      </script>
    </body>
    </html>
    `)}`)

    return splash
}

function createMainWindow(): BrowserWindow {
    const resourcesPath = app.isPackaged
        ? path.join(process.resourcesPath, 'assets') // In production
        : path.join(app.getAppPath(), 'resources')   // In development/preview

    const icon = nativeImage.createFromPath(path.join(resourcesPath, 'app_icon.png'))

    const main = new BrowserWindow({
        width: 1200,
        height: 850,
        show: false,
        autoHideMenuBar: true,
        backgroundColor: '#05060f',
        title: 'Sharky Parser PRO',
        icon: icon.isEmpty() ? undefined : icon,
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

        if (process.platform === 'win32') {
            app.setAppUserModelId(APP_ID)
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
    return fs.existsSync(sharkyPath)
}

app.whenReady().then(async () => {
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

    ipcMain.handle('get-changelog-path', () => {
        const isDev = !app.isPackaged
        if (isDev) {
            return path.join(app.getAppPath(), '..', 'Changelog.md')
        }
        return path.join(process.resourcesPath, 'Changelog.md')
    })

    ipcMain.handle('set-zoom', (_, factor: number) => {
        if (mainWindow) {
            mainWindow.webContents.setZoomFactor(factor)
        }
    })


    ipcMain.handle('parse-log-csharp', async (_, filePath: string, logType: string = 'Installation') => {
        const sharkyPath = findSharky()
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(sharkyPath, ['parse', filePath, '--type', logType, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.stderr.on('data', (data) => console.error('CLI stderr:', data.toString()))
            proc.on('close', (code) => {
                if (code === 0) resolve(parseEmbeddedOutput(output))
                else reject(new Error(`Exit code ${code}`))
            })
        })
    })

    ipcMain.handle('analyze-log-csharp', async (_, filePath: string, logType: string = 'Installation') => {
        const sharkyPath = findSharky()
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(sharkyPath, ['analyze', filePath, '--type', logType, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.stderr.on('data', (data) => console.error('CLI stderr:', data.toString()))
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

    // Configure AutoUpdater Noise Reduction
    autoUpdater.autoDownload = false
    autoUpdater.logger = null // Stop default console spam as we handle errors ourselves

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
    ipcMain.handle('install-update', async () => {
        if (manualDownloadPath) {
            const isWin = process.platform === 'win32'
            console.log(`🚀 Launching Installer: ${manualDownloadPath}`)

            if (isWin && manualDownloadPath.endsWith('.exe')) {
                // For Windows: Run installer normally so users see the progress/UAC prompt
                const { spawn } = await import('child_process')

                // We launch it detached and immediately exit our app
                // This allows the installer to "replace" the files of the closed app
                const child = spawn(`"${manualDownloadPath}"`, [], {
                    detached: true,
                    stdio: 'ignore',
                    shell: true
                })
                child.unref()

                console.log('🏁 Terminating app for update...')
                setTimeout(() => app.exit(0), 500)
            } else {
                // For Mac/Linux, open the DMG/AppImage
                await shell.openPath(manualDownloadPath)
                app.exit(0)
            }
        } else {
            console.log('🚀 Using native quitAndInstall...')
            autoUpdater.quitAndInstall(false, true)
        }
    })

    autoUpdater.on('error', async (err) => {
        // Silence noisy 404/metadata errors as they are expected for some release types
        const isExpectedError = err.message && (
            err.message.includes('latest-mac.yml') ||
            err.message.includes('latest.yml') ||
            err.message.includes('404') ||
            err.message.includes('ENOENT')
        )

        if (isExpectedError) {
            console.log('⚠️ Standard mechanism failed, switching to Smart Native Download...')
            try {
                await startSmartDownload(mainWindow)
            } catch (manualErr: any) {
                console.error('Smart download failed', manualErr)
                mainWindow?.webContents.send('update-error', 'Update failed: ' + manualErr.message)
            }
            return
        }

        console.error('❌ Update error:', err)
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
            // Silence noisy 404 errors as they are expected for old releases without latest.yml
            const isMissingMetadata = error.message && (
                error.message.includes('latest-mac.yml') ||
                error.message.includes('latest.yml') ||
                error.message.includes('404') ||
                error.message.includes('ENOENT')
            )

            if (isMissingMetadata) {
                console.log('⚠️ Standard Update info not found. Starting Smart Fallback...')
                await startSmartDownload(mainWindow)
                return { available: false, fallbackStarted: true }
            }

            console.error('Update check failed:', error)
            return { available: false, error: String(error) }
        }
    })
}

// Global variable to store manually downloaded update path
let manualDownloadPath: string | null = null
let isSmartDownloading = false

/**
 * Handles cross-platform version comparison
 * Returns true if remote version > local version
 */
function isNewer(remote: string, local: string): boolean {
    const r = remote.replace(/^v/, '').split('.').map(Number)
    const l = local.replace(/^v/, '').split('.').map(Number)
    for (let i = 0; i < 3; i++) {
        if ((r[i] || 0) > (l[i] || 0)) return true
        if ((r[i] || 0) < (l[i] || 0)) return false
    }
    return false
}

/**
 * Robust download function with multi-level redirect support
 */
async function downloadWithRedirects(url: string, dest: string, onProgress: (percent: number) => void): Promise<void> {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(dest)
        let totalBytes = 0
        let receivedBytes = 0

        const request = (currentUrl: string) => {
            https.get(currentUrl, {
                headers: { 'User-Agent': 'SharkyParser' }
            }, (res) => {
                // Handle Redirects (301, 302, 303, 307, 308)
                if ([301, 302, 303, 307, 308].includes(res.statusCode || 0) && res.headers.location) {
                    console.log('🔗 Redirecting to:', res.headers.location)
                    request(res.headers.location)
                    return
                }

                if (res.statusCode !== 200) {
                    reject(new Error(`Server returned status code ${res.statusCode}`))
                    return
                }

                totalBytes = parseInt(res.headers['content-length'] || '0', 10)
                res.pipe(file)

                res.on('data', (chunk) => {
                    receivedBytes += chunk.length
                    if (totalBytes > 0) {
                        const percent = Math.round((receivedBytes / totalBytes) * 100)
                        onProgress(percent)
                    }
                })

                file.on('finish', () => {
                    file.close()
                    resolve()
                })

                file.on('error', (err) => {
                    fs.unlink(dest, () => reject(err))
                })
            }).on('error', (err) => {
                fs.unlink(dest, () => reject(err))
            })
        }

        request(url)
    })
}

async function startSmartDownload(win: BrowserWindow | null) {
    if (isSmartDownloading) {
        console.log('⏳ Smart Download already in progress, skipping...')
        return
    }

    if (!win) win = mainWindow
    if (!win) return

    isSmartDownloading = true
    console.log('🚀 Smart Download: Fetching releases from GitHub API...')

    try {
        // 1. Fetch latest release info
        const release = await new Promise<any>((resolve, reject) => {
            const req = https.get({
                hostname: 'api.github.com',
                path: '/repos/BlessedDayss/SharkyParser/releases/latest',
                headers: { 'User-Agent': 'SharkyParser' }
            }, (res) => {
                let data = ''
                res.on('data', c => data += c)
                res.on('end', () => {
                    if (res.statusCode === 403) reject(new Error('GitHub API Rate Limit Exceeded. Try again later.'))
                    else if (res.statusCode !== 200) reject(new Error(`GitHub API error: ${res.statusCode}`))
                    else {
                        try { resolve(JSON.parse(data)) } catch (e) { reject(e) }
                    }
                })
            })
            req.on('error', reject)
        })

        if (!release.tag_name) throw new Error('No tag_name found in release')

        // 2. Version Check
        const currentVersion = app.getVersion()
        if (!isNewer(release.tag_name, currentVersion)) {
            console.log(`✅ Version ${currentVersion} is current. Latest is ${release.tag_name}`)
            manualDownloadPath = null
            win.webContents.send('update-not-available')
            return
        }

        console.log(`✨ New version confirmed: ${release.tag_name} (Local: ${currentVersion})`)

        // 3. Find correct asset
        const isMac = process.platform === 'darwin'
        const asset = release.assets.find((a: any) => {
            const name = a.name.toLowerCase()
            if (isMac) return name.endsWith('.dmg')
            // For Windows, prefer Setup.exe or the largest .exe (avoiding blockmaps)
            return name.endsWith('.exe') && !name.includes('blockmap')
        })

        if (!asset) throw new Error(`Could not find a valid installer (.${isMac ? 'dmg' : 'exe'}) in release assets`)

        // Notify UI: Update Available
        win.webContents.send('update-available', release.tag_name)

        // 4. Download
        const tempPath = app.getPath('temp')
        const filePath = path.join(tempPath, asset.name)
        manualDownloadPath = filePath

        await downloadWithRedirects(asset.browser_download_url, filePath, (percent) => {
            win?.webContents.send('download-progress', percent)
        })

        // 5. Notify Ready
        console.log('✅ Update downloaded to:', filePath)
        win.webContents.send('update-downloaded', release.tag_name)

    } catch (err: any) {
        console.error('❌ Smart Download Failed:', err.message)
        win.webContents.send('update-error', err.message)
    } finally {
        isSmartDownloading = false
    }
}

