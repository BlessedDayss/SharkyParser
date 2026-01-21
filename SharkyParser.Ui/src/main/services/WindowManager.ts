import { BrowserWindow, app, nativeImage, shell } from 'electron'
import { join } from 'path'
import { is } from '@electron-toolkit/utils'
import * as path from 'path'

export class WindowManager {
    private mainWindow: BrowserWindow | null = null
    private splashWindow: BrowserWindow | null = null
    private readonly resourcesPath: string

    constructor() {
        this.resourcesPath = app.isPackaged
            ? path.join(process.resourcesPath, 'assets')
            : path.join(app.getAppPath(), 'resources')
    }

    public createSplashScreen(): BrowserWindow {
        const iconPath = path.join(this.resourcesPath, 'app_icon.png')
        const icon = nativeImage.createFromPath(iconPath)

        this.splashWindow = new BrowserWindow({
            width: 440,
            height: 340,
            transparent: true,
            frame: false,
            alwaysOnTop: true,
            skipTaskbar: true,
            icon: icon.isEmpty() ? undefined : icon,
            webPreferences: {
                nodeIntegration: false,
                contextIsolation: true,
                sandbox: false
            }
        })

        this.splashWindow.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(this.getSplashHtml())}`)
        return this.splashWindow
    }

    public createMainWindow(preloadPath: string): BrowserWindow {
        const icon = nativeImage.createFromPath(path.join(this.resourcesPath, 'app_icon.png'))

        this.mainWindow = new BrowserWindow({
            width: 1200,
            height: 850,
            show: false,
            autoHideMenuBar: true,
            backgroundColor: '#05060f',
            title: 'Sharky Parser PRO',
            icon: icon.isEmpty() ? undefined : icon,
            webPreferences: {
                preload: preloadPath,
                sandbox: false,
                contextIsolation: true,
                nodeIntegration: false
            }
        })

        this.mainWindow.on('ready-to-show', () => {
            if (this.splashWindow) {
                this.splashWindow.close()
                this.splashWindow = null
            }
            this.mainWindow?.show()
        })

        this.mainWindow.webContents.setWindowOpenHandler((details) => {
            shell.openExternal(details.url)
            return { action: 'deny' }
        })

        if (is.dev && process.env['ELECTRON_RENDERER_URL']) {
            this.mainWindow.loadURL(process.env['ELECTRON_RENDERER_URL'])
        } else {
            this.mainWindow.loadFile(join(path.dirname(preloadPath), '../renderer/index.html'))
        }

        return this.mainWindow
    }

    public getMainWindow(): BrowserWindow | null {
        return this.mainWindow
    }

    private getSplashHtml(): string {
        return `
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
    `
    }
}
