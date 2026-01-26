export class ChartService {
    private healthPulse: HTMLDivElement | null
    private healthText: HTMLDivElement | null
    private distContainer: HTMLElement | null
    private topSourcesList: HTMLDivElement | null
    private timeline: HTMLElement | null

    constructor() {
        this.healthPulse = document.getElementById('health-pulse') as HTMLDivElement
        this.healthText = document.getElementById('health-text') as HTMLDivElement
        this.distContainer = document.getElementById('distribution-container')
        this.topSourcesList = document.getElementById('top-sources-list') as HTMLDivElement
        this.timeline = document.getElementById('chart-timeline')
    }

    public init() {
        window.addEventListener('analytics-view-active', () => {
            const currentFile = (window as any).app.logService.currentFilePath
            const logType = (window as any).app.logService.selectedLogType
            if (currentFile) {
                this.refreshCharts(currentFile, logType)
            }
        })
        window.addEventListener('logs-parsed', (e: any) => {
            this.refreshCharts(e.detail.filePath, e.detail.logType)
        })
    }

    private async refreshCharts(filePath: string, logType: string) {
        const data = await (window as any).electron.ipcRenderer.invoke('analyze-log-csharp', filePath, logType)
        this.renderDashboard(data)
        this.renderVolumeChart()
    }

    private renderDashboard(data: any) {
        const isHealthy = data.status === 'HEALTHY'
        if (this.healthPulse) {
            this.healthPulse.className = `status-pulse ${isHealthy ? 'healthy' : 'unhealthy'}`
            this.healthPulse.textContent = isHealthy ? '✓' : '⚠'
        }
        if (this.healthText) {
            this.healthText.textContent = isHealthy ? 'System Healthy' : 'Action Required'
            this.healthText.style.color = isHealthy ? 'var(--success)' : 'var(--error)'
        }

        const total = data.total || 1
        const infoPercent = Math.round((data.info / total) * 100)
        const warnPercent = Math.round((data.warnings / total) * 100)
        const errorPercent = 100 - infoPercent - warnPercent

        if (this.distContainer) {
            this.distContainer.innerHTML = `
                <div class="pie-chart" style="width: 160px; height: 160px; --info-end: ${infoPercent}%; --warn-end: ${infoPercent + warnPercent}%">
                    <div class="pie-chart-center">
                        <span style="font-size: 24px; font-weight: 700; color: white;">${this.formatNumber(total)}</span>
                        <span style="font-size: 10px; color: var(--text-dim); text-transform: uppercase; letter-spacing: 1px;">Logs</span>
                    </div>
                </div>
                <div style="display: flex; flex-direction: column; gap: 12px; font-size: 13px;">
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <div style="width: 12px; height: 12px; border-radius: 3px; background: var(--chart-info);"></div>
                        <span style="color: var(--text-main);">Info (${infoPercent}%)</span>
                    </div>
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <div style="width: 12px; height: 12px; border-radius: 3px; background: var(--chart-warn);"></div>
                        <span style="color: var(--text-main);">Warning (${warnPercent}%)</span>
                    </div>
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <div style="width: 12px; height: 12px; border-radius: 3px; background: var(--chart-error); box-shadow: 0 0 10px rgba(255, 51, 51, 0.4);"></div>
                        <span style="color: var(--text-main); font-weight: 500;">Error (${errorPercent}%)</span>
                    </div>
                </div>
            `
        }

        if (this.topSourcesList) {
            this.topSourcesList.innerHTML = ''
            if (data.extendedData) {
                data.extendedData.split('|').forEach(sourceStr => {
                    if (!sourceStr.includes(':')) return
                    const [name, count] = sourceStr.split(':')
                    const item = document.createElement('div')
                    item.className = 'source-item'
                    item.innerHTML = `
                        <span class="source-name">${name}</span>
                        <span class="source-count">${this.formatNumber(parseInt(count))} logs</span>
                    `
                    this.topSourcesList?.appendChild(item)
                })
            } else {
                this.topSourcesList.innerHTML = '<div style="color: var(--text-dim);">No source data available</div>'
            }
        }
    }

    private renderVolumeChart() {
        const volumePath = document.getElementById('volume-path')
        const volumeFill = document.getElementById('volume-fill')
        const errorPath = document.getElementById('error-path')
        const errorFill = document.getElementById('error-fill')
        const pulse = document.getElementById('chart-pulse')

        const allEntries = (window as any).app.logService.allEntries
        if (!this.timeline || !allEntries || allEntries.length === 0) return

        const sorted = [...allEntries].sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime())
        const start = new Date(sorted[0].timestamp).getTime()
        const end = new Date(sorted[sorted.length - 1].timestamp).getTime()
        const finalEnd = isNaN(end) ? Date.now() : end
        const finalStart = isNaN(start) ? Date.now() - 3600000 : start
        const duration = Math.max(1, finalEnd - finalStart)

        const bucketsCount = 40
        const totalBuckets = new Array(bucketsCount).fill(0)
        const errorBuckets = new Array(bucketsCount).fill(0)

        sorted.forEach(entry => {
            const time = new Date(entry.timestamp).getTime()
            if (isNaN(time)) return
            const bucketIndex = Math.min(bucketsCount - 1, Math.floor(((time - finalStart) / duration) * bucketsCount))
            if (bucketIndex < 0) return
            totalBuckets[bucketIndex]++
            if (entry.level.toUpperCase() === 'ERROR') errorBuckets[bucketIndex]++
        })

        const maxVal = Math.max(...totalBuckets, 1)
        const getY = (val: number) => 180 - (val / maxVal) * 160
        const getX = (i: number) => (i / (bucketsCount - 1)) * 1000

        const createPath = (data: number[]) => {
            let d = `M 0 ${getY(data[0])}`
            for (let i = 0; i < data.length - 1; i++) {
                const x1 = getX(i); const y1 = getY(data[i])
                const x2 = getX(i + 1); const y2 = getY(data[i + 1])
                const cx = (x1 + x2) / 2
                d += ` C ${cx} ${y1}, ${cx} ${y2}, ${x2} ${y2}`
            }
            return d
        }

        if (volumePath) volumePath.setAttribute('d', createPath(totalBuckets))
        if (volumeFill) volumeFill.setAttribute('d', createPath(totalBuckets) + ` L 1000 200 L 0 200 Z`)
        if (errorPath) errorPath.setAttribute('d', createPath(errorBuckets))
        if (errorFill) errorFill.setAttribute('d', createPath(errorBuckets) + ` L 1000 200 L 0 200 Z`)

        if (pulse) {
            pulse.style.display = 'block'
            pulse.setAttribute('cx', getX(bucketsCount - 1).toString())
            pulse.setAttribute('cy', getY(totalBuckets[bucketsCount - 1]).toString())
        }

        this.timeline.innerHTML = ''
        for (let i = 0; i <= 6; i++) {
            const t = new Date(finalStart + (duration * (i / 6)))
            const span = document.createElement('span')
            span.textContent = i === 6 ? 'Now' : `${String(t.getHours()).padStart(2, '0')}:${String(t.getMinutes()).padStart(2, '0')}`
            this.timeline.appendChild(span)
        }
    }

    private formatNumber(num: number): string {
        if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
        if (num >= 1000) return (num / 1000).toFixed(1) + 'K'
        return num.toString()
    }
}
