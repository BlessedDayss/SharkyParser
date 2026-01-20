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

interface UpdateInfo {
    available: boolean
    version?: string
    downloadUrl?: string
}

let allEntries: LogEntry[] = []
let currentFilePath: string | null = null
let selectedLogType: string = 'Installation'

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

const navLogs = document.getElementById('nav-logs') as HTMLDivElement
const navAnalytics = document.getElementById('nav-analytics') as HTMLDivElement
const navChangelog = document.getElementById('nav-changelog') as HTMLDivElement
const navSettings = document.getElementById('nav-settings') as HTMLDivElement
const mainTitle = document.querySelector('.header-row h2') as HTMLHeadingElement

const logViewer = document.querySelector('.log-viewer-container') as HTMLDivElement
const statsGrid = document.querySelector('.stats-grid') as HTMLDivElement
const analysisDashboard = document.getElementById('analysis-dashboard') as HTMLDivElement
const changelogDashboard = document.getElementById('changelog-dashboard') as HTMLDivElement
const settingsDashboard = document.getElementById('settings-dashboard') as HTMLDivElement
const logTypeSection = document.getElementById('log-type-section') as HTMLDivElement

const healthPulse = document.getElementById('health-pulse') as HTMLDivElement
const healthText = document.getElementById('health-text') as HTMLDivElement
const distributionBars = document.getElementById('distribution-bars') as HTMLDivElement
const topSourcesList = document.getElementById('top-sources-list') as HTMLDivElement

const themeToggle = document.getElementById('theme-toggle') as HTMLInputElement
const themeLabel = document.getElementById('theme-label') as HTMLSpanElement
const checkUpdateBtn = document.getElementById('check-update-btn') as HTMLButtonElement
const updateStatus = document.getElementById('update-status') as HTMLDivElement

window.addEventListener('DOMContentLoaded', async () => {
    try {
        const isReady = await (window as any).electron.ipcRenderer.invoke('check-csharp-backend')
        backendStatus.textContent = isReady ? 'Operational ✅' : 'Offline ❌'
        backendStatus.style.color = isReady ? 'var(--success)' : 'var(--error)'
    } catch (err) {
        backendStatus.textContent = 'Error ⚠️'
    }

    try {
        const version = await (window as any).electron.ipcRenderer.invoke('get-app-version')
        const versionEl = document.getElementById('current-version')
        if (versionEl) versionEl.textContent = `v${version}`
    } catch (err) {
        console.error('Failed to load version:', err)
    }

    const savedTheme = localStorage.getItem('theme') || 'dark'
    applyTheme(savedTheme)

    setupZoom()
    setupLogTypeSelector()
    setupStatCardFilters()
    loadChangelog()
})

let zoomTimeout: any = null
let lastZoomTime = 0
const ZOOM_THROTTLE = 150 // ms

function applyZoom(zoom: number, immediate = false) {
    const factor = zoom / 100
    const zoomValue = document.getElementById('zoom-value')

    if (zoomValue) zoomValue.textContent = `${zoom}%`

    if (immediate) {
        if (zoomTimeout) clearTimeout(zoomTimeout)
            ; (window as any).electron.ipcRenderer.invoke('set-zoom', factor)
        return
    }

    if (zoomTimeout) clearTimeout(zoomTimeout)
    zoomTimeout = setTimeout(() => {
        ; (window as any).electron.ipcRenderer.invoke('set-zoom', factor)
    }, 50)
}

function setupZoom() {
    const zoomInBtn = document.getElementById('zoom-in-btn') as HTMLButtonElement
    const zoomOutBtn = document.getElementById('zoom-out-btn') as HTMLButtonElement
    const savedZoom = parseInt(localStorage.getItem('zoomLevel') || '100')

    applyZoom(savedZoom, true)

    const changeZoom = (delta: number) => {
        const now = Date.now()
        if (now - lastZoomTime < ZOOM_THROTTLE) return

        lastZoomTime = now
        const currentZoom = parseInt(localStorage.getItem('zoomLevel') || '100')
        const newZoom = Math.min(200, Math.max(50, currentZoom + delta))

        localStorage.setItem('zoomLevel', newZoom.toString())
        applyZoom(newZoom)
    }

    zoomInBtn?.addEventListener('click', () => changeZoom(10))
    zoomOutBtn?.addEventListener('click', () => changeZoom(-10))

    // Mouse wheel zoom (Ctrl + Wheel)
    window.addEventListener('wheel', (e) => {
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault()
            changeZoom(e.deltaY > 0 ? -10 : 10)
        }
    }, { passive: false })

    // Shortcuts (Ctrl + Plus/Minus/Zero)
    window.addEventListener('keydown', (e) => {
        if (e.ctrlKey || e.metaKey) {
            if (e.key === '=' || e.key === '+') {
                e.preventDefault()
                changeZoom(10)
            } else if (e.key === '-') {
                e.preventDefault()
                changeZoom(-10)
            } else if (e.key === '0') {
                e.preventDefault()
                localStorage.setItem('zoomLevel', '100')
                applyZoom(100, true)
            }
        }
    })
}

async function loadChangelog() {
    try {
        const changelogPath = await (window as any).electron.ipcRenderer.invoke('get-changelog-path')
        const response = await fetch(`file:///${changelogPath}`)
        const markdown = await response.text()

        const changelogContent = document.getElementById('changelog-content')
        if (changelogContent) {
            changelogContent.innerHTML = renderMarkdown(markdown)
        }
    } catch (error) {
        console.error('Failed to load changelog:', error)
        const changelogContent = document.getElementById('changelog-content')
        if (changelogContent) {
            changelogContent.innerHTML = '<p style="color: var(--error);">Failed to load changelog</p>'
        }
    }
}

function renderMarkdown(markdown: string): string {
    let html = markdown
        .replace(/^### (.+)$/gm, '<h4 style="color: var(--accent-primary); margin-top: 16px; margin-bottom: 8px;">$1</h4>')
        .replace(/^## (.+)$/gm, '<h3 style="color: var(--text-main); font-weight: 600; margin-top: 24px; margin-bottom: 12px; border-bottom: 1px solid var(--border-glass); padding-bottom: 8px;">$1</h3>')
        .replace(/^# (.+)$/gm, '<h2 style="color: var(--accent-primary); font-size: 24px; margin-bottom: 16px;">$1</h2>')
        .replace(/^\- (.+)$/gm, '<li style="margin-left: 20px; margin-bottom: 4px;">$1</li>')
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/^---$/gm, '<hr style="border: none; border-top: 1px solid var(--border-glass); margin: 24px 0;">')
        .replace(/\n\n/g, '</p><p style="margin-top: 8px;">')

    return `<p style="margin-top: 8px;">${html}</p>`
}

function setupStatCardFilters() {
    const statCards = document.querySelectorAll('.stat-card.clickable')

    statCards.forEach(card => {
        card.addEventListener('click', () => {
            const filterValue = (card as HTMLElement).dataset.filter || 'ALL'
            levelFilter.value = filterValue
            applyFilters()
        })
    })
}

function setupLogTypeSelector() {
    const logTypeButtons = document.querySelectorAll('.log-type-btn')

    logTypeButtons.forEach(btn => {
        if (!btn.classList.contains('disabled')) {
            btn.addEventListener('click', () => {
                logTypeButtons.forEach(b => b.classList.remove('active'))
                btn.classList.add('active')
                selectedLogType = (btn as HTMLElement).dataset.type || 'Installation'
                console.log('Selected log type:', selectedLogType)
            })
        }
    })
}

themeToggle.addEventListener('change', () => {
    const newTheme = themeToggle.checked ? 'light' : 'dark'
    applyTheme(newTheme)
    localStorage.setItem('theme', newTheme)
})

function applyTheme(theme: string) {
    if (theme === 'light') {
        document.body.classList.add('light-theme')
        themeToggle.checked = true
        themeLabel.textContent = 'Light'
    } else {
        document.body.classList.remove('light-theme')
        themeToggle.checked = false
        themeLabel.textContent = 'Dark'
    }
}

window.addEventListener('DOMContentLoaded', () => {
    const ipc = (window as any).electron.ipcRenderer

    ipc.on('update-available', (_: any, version: string) => {
        updateStatus.style.display = 'block'
        updateStatus.innerHTML = `
            <div style="color: var(--accent-primary);">⬇️ Downloading update ${version}...</div>
            <div class="progress-bar-bg" style="margin-top: 8px; height: 6px;">
                <div class="progress-bar-fill" id="update-progress" style="width: 0%"></div>
            </div>
        `
    })

    ipc.on('download-progress', (_: any, percent: number) => {
        const bar = document.getElementById('update-progress')
        if (bar) bar.style.width = `${percent}%`
    })

    ipc.on('update-downloaded', (_: any, version: string) => {
        updateStatus.innerHTML = `
            <div style="color: var(--success); font-weight: 600;">✅ Update ${version} Ready!</div>
            <button class="update-btn" id="restart-btn" style="margin-top: 8px; width: 100%; justify-content: center;">
                <span>🚀</span> Restart to Install
            </button>
        `
        document.getElementById('restart-btn')?.addEventListener('click', () => {
            ipc.invoke('install-update')
        })
    })

    ipc.on('update-error', (_: any, err: string) => {
        updateStatus.style.display = 'block'
        updateStatus.innerHTML = `<div style="color: var(--error);">❌ Update Error: ${err}</div>`
    })

    ipc.on('update-not-available', () => {
        updateStatus.style.display = 'block'
        updateStatus.innerHTML = '<div style="color: var(--success);">✅ You are on the latest version</div>'
        setTimeout(() => { updateStatus.style.display = 'none' }, 5000)
    })
})

checkUpdateBtn.addEventListener('click', async () => {
    checkUpdateBtn.disabled = true
    checkUpdateBtn.innerHTML = '<span>⏳</span> Checking...'

    try {
        const result = await (window as any).electron.ipcRenderer.invoke('check-for-updates')

        if (result.fallbackStarted) {
            console.log('Fallback update mechanism triggered')
        } else if (!result.available) {
            updateStatus.style.display = 'block'
            updateStatus.innerHTML = '<div style="color: var(--success);">✅ You are on the latest version</div>'
            setTimeout(() => { updateStatus.style.display = 'none' }, 3000)
        }
    } finally {
        checkUpdateBtn.disabled = false
        checkUpdateBtn.innerHTML = '<span>🔄</span> Check for Updates'
    }
})

navLogs.addEventListener('click', () => switchView('logs'))
navAnalytics.addEventListener('click', () => switchView('analytics'))
navChangelog.addEventListener('click', () => switchView('changelog'))
navSettings.addEventListener('click', () => switchView('settings'))

async function switchView(view: 'logs' | 'analytics' | 'changelog' | 'settings') {
    navLogs.classList.remove('active')
    navAnalytics.classList.remove('active')
    navChangelog.classList.remove('active')
    navSettings.classList.remove('active')

    logViewer.style.display = 'none'
    statsGrid.style.display = 'none'
    analysisDashboard.style.display = 'none'
    changelogDashboard.style.display = 'none'
    settingsDashboard.style.display = 'none'
    logTypeSection.style.display = 'none'

    if (view === 'analytics' && !currentFilePath) {
        alert('Please open a log file first!')
        switchView('logs')
        return
    }

    if (view === 'logs') {
        navLogs.classList.add('active')
        mainTitle.textContent = 'Dashboard'
        logViewer.style.display = 'flex'
        statsGrid.style.display = 'grid'
        logTypeSection.style.display = 'block'
    } else if (view === 'analytics') {
        navAnalytics.classList.add('active')
        mainTitle.textContent = 'System Analysis'
        analysisDashboard.style.display = 'flex'
        try {
            const analysis: AnalysisResult = await (window as any).electron.ipcRenderer.invoke('analyze-log-csharp', currentFilePath, selectedLogType)
            renderDashboard(analysis)
        } catch (err) {
            console.error('Analysis failed', err)
        }
    } else if (view === 'changelog') {
        navChangelog.classList.add('active')
        mainTitle.textContent = 'Changelog'
        changelogDashboard.style.display = 'flex'
    } else if (view === 'settings') {
        navSettings.classList.add('active')
        mainTitle.textContent = 'Settings'
        settingsDashboard.style.display = 'flex'
    }
}

function renderDashboard(data: AnalysisResult) {
    const isHealthy = data.status === 'HEALTHY'
    healthPulse.className = `status-pulse ${isHealthy ? 'healthy' : 'unhealthy'}`
    healthPulse.textContent = isHealthy ? '✓' : '⚠'
    healthText.textContent = isHealthy ? 'System Healthy' : 'Action Required'
    healthText.style.color = isHealthy ? 'var(--success)' : 'var(--error)'

    const total = data.total || 1
    distributionBars.innerHTML = `
        ${renderProgressBar('Errors', data.errors, total, 'var(--error)')}
        ${renderProgressBar('Warnings', data.warnings, total, 'var(--warning)')}
        ${renderProgressBar('Informational', data.info, total, 'var(--info)')}
    `

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

selectFileBtn.addEventListener('click', async () => {
    try {
        const result: string = await (window as any).electron.ipcRenderer.invoke('select-file')
        if (result) {
            currentFilePath = result
            fileInfo.textContent = result.split('/').pop() || result
            selectFileBtn.innerHTML = '<span>⏳</span> Parsing...'

            const data: ParseResult = await (window as any).electron.ipcRenderer.invoke('parse-log-csharp', result, selectedLogType)
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
    entries.slice(0, 10000).forEach(entry => {
        const row = document.createElement('tr')
        row.className = 'log-row'
        row.onclick = () => showDetails(entry)
        row.innerHTML = `
            <td class="timestamp">${formatTimestamp(entry.timestamp)}</td>
            <td><span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span></td>
            <td class="message">${escapeHtml(entry.message)}</td>
        `
        logBody.appendChild(row)
    })
}

function showDetails(entry: LogEntry) {
    const hasStackTrace = entry.stackTrace && entry.stackTrace.trim().length > 0

    modalDetails.innerHTML = `
        <div class="detail-row"><span class="detail-label">Level</span><span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span></div>
        <div class="detail-row"><span class="detail-label">Time</span><span>${formatTimestamp(entry.timestamp)}</span></div>
        <div class="detail-row"><span class="detail-label">Line</span><span>#${entry.lineNumber || 'N/A'}</span></div>
        
        ${hasStackTrace ? `
        <div class="modal-tabs">
            <button class="modal-tab active" data-tab="message">Message</button>
            <button class="modal-tab" data-tab="stacktrace">Stack Trace</button>
        </div>
        
        <div class="modal-tab-content" id="tab-message">
            <div class="code-block">${escapeHtml(entry.message)}</div>
        </div>
        
        <div class="modal-tab-content" id="tab-stacktrace" style="display: none;">
            <div class="code-block">${escapeHtml(entry.stackTrace || '')}</div>
        </div>
        ` : `
        <div class="detail-row"><span class="detail-label">Message</span><div class="code-block">${escapeHtml(entry.message)}</div></div>
        `}
    `

    if (hasStackTrace) {
        const tabs = modalDetails.querySelectorAll('.modal-tab')
        tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                const tabName = (tab as HTMLElement).dataset.tab

                tabs.forEach(t => t.classList.remove('active'))
                tab.classList.add('active')

                const contents = modalDetails.querySelectorAll('.modal-tab-content')
                contents.forEach(content => {
                    if (content.id === `tab-${tabName}`) {
                        (content as HTMLElement).style.display = 'block'
                    } else {
                        (content as HTMLElement).style.display = 'none'
                    }
                })
            })
        })
    }

    modalOverlay.classList.add('active')
}


closeModal.onclick = () => modalOverlay.classList.remove('active')

function formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp)
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    const seconds = String(date.getSeconds()).padStart(2, '0')
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
}

function escapeHtml(t: string) { const d = document.createElement('div'); d.textContent = t; return d.innerHTML; }
