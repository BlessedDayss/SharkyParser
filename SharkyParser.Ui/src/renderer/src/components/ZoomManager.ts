export class ZoomManager {
    private zoomValue: HTMLElement | null

    constructor() {
        this.zoomValue = document.getElementById('zoom-value')
    }

    public init() {
        document.getElementById('zoom-in-btn')?.addEventListener('click', () => this.changeZoom(0.1))
        document.getElementById('zoom-out-btn')?.addEventListener('click', () => this.changeZoom(-0.1))

        const savedZoom = localStorage.getItem('zoom-factor')
        if (savedZoom) {
            this.setZoom(parseFloat(savedZoom))
        } else {
            this.setZoom(1.0)
        }
    }

    private changeZoom(delta: number) {
        let current = parseFloat(localStorage.getItem('zoom-factor') || '1.0')
        current = Math.min(2.0, Math.max(0.5, current + delta))
        this.setZoom(current)
    }

    private setZoom(factor: number) {
        ; (window as any).electron.ipcRenderer.invoke('set-zoom', factor)
        localStorage.setItem('zoom-factor', factor.toString())
        if (this.zoomValue) {
            this.zoomValue.textContent = `${Math.round(factor * 100)}%`
        }
    }
}
