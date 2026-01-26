import { NavigationService } from '../services/NavigationService'
import { LogService } from '../services/LogService'
import { ChartService } from '../services/ChartService'
import { UpdateService } from '../services/UpdateService'
import { ZoomManager } from '../components/ZoomManager'
import { LogTable } from '../components/LogTable'
import { LogModal } from '../components/LogModal'

export interface LogEntry {
    id: number
    timestamp: string
    level: string
    message: string
    source: string
    stackTrace: string
    lineNumber?: string
    filePath?: string
}

export class App {
    private static instance: App
    public navigationService: NavigationService
    public logService: LogService
    public chartService: ChartService
    public updateService: UpdateService
    public zoomManager: ZoomManager
    public logTable: LogTable
    public logModal: LogModal

    constructor() {
        this.navigationService = new NavigationService()
        this.logService = new LogService()
        this.chartService = new ChartService()
        this.updateService = new UpdateService()
        this.zoomManager = new ZoomManager()
        this.logTable = new LogTable()
        this.logModal = new LogModal()
    }

    public static getInstance(): App {
        if (!App.instance) {
            App.instance = new App()
        }
        return App.instance
    }

    public init() {
        (window as any).app = this

        this.navigationService.init()
        this.logService.init()
        this.chartService.init()
        this.updateService.init()
        this.zoomManager.init()
        this.logTable.init()
        this.logModal.init()

        this.setupEventListeners()
        this.checkBackend()
    }

    private setupEventListeners() {
        // Theme Toggle Interceptor
        const themeToggle = document.getElementById('theme-toggle') as HTMLInputElement
        const noticeModal = document.getElementById('notice-modal')
        const closeNoticeBtn = document.getElementById('close-notice-modal')
        const requestFeatureBtn = document.getElementById('request-feature-btn')

        if (themeToggle) {
            themeToggle.checked = false // Always keep it unchecked (dark)
            themeToggle.addEventListener('click', (e) => {
                e.preventDefault() // Stop the checkbox from actually changing
                themeToggle.checked = false
                noticeModal?.classList.add('active')
            })
        }

        closeNoticeBtn?.addEventListener('click', () => {
            noticeModal?.classList.remove('active')
        })

        noticeModal?.addEventListener('click', (e) => {
            if (e.target === noticeModal) noticeModal.classList.remove('active')
        })

        requestFeatureBtn?.addEventListener('click', () => {
            (window as any).electron.ipcRenderer.invoke('open-external', 'https://github.com/BlessedDayss/SharkyParser/issues/new')
            noticeModal?.classList.remove('active')
        })

        // External Links
        const githubBtn = document.getElementById('github-btn')
        const starBtn = document.getElementById('star-btn')

        githubBtn?.addEventListener('click', () => {
            ; (window as any).electron.ipcRenderer.invoke('open-external', 'https://github.com/BlessedDayss/SharkyParser/issues/new')
        })

        starBtn?.addEventListener('click', () => {
            ; (window as any).electron.ipcRenderer.invoke('open-external', 'https://github.com/BlessedDayss/SharkyParser')
        })
    }

    private async checkBackend() {
        const statusEl = document.getElementById('backend-status')
        const isReady = await (window as any).electron.ipcRenderer.invoke('check-csharp-backend')
        if (!isReady) {
            if (statusEl) {
                statusEl.textContent = 'Not Found'
                statusEl.style.color = 'var(--error)'
            }
            alert('SharkyParser Backend is not available! Please rebuild project.')
        } else {
            if (statusEl) {
                statusEl.textContent = 'Active'
                statusEl.style.color = 'var(--success)'
            }
        }
    }
}
