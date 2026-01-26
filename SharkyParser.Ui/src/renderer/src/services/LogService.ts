import { LogEntry } from '../core/App'

export class LogService {
    public allEntries: LogEntry[] = []
    private currentFilePath: string | null = null
    private selectFileBtn: HTMLButtonElement | null
    private fileInfo: HTMLDivElement | null
    private levelFilter: HTMLSelectElement | null
    private searchInput: HTMLInputElement | null
    private selectedLogType: string = 'Installation'

    constructor() {
        this.selectFileBtn = document.getElementById('select-file-btn') as HTMLButtonElement
        this.fileInfo = document.getElementById('file-info') as HTMLDivElement
        this.levelFilter = document.getElementById('level-filter') as HTMLSelectElement
        this.searchInput = document.getElementById('search-input') as HTMLInputElement
    }

    public isFileSelected(): boolean {
        return this.currentFilePath !== null
    }

    public async triggerFilePicker() {
        await this.handleFileSelection()
    }

    public init() {
        this.selectFileBtn?.addEventListener('click', () => this.handleFileSelection())
        this.searchInput?.addEventListener('input', () => this.applyFilters())
        this.levelFilter?.addEventListener('change', () => this.applyFilters())

        const logTypeSelectors = document.querySelectorAll('.log-type-btn')
        logTypeSelectors.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const target = e.currentTarget as HTMLElement
                if (target.classList.contains('disabled')) {
                    // Show "Coming Soon" modal
                    document.getElementById('notice-modal')?.classList.add('active')
                    return
                }

                logTypeSelectors.forEach(b => b.classList.remove('active'))
                target.classList.add('active')
                this.selectedLogType = target.dataset.type || 'Installation'

                if (this.currentFilePath) {
                    this.parseFile(this.currentFilePath)
                }
            })
        })

        // Stats Cards Filtering
        const statCards = document.querySelectorAll('.stat-card.clickable')
        statCards.forEach(card => {
            card.addEventListener('click', () => {
                const filter = (card as HTMLElement).dataset.filter || 'ALL'
                if (this.levelFilter) {
                    this.levelFilter.value = filter
                    this.applyFilters()
                    // Switch to logs view when clicking a stat card
                    window.dispatchEvent(new CustomEvent('switch-view', { detail: 'logs' }))
                }
            })
        })

        window.addEventListener('switch-view', (e: any) => {
            (window as any).app.navigationService.switchView(e.detail)
        })
    }

    private async handleFileSelection() {
        try {
            const result: string = await (window as any).electron.ipcRenderer.invoke('select-file')
            if (result) {
                // Switch view to logs if we selected a file
                window.dispatchEvent(new CustomEvent('switch-view', { detail: 'logs' }))
                this.currentFilePath = result
                await this.parseFile(result)
            }
        } catch (error: any) {
            alert('Error: ' + error.message)
        } finally {
            if (this.selectFileBtn) this.selectFileBtn.innerHTML = '<span>🚀</span> Open Log File'
        }
    }

    private async parseFile(filePath: string) {
        if (this.fileInfo) this.fileInfo.textContent = filePath.split('/').pop() || filePath
        if (this.selectFileBtn) this.selectFileBtn.innerHTML = '<span>⏳</span> Parsing...'

        const data = await (window as any).electron.ipcRenderer.invoke('parse-log-csharp', filePath, this.selectedLogType)
        this.allEntries = data.entries.map((e: any, idx: number) => ({ ...e, id: idx }))

        this.updateStats(data.statistics)
        this.applyFilters()

        window.dispatchEvent(new CustomEvent('logs-parsed', { detail: { filePath, logType: this.selectedLogType } }))
    }

    public applyFilters() {
        const searchTerm = this.searchInput?.value.toLowerCase() || ''
        const level = this.levelFilter?.value || 'ALL'

        const filtered = this.allEntries.filter(entry => {
            const matchesSearch = entry.message.toLowerCase().includes(searchTerm)
            const matchesLevel = level === 'ALL' || entry.level.toUpperCase() === level.toUpperCase()
            return matchesSearch && matchesLevel
        })

        window.dispatchEvent(new CustomEvent('logs-filtered', { detail: filtered }))
    }

    private updateStats(stats: any) {
        const statTotal = document.getElementById('stat-total')
        const statErrors = document.getElementById('stat-errors')
        const statWarnings = document.getElementById('stat-warnings')
        const statInfo = document.getElementById('stat-info')

        if (statTotal) statTotal.textContent = stats.total.toString()
        if (statErrors) statErrors.textContent = stats.errors.toString()
        if (statWarnings) statWarnings.textContent = stats.warnings.toString()
        if (statInfo) statInfo.textContent = stats.info.toString()
    }
}
