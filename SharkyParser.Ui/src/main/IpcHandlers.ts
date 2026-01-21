import { ipcMain, dialog, app, shell } from 'electron'
import { BackendService } from './services/BackendService'
import { WindowManager } from './services/WindowManager'
import * as path from 'path'

export class IpcHandlers {
    constructor(
        private backendService: BackendService,
        private windowManager: WindowManager
    ) { }

    public register() {
        ipcMain.handle('select-file', async () => {
            const result = await dialog.showOpenDialog({
                properties: ['openFile'],
                filters: [{ name: 'Log Files', extensions: ['log', 'txt', 'csv'] }]
            })
            return result.canceled ? null : result.filePaths[0]
        })

        ipcMain.handle('check-csharp-backend', () => {
            return this.backendService.isReady()
        })

        ipcMain.handle('get-app-version', () => {
            return app.getVersion()
        })

        ipcMain.handle('read-changelog', async () => {
            const isDev = !app.isPackaged
            const filePath = isDev
                ? path.join(app.getAppPath(), '..', 'Changelog.md')
                : path.join(process.resourcesPath, 'Changelog.md')

            try {
                const fs = await import('fs/promises')
                return await fs.readFile(filePath, 'utf-8')
            } catch (e) {
                console.error('Failed to read changelog:', e)
                return ''
            }
        })

        ipcMain.handle('set-zoom', (_, factor: number) => {
            const win = this.windowManager.getMainWindow()
            if (win) {
                win.webContents.setZoomFactor(factor)
            }
        })

        ipcMain.handle('open-external', (_, url: string) => {
            shell.openExternal(url)
        })

        ipcMain.handle('parse-log-csharp', async (_, filePath: string, logType: string) => {
            return this.backendService.parseLog(filePath, logType)
        })

        ipcMain.handle('analyze-log-csharp', async (_, filePath: string, logType: string) => {
            return this.backendService.analyzeLog(filePath, logType)
        })
    }
}
