import { LogEntry } from '../core/App'

export class LogTable {
    private logBody: HTMLElement | null

    constructor() {
        this.logBody = document.getElementById('log-body')
    }

    public init() {
        window.addEventListener('logs-filtered', (e: any) => this.render(e.detail))
    }

    public render(entries: LogEntry[]) {
        if (!this.logBody) return
        this.logBody.innerHTML = ''

        entries.slice(0, 10000).forEach(entry => {
            const row = document.createElement('tr')
            row.className = 'log-row'
            row.onclick = () => window.dispatchEvent(new CustomEvent('show-log-details', { detail: entry }))

            row.innerHTML = `
                <td class="timestamp">${this.formatTimestamp(entry.timestamp)}</td>
                <td><span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span></td>
                <td class="message">${this.escapeHtml(entry.message)}</td>
            `
            this.logBody?.appendChild(row)
        })
    }

    private formatTimestamp(timestamp: string): string {
        const date = new Date(timestamp)
        const year = date.getFullYear()
        const month = String(date.getMonth() + 1).padStart(2, '0')
        const day = String(date.getDate()).padStart(2, '0')
        const hours = String(date.getHours()).padStart(2, '0')
        const minutes = String(date.getMinutes()).padStart(2, '0')
        const seconds = String(date.getSeconds()).padStart(2, '0')
        return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
    }

    private escapeHtml(t: string): string {
        const d = document.createElement('div')
        d.textContent = t
        return d.innerHTML
    }
}
