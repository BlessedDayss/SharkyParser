import { app, BrowserWindow } from 'electron'
import { optimizer } from '@electron-toolkit/utils'
import { WindowManager } from './services/WindowManager'
import { BackendService } from './services/BackendService'
import { AutoUpdateService } from './services/AutoUpdateService'
import { IpcHandlers } from './IpcHandlers'
import { join } from 'path'

class Main {
    private windowManager: WindowManager
    private backendService: BackendService
    private autoUpdateService: AutoUpdateService
    private ipcHandlers: IpcHandlers

    constructor() {
        this.windowManager = new WindowManager()
        this.backendService = new BackendService()
        this.autoUpdateService = new AutoUpdateService()
        this.ipcHandlers = new IpcHandlers(this.backendService, this.windowManager)
    }

    public init() {
        app.whenReady().then(() => {
            // Set app user model id for windows
            if (process.platform === 'win32') {
                app.setAppUserModelId('com.sharkyparser.app')
            }

            app.on('browser-window-created', (_, window) => {
                optimizer.watchWindowShortcuts(window)
            })

            this.windowManager.createSplashScreen()

            const preloadPath = join(__dirname, '../preload/index.mjs')
            const mainWindow = this.windowManager.createMainWindow(preloadPath)

            this.autoUpdateService.init(mainWindow)
            this.ipcHandlers.register()

            app.on('activate', () => {
                if (BrowserWindow.getAllWindows().length === 0) {
                    this.windowManager.createMainWindow(preloadPath)
                }
            })
        })

        app.on('window-all-closed', () => {
            if (process.platform !== 'darwin') {
                app.quit()
            }
        })
    }
}

const main = new Main()
main.init()
