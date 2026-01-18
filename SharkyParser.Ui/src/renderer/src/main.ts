import './style.css'

interface LogEntry {
    timestamp: string
    level: string
    message: string
    source?: string
    stackTrace?: string
    lineNumber?: number
    filePath?: string
    rawData?: string
    id: number
}

interface ParseResult {
    entries: LogEntry[]
    statistics: {
        total: number
        errors: number
        warnings: number
        info: number
        debug: number
    }
}

interface AnalysisResult {
    total: number
    errors: number
    warnings: number
    info: number
    debug: number
    status: string
    extendedData: string
}

// State
let allEntries: LogEntry[] = []
let currentFilePath: string | null = null

// DOM Elements
const selectFileBtn = document.getElementById('select-file-btn') as HTMLButtonElement
const logBody = document.getElementById('log-body') as HTMLTableSectionElement
const statTotal = document.getElementById('stat-total') as HTMLSpanElement
const statErrors = document.getElementById('stat-errors') as HTMLSpanElement
const statWarnings = document.getElementById('stat-warnings') as HTMLSpanElement
const statInfo = document.getElementById('stat-info') as HTMLSpanElement
const fileInfo = document.getElementById('file-info') as HTMLDivElement
const modalOverlay = document.getElementById('modal-overlay') as HTMLDivElement
const modalDetails = document.getElementById('modal-details') as HTMLDivElement
const closeModal = document.getElementById('close-modal') as HTMLButtonElement
const searchInput = document.getElementById('search-input') as HTMLInputElement
const levelFilter = document.getElementById('level-filter') as HTMLSelectElement
const backendStatus = document.getElementById('backend-status') as HTMLSpanElement

// Nav Elements
const navLogs = document.getElementById('nav-logs') as HTMLDivElement
const navAnalytics = document.getElementById('nav-analytics') as HTMLDivElement
const mainTitle = document.querySelector('.header-row h2') as HTMLHeadingElement

// Views
const logViewer = document.querySelector('.log-viewer-container') as HTMLDivElement
const statsGrid = document.querySelector('.stats-grid') as HTMLDivElement
const analysisDashboard = document.getElementById('analysis-dashboard') as HTMLDivElement

// Dashboard Elements
const healthPulse = document.getElementById('health-pulse') as HTMLDivElement
const healthText = document.getElementById('health-text') as HTMLDivElement
const distributionBars = document.getElementById('distribution-bars') as HTMLDivElement
const topSourcesList = document.getElementById('top-sources-list') as HTMLDivElement

// Initialize - Backend status already checked before window showed
window.addEventListener('DOMContentLoaded', async () => {
    try {
        const isReady = await (window as any).electron.ipcRenderer.invoke('check-csharp-backend')
        backendStatus.textContent = isReady ? 'Operational ✅' : 'Offline ❌'
        backendStatus.style.color = isReady ? 'var(--success)' : 'var(--error)'
    } catch (err) {
        backendStatus.textContent = 'Error ⚠️'
    }
})

// Navigation
navLogs.addEventListener('click', () => switchView('logs'))
navAnalytics.addEventListener('click', () => switchView('analytics'))

async function switchView(view: 'logs' | 'analytics') {
    if (view === 'analytics' && !currentFilePath) {
        alert('Please open a log file first!')
        return
    }

    if (view === 'logs') {
        navLogs.classList.add('active')
        navAnalytics.classList.remove('active')
        mainTitle.textContent = 'Dashboard'
        logViewer.style.display = 'flex'
        statsGrid.style.display = 'grid'
        analysisDashboard.style.display = 'none'
    } else {
        navAnalytics.classList.add('active')
        navLogs.classList.remove('active')
        mainTitle.textContent = 'System Analysis'
        logViewer.style.display = 'none'
        statsGrid.style.display = 'none'
        analysisDashboard.style.display = 'flex'

        try {
            const analysis: AnalysisResult = await (window as any).electron.ipcRenderer.invoke('analyze-log-csharp', currentFilePath)
            renderDashboard(analysis)
        } catch (err) {
            console.error('Analysis failed', err)
        }
    }
}

function renderDashboard(data: AnalysisResult) {
    // Health Update
    const isHealthy = data.status === 'HEALTHY'
    healthPulse.className = `status-pulse ${isHealthy ? 'healthy' : 'unhealthy'}`
    healthPulse.textContent = isHealthy ? '✓' : '⚠'
    healthText.textContent = isHealthy ? 'System Healthy' : 'Action Required'
    healthText.style.color = isHealthy ? 'var(--success)' : 'var(--error)'

    // Distribution Bars
    const total = data.total || 1
    distributionBars.innerHTML = `
        ${renderProgressBar('Errors', data.errors, total, 'var(--error)')}
        ${renderProgressBar('Warnings', data.warnings, total, 'var(--warning)')}
        ${renderProgressBar('Informational', data.info, total, 'var(--info)')}
    `

    // Top Sources
    topSourcesList.innerHTML = ''
    if (data.extendedData) {
        data.extendedData.split('|').forEach(sourceStr => {
            const [name, count] = sourceStr.split(':')
            const item = document.createElement('div')
            item.className = 'source-item'
            item.innerHTML = `
                <span class="source-name">${name}</span>
                <span class="source-count">${count} logs</span>
            `
            topSourcesList.appendChild(item)
        })
    } else {
        topSourcesList.innerHTML = '<div style="color: var(--text-dim);">No source data available</div>'
    }
}

function renderProgressBar(label: string, value: number, total: number, color: string) {
    const percent = Math.round((value / total) * 100)
    return `
        <div class="progress-item">
            <div class="progress-label"><span>${label}</span><span>${percent}%</span></div>
            <div class="progress-bar-bg"><div class="progress-bar-fill" style="width: ${percent}%; background: ${color}"></div></div>
        </div>
    `
}

// File Selection
selectFileBtn.addEventListener('click', async () => {
    try {
        const result: string = await (window as any).electron.ipcRenderer.invoke('select-file')
        if (result) {
            currentFilePath = result
            fileInfo.textContent = result.split('/').pop() || result
            selectFileBtn.innerHTML = '<span>⏳</span> Parsing...'

            const data: ParseResult = await (window as any).electron.ipcRenderer.invoke('parse-log-csharp', result)
            allEntries = data.entries.map((e, idx) => ({ ...e, id: idx }))
            updateStats(data.statistics)
            applyFilters()
            if (navAnalytics.classList.contains('active')) switchView('analytics')
        }
    } catch (error: any) {
        alert('Error: ' + error.message)
    } finally {
        selectFileBtn.innerHTML = '<span>🚀</span> Open Log File'
    }
})

// Filters & Rendering
searchInput.addEventListener('input', () => applyFilters())
levelFilter.addEventListener('change', () => applyFilters())

function applyFilters() {
    const searchTerm = searchInput.value.toLowerCase()
    const level = levelFilter.value
    const filtered = allEntries.filter(entry => {
        const matchesSearch = entry.message.toLowerCase().includes(searchTerm)
        const matchesLevel = level === 'ALL' || entry.level.toUpperCase() === level.toUpperCase()
        return matchesSearch && matchesLevel
    })
    renderTable(filtered)
}

function updateStats(stats: any) {
    statTotal.textContent = stats.total.toString()
    statErrors.textContent = stats.errors.toString()
    statWarnings.textContent = stats.warnings.toString()
    statInfo.textContent = stats.info.toString()
}

function renderTable(entries: LogEntry[]) {
    logBody.innerHTML = ''
    entries.slice(0, 500).forEach(entry => {
        const row = document.createElement('tr')
        row.className = 'log-row'
        row.onclick = () => showDetails(entry)
        row.innerHTML = `
            <td class="timestamp">${new Date(entry.timestamp).toLocaleTimeString()}</td>
            <td><span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span></td>
            <td class="message">${escapeHtml(entry.message)}</td>
        `
        logBody.appendChild(row)
    })
}

function showDetails(entry: LogEntry) {
    modalDetails.innerHTML = `
        <div class="detail-row"><span class="detail-label">Level</span><span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span></div>
        <div class="detail-row"><span class="detail-label">Time</span><span>${entry.timestamp}</span></div>
        <div class="detail-row"><span class="detail-label">Message</span><div class="code-block">${escapeHtml(entry.message)}</div></div>
        ${entry.stackTrace ? `<div class="detail-row"><span class="detail-label">Stack</span><div class="code-block">${escapeHtml(entry.stackTrace)}</div></div>` : ''}
    `
    modalOverlay.classList.add('active')
}

closeModal.onclick = () => modalOverlay.classList.remove('active')
function escapeHtml(t: string) { const d = document.createElement('div'); d.textContent = t; return d.innerHTML; }
