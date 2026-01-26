import './style.css'
import { App } from './core/App'

// Bootstrap the application
document.addEventListener('DOMContentLoaded', () => {
    const app = App.getInstance()
    app.init()
})
