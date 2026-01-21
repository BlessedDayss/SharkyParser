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
        this.setupModalHandlers()

        // Show current version
        const version = await (window as any).electron.ipcRenderer.invoke('get-app-version')
        if (this.currentVersionEl) this.currentVersionEl.textContent = `v${version}`

        // Startup check
        this.checkForUpdates(true)
    }

    private setupModalHandlers() {
        const updateModal = document.getElementById('update-notification-modal')
        const updateNowBtn = document.getElementById('update-now-btn')
        const skipBtn = document.getElementById('skip-update-btn')

        updateNowBtn?.addEventListener('click', () => {
            updateModal?.classList.remove('active')
            // Switch to settings
            const app = (window as any).app
            app.navigationService.switchView('settings')
            // Trigger download
            const { ipcRenderer } = (window as any).electron
            ipcRenderer.send('download-update')
        })

        skipBtn?.addEventListener('click', () => {
            updateModal?.classList.remove('active')
        })
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

    private async checkForUpdates(isStartup: boolean = false) {
        if (!isStartup && this.checkUpdateBtn) this.checkUpdateBtn.innerHTML = '<span>⏳</span> Checking...'
        const result = await (window as any).electron.ipcRenderer.invoke('check-for-updates')
        if (!isStartup && this.checkUpdateBtn) this.checkUpdateBtn.innerHTML = '<span>🆕</span> Check for Updates'

        if (result.available || result.fallbackStarted) {
            this.setUpdateStatus(result.fallbackStarted ? 'Smart Download ready...' : 'Update found!', 'var(--info)')

            // If manual check (not startup), trigger download immediately
            if (!isStartup) {
                (window as any).electron.ipcRenderer.send('download-update')
            }

            if (isStartup) {
                const updateModal = document.getElementById('update-notification-modal')
                const modalText = document.getElementById('update-modal-text')
                if (modalText && result.version) {
                    modalText.innerText = `A newer version (v${result.version}) is available. Do you want to update now?`
                }
                updateModal?.classList.add('active')
            }
        } else if (result.error && !isStartup) {
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
