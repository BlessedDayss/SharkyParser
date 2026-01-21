import { LogEntry } from '../core/App'

export class LogModal {
    private modalOverlay: HTMLElement | null
    private modalDetails: HTMLElement | null
    private closeModal: HTMLElement | null

    constructor() {
        this.modalOverlay = document.getElementById('modal-overlay')
        this.modalDetails = document.getElementById('modal-details')
        this.closeModal = document.getElementById('close-modal')
    }

    public init() {
        if (this.closeModal) {
            this.closeModal.onclick = () => this.modalOverlay?.classList.remove('active')
        }
        window.addEventListener('show-log-details', (e: any) => this.show(e.detail))
    }

    public show(entry: LogEntry) {
        if (!this.modalDetails || !this.modalOverlay) return

        const hasStackTrace = !!(entry.stackTrace && entry.stackTrace.trim().length > 0)
        this.modalDetails.innerHTML = this.getModalHtml(entry, hasStackTrace)

        if (hasStackTrace) {
            this.setupTabs()
        }

        this.modalOverlay.classList.add('active')
    }

    private getModalHtml(entry: LogEntry, hasStackTrace: boolean): string {
        return `
            <div class="detail-row">
                <span class="detail-label">Level</span>
                <span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">Time</span>
                <span>${this.formatTimestamp(entry.timestamp)}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">Line</span>
                <span>#${entry.lineNumber || 'N/A'}</span>
            </div>
            
            ${hasStackTrace ? `
            <div class="modal-tabs">
                <button class="modal-tab active" data-tab="message">Message</button>
                <button class="modal-tab" data-tab="stacktrace">Stack Trace</button>
            </div>
            <div class="modal-tab-content" id="tab-message">
                <div class="code-block">${this.escapeHtml(entry.message)}</div>
            </div>
            <div class="modal-tab-content" id="tab-stacktrace" style="display: none;">
                <div class="code-block">${this.escapeHtml(entry.stackTrace || '')}</div>
            </div>
            ` : `
            <div class="detail-row">
                <span class="detail-label">Message</span>
                <div class="code-block">${this.escapeHtml(entry.message)}</div>
            </div>
            `}
        `
    }

    private setupTabs() {
        const tabs = this.modalDetails?.querySelectorAll('.modal-tab')
        tabs?.forEach(tab => {
            tab.addEventListener('click', () => {
                const tabName = (tab as HTMLElement).dataset.tab
                tabs.forEach(t => t.classList.remove('active'))
                tab.classList.add('active')

                const contents = this.modalDetails?.querySelectorAll('.modal-tab-content')
                contents?.forEach(content => {
                    (content as HTMLElement).style.display = content.id === `tab-${tabName}` ? 'block' : 'none'
                })
            })
        })
    }

    private formatTimestamp(timestamp: string): string {
        const date = new Date(timestamp)
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ` +
            `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}`
    }

    private escapeHtml(t: string): string {
        const d = document.createElement('div')
        d.textContent = t
        return d.innerHTML
    }
}
