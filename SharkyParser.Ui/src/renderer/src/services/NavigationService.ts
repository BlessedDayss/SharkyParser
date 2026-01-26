export class NavigationService {
    private currentView: string = 'logs'

    constructor() { }

    public init() {
        // Use event delegation for better reliability
        const sidebar = document.querySelector('.sidebar-nav')
        sidebar?.addEventListener('click', (e) => {
            const target = (e.target as HTMLElement).closest('.nav-item') as HTMLElement
            if (target && target.id) {
                const viewId = target.id.replace('nav-', '')
                if (viewId) {
                    this.switchView(viewId)
                }
            }
        })

        // Analyze Modal Listeners
        const analyzeModal = document.getElementById('analyze-modal')
        const closeAnalyzeBtn = document.getElementById('close-analyze-modal')
        const goSelectFileBtn = document.getElementById('go-select-file-btn')

        closeAnalyzeBtn?.addEventListener('click', () => {
            analyzeModal?.classList.remove('active')
        })

        goSelectFileBtn?.addEventListener('click', () => {
            analyzeModal?.classList.remove('active')
            this.switchView('logs')
            setTimeout(() => {
                const app = (window as any).app
                app.logService.triggerFilePicker()
            }, 100)
        })

        // Force initial state
        this.syncUI('logs')
    }

    public switchView(view: string) {
        // Analytics Guard
        if (view === 'analytics') {
            const app = (window as any).app
            if (!app.logService.isFileSelected()) {
                document.getElementById('analyze-modal')?.classList.add('active')
                // Force sync UI back to current view just in case
                this.syncUI(this.currentView)
                return
            }
        }

        // Prevent double trigger (except for changelog update)
        if (this.currentView === view && view !== 'changelog') return
        this.currentView = view

        console.log(`Switching view to: ${view}`)

        // 1. Update Sidebar UI
        this.syncUI(view)

        // 2. Clear all visible sections
        const allSections = document.querySelectorAll('.view-section')
        allSections.forEach(sec => {
            (sec as HTMLElement).style.display = 'none'
            sec.classList.remove('active')
        })

        // 3. Show target section
        const targetSection = document.getElementById(`${view}-dashboard`)
        if (targetSection) {
            targetSection.style.display = 'flex'
            targetSection.classList.add('active')
        }

        // 4. Run view-specific logic
        if (view === 'analytics') {
            window.dispatchEvent(new CustomEvent('analytics-view-active'))
        } else if (view === 'changelog') {
            this.loadChangelog()
        }
    }

    private syncUI(activeView: string) {
        // Identify ALL nav items in the sidebar
        const navItems = document.querySelectorAll('.sidebar-nav .nav-item')

        navItems.forEach(item => {
            const viewId = item.id.replace('nav-', '')
            if (viewId === activeView) {
                item.classList.add('active')
            } else {
                item.classList.remove('active')
            }
        })
    }

    private async loadChangelog() {
        try {
            const text = await (window as any).electron.ipcRenderer.invoke('read-changelog')
            if (text) {
                this.renderPremiumChangelog(text)
            }
        } catch (e) {
            console.error('Failed to load changelog:', e)
        }
    }

    private renderPremiumChangelog(text: string) {
        const container = document.getElementById('changelog-content')
        if (!container) return

        const segments = text.split(/##\s+Version\s+/i).filter(s => s.trim().length > 0)
        if (segments[0].toLowerCase().includes('# changelog') && !segments[0].toLowerCase().includes('version')) {
            segments.shift()
        }

        container.innerHTML = segments.map((content, idx) => {
            const lines = content.split(/\r?\n/)
            const header = lines[0].trim()

            const versionMatch = header.match(/^([\d\.]+)/)
            const dateMatch = header.match(/(\d{4}-\d{2}-\d{2})/)

            const versionNum = versionMatch ? versionMatch[1] : ''
            const dateStr = dateMatch ? dateMatch[0] : ''

            let versionTitle = header
                .replace(versionNum || '', '')
                .replace(dateStr || '', '')
                .replace(/[-\s]+/g, ' ')
                .trim()

            const categories: { name: string, items: string[] }[] = []
            let currentCat: { name: string, items: string[] } | null = null

            for (let i = 1; i < lines.length; i++) {
                const line = lines[i].trim()
                if (line.startsWith('### ')) {
                    currentCat = { name: line.substring(4).trim(), items: [] }
                    categories.push(currentCat)
                } else if ((line.startsWith('- ') || line.startsWith('* ')) && currentCat) {
                    const itemText = line.substring(2).trim()
                    if (itemText) currentCat.items.push(itemText)
                }
            }

            const displayTitle = versionTitle || (idx === 0 ? 'Latest Stable' : 'Point Release')

            return `
                <div class="relative mb-16 group">
                    <div class="absolute -left-[44.5px] top-6 size-2.5 rounded-full ${idx === 0 ? 'bg-primary shadow-neon-cyan' : 'bg-secondary shadow-[0_0_10px_#7000ff]'} ring-4 ring-background-dark"></div>
                    <div class="bg-surface-glass backdrop-blur-glass p-6 rounded-xl border border-white/10 group-hover:border-primary/30 transition-all duration-300 relative overflow-hidden">
                        <div class="absolute top-0 right-0 w-64 h-64 bg-primary/5 rounded-full blur-3xl -mr-32 -mt-32 pointer-events-none"></div>
                        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6 border-b border-white/5 pb-4">
                            <div class="flex items-center gap-4">
                                ${versionNum ? `<span class="px-3 py-1 rounded-full bg-primary/10 border border-primary/20 text-primary font-mono font-bold text-sm shadow-[0_0_10px_rgba(0,242,255,0.2)]">v${versionNum}</span>` : ''}
                                <h3 class="text-xl text-white font-display font-semibold">${displayTitle}</h3>
                            </div>
                            <span class="text-slate-500 font-mono text-sm flex items-center gap-2">
                                <span class="material-symbols-outlined text-[16px]">calendar_today</span> ${dateStr || 'Recent'}
                            </span>
                        </div>
                        <div class="grid grid-cols-1 md:grid-cols-3 gap-8 relative z-10">
                            ${categories.length > 0 ? categories.map(cat => this.renderCategory(cat.name, cat.items)).join('') : '<p class="text-slate-500 italic">No detailed notes for this version.</p>'}
                        </div>
                    </div>
                </div>
            `
        }).join('')
    }

    private renderCategory(name: string, items: string[]) {
        if (items.length === 0) return ''
        let icon = 'verified', colorClass = 'text-success'
        const lowerName = name.toLowerCase()
        if (lowerName.includes('added')) { icon = 'verified'; colorClass = 'text-success' }
        else if (lowerName.includes('improved') || lowerName.includes('update')) { icon = 'rocket_launch'; colorClass = 'text-primary' }
        else if (lowerName.includes('fixed') || lowerName.includes('bug')) { icon = 'build'; colorClass = 'text-warning' }
        else { icon = 'stars'; colorClass = 'text-secondary' }

        return `
            <div>
                <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4 flex items-center gap-2">
                    <span class="material-symbols-outlined text-sm ${colorClass}">${icon}</span> ${name}
                </h4>
                <ul class="space-y-3">
                    ${items.map(item => `
                        <li class="text-sm text-slate-300 flex items-start gap-2">
                            <span class="w-1.5 h-1.5 rounded-full bg-secondary mt-1.5 shrink-0"></span>
                            <span>${item}</span>
                        </li>
                    `).join('')}
                </ul>
            </div>
        `
    }
}
