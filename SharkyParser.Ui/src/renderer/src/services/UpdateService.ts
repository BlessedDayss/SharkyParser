export class UpdateService {
    private checkUpdateBtn: HTMLElement | null
    private updateStatus: HTMLElement | null
    private updateProgressContainer: HTMLElement | null
    private updateProgressBar: HTMLElement | null
    private currentVersionEl: HTMLElement | null

    constructor() {
        this.checkUpdateBtn = document.getElementById('check-update-btn')
        this.updateStatus = document.getElementById('update-status')
        this.updateProgressContainer = document.getElementById('update-progress-container')
        this.updateProgressBar = document.getElementById('update-progress-bar')
        this.currentVersionEl = document.getElementById('current-version')
    }

    public async init() {
        this.checkUpdateBtn?.addEventListener('click', () => this.checkForUpdates())
        this.setupIpcListeners()

        // Show current version
        const version = await (window as any).electron.ipcRenderer.invoke('get-app-version')
        if (this.currentVersionEl) this.currentVersionEl.textContent = `v${version}`
    }

    private setupIpcListeners() {
        const { ipcRenderer } = (window as any).electron

        ipcRenderer.on('update-available', (_, version) => {
            this.setUpdateStatus(`Update available: ${version}`, 'var(--info)')
        })

        ipcRenderer.on('update-not-available', () => {
            this.setUpdateStatus('App is up to date', 'var(--success)')
        })

        ipcRenderer.on('download-progress', (_, progress) => {
            if (this.updateProgressContainer) this.updateProgressContainer.style.display = 'block'
            if (this.updateProgressBar) this.updateProgressBar.style.width = `${progress}%`
            this.setUpdateStatus(`Downloading: ${progress}%`, 'var(--info)')
        })

        ipcRenderer.on('update-downloaded', (_, version) => {
            this.setUpdateStatus(`Ready to install: ${version}`, 'var(--success)')
            if (this.checkUpdateBtn) {
                this.checkUpdateBtn.innerHTML = '<span>🚀</span> Install & Restart'
                this.checkUpdateBtn.onclick = () => ipcRenderer.invoke('install-update')
            }
        })

        ipcRenderer.on('update-error', (_, err) => {
            this.setUpdateStatus(`Update Error: ${err}`, 'var(--error)')
        })
    }

    private async checkForUpdates() {
        if (this.checkUpdateBtn) this.checkUpdateBtn.innerHTML = '<span>⏳</span> Checking...'
        const result = await (window as any).electron.ipcRenderer.invoke('check-for-updates')
        if (this.checkUpdateBtn) this.checkUpdateBtn.innerHTML = '<span>🆕</span> Check for Updates'

        if (result.available || result.fallbackStarted) {
            this.setUpdateStatus(result.fallbackStarted ? 'Smart Download started...' : 'Update found!', 'var(--info)')
        } else if (result.error) {
            this.setUpdateStatus('Error checking updates', 'var(--error)')
        }
    }

    private setUpdateStatus(text: string, color: string) {
        if (this.updateStatus) {
            this.updateStatus.style.display = 'block'
            this.updateStatus.textContent = text
            this.updateStatus.style.color = color
        }
    }
}
