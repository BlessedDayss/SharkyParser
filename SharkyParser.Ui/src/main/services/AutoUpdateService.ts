import { app, ipcMain, shell, BrowserWindow } from 'electron'
import pkg from 'electron-updater'
import * as fs from 'fs'
import * as path from 'path'
import * as https from 'https'
import { is } from '@electron-toolkit/utils'

const { autoUpdater } = pkg

export class AutoUpdateService {
    private mainWindow: BrowserWindow | null = null
    private manualDownloadPath: string | null = null
    private isSmartDownloading = false

    constructor() {
        this.setupHandlers()
    }

    public init(win: BrowserWindow | null) {
        this.mainWindow = win
        this.setupAutoUpdater()
    }

    private setupHandlers() {
        ipcMain.handle('check-for-updates', async () => {
            try {
                const result = await autoUpdater.checkForUpdates()
                const remoteVersion = result?.updateInfo?.version
                const localVersion = app.getVersion()

                const isNewer = remoteVersion && this.isNewer(remoteVersion, localVersion)

                return {
                    available: !!isNewer,
                    version: remoteVersion || null
                }
            } catch (error: any) {
                if (this.isMissingMetadata(error)) {
                    try {
                        const release = await this.fetchLatestRelease()
                        const remoteVersion = release.tag_name
                        const localVersion = app.getVersion()

                        if (this.isNewer(remoteVersion, localVersion)) {
                            return { available: false, fallbackStarted: true, version: remoteVersion }
                        }
                        return { available: false }
                    } catch (e) {
                        return { available: false, error: String(e) }
                    }
                }
                return { available: false, error: String(error) }
            }
        })

        ipcMain.on('download-update', async () => {
            if (this.isSmartDownloading) return
            try {
                // If autoUpdater has the update, use it
                await autoUpdater.downloadUpdate()
            } catch (error: any) {
                // Fallback to manual if autoUpdater fails
                this.startSmartDownload()
            }
        })

        ipcMain.handle('install-update', async () => {
            if (this.manualDownloadPath) {
                await this.launchManualInstaller()
            } else {
                autoUpdater.quitAndInstall(false, true)
            }
        })
    }

    private setupAutoUpdater() {
        autoUpdater.autoDownload = false
        autoUpdater.logger = null
        autoUpdater.autoInstallOnAppQuit = true

        if (is.dev) {
            autoUpdater.forceDevUpdateConfig = true
        }

        autoUpdater.setFeedURL({
            provider: 'github',
            owner: 'BlessedDayss',
            repo: 'SharkyParser'
        })

        autoUpdater.on('update-available', (info) => {
            this.mainWindow?.webContents.send('update-available', info.version)
        })

        autoUpdater.on('update-not-available', () => {
            this.mainWindow?.webContents.send('update-not-available')
        })

        autoUpdater.on('download-progress', (progress) => {
            this.mainWindow?.webContents.send('download-progress', Math.round(progress.percent))
        })

        autoUpdater.on('update-downloaded', (info) => {
            this.mainWindow?.webContents.send('update-downloaded', info.version)
        })

        autoUpdater.on('error', async (err) => {
            if (this.isMissingMetadata(err)) {
                await this.startSmartDownload()
                return
            }
            this.mainWindow?.webContents.send('update-error', err.message)
        })
    }

    private isMissingMetadata(err: any): boolean {
        const msg = err.message || ''
        return msg.includes('latest-mac.yml') || msg.includes('latest.yml') || msg.includes('404') || msg.includes('ENOENT')
    }

    private async startSmartDownload() {
        if (this.isSmartDownloading || !this.mainWindow) return
        this.isSmartDownloading = true

        try {
            const release = await this.fetchLatestRelease()
            if (!this.isNewer(release.tag_name, app.getVersion())) {
                this.mainWindow.webContents.send('update-not-available')
                return
            }

            const asset = this.findCorrectAsset(release.assets)
            if (!asset) throw new Error('No valid installer found in release assets')

            this.mainWindow.webContents.send('update-available', release.tag_name)
            const tempPath = path.join(app.getPath('temp'), asset.name)
            this.manualDownloadPath = tempPath

            await this.downloadFile(asset.browser_download_url, tempPath, (percent) => {
                this.mainWindow?.webContents.send('download-progress', percent)
            })

            this.mainWindow.webContents.send('update-downloaded', release.tag_name)
        } catch (err: any) {
            this.mainWindow.webContents.send('update-error', err.message)
        } finally {
            this.isSmartDownloading = false
        }
    }

    private async fetchLatestRelease(): Promise<any> {
        return new Promise((resolve, reject) => {
            const options = {
                hostname: 'api.github.com',
                path: '/repos/BlessedDayss/SharkyParser/releases/latest',
                headers: { 'User-Agent': 'SharkyParser' }
            }
            https.get(options, (res) => {
                let data = ''
                res.on('data', c => data += c)
                res.on('end', () => {
                    if (res.statusCode !== 200) reject(new Error(`GitHub API error: ${res.statusCode}`))
                    else resolve(JSON.parse(data))
                })
            }).on('error', reject)
        })
    }

    private isNewer(remote: string, local: string): boolean {
        const r = remote.replace(/^v/, '').split('.').map(Number)
        const l = local.replace(/^v/, '').split('.').map(Number)
        for (let i = 0; i < 3; i++) {
            if ((r[i] || 0) > (l[i] || 0)) return true
            if ((r[i] || 0) < (l[i] || 0)) return false
        }
        return false
    }

    private findCorrectAsset(assets: any[]): any {
        const isMac = process.platform === 'darwin'
        return assets.find((a: any) => {
            const name = a.name.toLowerCase()
            return isMac ? name.endsWith('.dmg') : (name.endsWith('.exe') && !name.includes('blockmap'))
        })
    }

    private async downloadFile(url: string, dest: string, onProgress: (p: number) => void): Promise<void> {
        return new Promise((resolve, reject) => {
            const request = (currentUrl: string) => {
                https.get(currentUrl, { headers: { 'User-Agent': 'SharkyParser' } }, (res) => {
                    if ([301, 302, 303, 307, 308].includes(res.statusCode || 0) && res.headers.location) {
                        request(res.headers.location)
                        return
                    }
                    if (res.statusCode !== 200) return reject(new Error(`Download failed: ${res.statusCode}`))

                    const file = fs.createWriteStream(dest)
                    const total = parseInt(res.headers['content-length'] || '0', 10)
                    let received = 0
                    res.pipe(file)
                    res.on('data', c => {
                        received += c.length
                        if (total > 0) onProgress(Math.round((received / total) * 100))
                    })
                    file.on('finish', () => { file.close(); resolve(); })
                }).on('error', reject)
            }
            request(url)
        })
    }

    private async launchManualInstaller() {
        if (!this.manualDownloadPath) return
        const isWin = process.platform === 'win32'
        if (isWin && this.manualDownloadPath.endsWith('.exe')) {
            const { spawn } = await import('child_process')
            const child = spawn(`"${this.manualDownloadPath}"`, [], { detached: true, stdio: 'ignore', shell: true })
            child.unref()
            setTimeout(() => app.exit(0), 500)
        } else {
            await shell.openPath(this.manualDownloadPath)
            app.exit(0)
        }
    }
}
