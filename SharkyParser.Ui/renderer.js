const { ipcRenderer } = require('electron');
const fs = require('fs');
const path = require('path');

// Load version from package.json
const packageJson = JSON.parse(fs.readFileSync(path.join(__dirname, 'package.json'), 'utf8'));
const appVersion = packageJson.version;

// Update version badge
document.addEventListener('DOMContentLoaded', () => {
    const versionBadge = document.querySelector('.version-badge');
    if (versionBadge) {
        versionBadge.textContent = `v${appVersion}`;
    }
});

// State
let allEntries = [];
let filteredEntries = [];
let currentView = 'table';

// DOM Elements
const loadFileBtn = document.getElementById('loadFileBtn');
const filePathEl = document.getElementById('filePath');
const statusEl = document.getElementById('status');
const resultsEl = document.getElementById('results');
const entryCountEl = document.getElementById('entryCount');
const modalEl = document.getElementById('entryModal');
const modalBodyEl = document.getElementById('modalBody');

// Statistics elements
const totalEntriesEl = document.getElementById('totalEntries');
const errorCountEl = document.getElementById('errorCount');
const warningCountEl = document.getElementById('warningCount');
const infoCountEl = document.getElementById('infoCount');

// Filter elements
const filterError = document.getElementById('filterError');
const filterWarning = document.getElementById('filterWarning');
const filterInfo = document.getElementById('filterInfo');
const filterDebug = document.getElementById('filterDebug');
const searchInput = document.getElementById('searchInput');
const errorTypeFilter = document.getElementById('errorTypeFilter');

// View buttons
const viewTableBtn = document.getElementById('viewTable');
const viewCardsBtn = document.getElementById('viewCards');

// Theme toggle
const themeToggle = document.getElementById('themeToggle');

// Event Listeners
loadFileBtn.addEventListener('click', loadFile);
filterError.addEventListener('change', applyFilters);
filterWarning.addEventListener('change', applyFilters);
filterInfo.addEventListener('change', applyFilters);
filterDebug.addEventListener('change', applyFilters);
searchInput.addEventListener('input', debounce(applyFilters, 300));
errorTypeFilter.addEventListener('change', applyFilters);

viewTableBtn.addEventListener('click', () => setView('table'));
viewCardsBtn.addEventListener('click', () => setView('cards'));

themeToggle.addEventListener('click', toggleTheme);

// Modal close handlers
document.querySelector('.modal-backdrop').addEventListener('click', closeModal);
document.querySelector('.close-btn').addEventListener('click', closeModal);

// Check C# backend on load
checkBackend();

// Setup update status
setupUpdateStatus();

async function checkBackend() {
    const available = await ipcRenderer.invoke('check-csharp-backend');
    if (!available) {
        statusEl.innerHTML = '⚠️ <a href="#" id="buildLink">Embedded SharkyParser not built. Click to build.</a>';
        document.getElementById('buildLink')?.addEventListener('click', () => {
            require('electron').shell.openExternal('https://github.com/BlessedDayss/SharkyParser#building');
        });
    } else {
        statusEl.textContent = '✅ Embedded C# Backend (SharkyParser) ready';
    }
}

function setupUpdateStatus() {
    // Update status can be shown here if needed
    // For now, auto-updater works silently in the background
}

async function loadFile() {
    const result = await ipcRenderer.invoke('select-file');
    if (!result) return;

    filePathEl.textContent = result;
    statusEl.textContent = '⏳ Parsing with C# backend...';
    loadFileBtn.disabled = true;

    try {
        const data = await ipcRenderer.invoke('parse-log-csharp', result);

        allEntries = data.entries.map((e, i) => ({
            ...e,
            id: i,
            timestamp: new Date(e.timestamp),
            errorType: determineErrorType(e.message)
        }));

        updateStatistics(data.statistics);
        updateErrorTypeFilter();
        applyFilters();

        statusEl.textContent = `✅ Parsed ${allEntries.length} entries using C# SharkyParser`;
    } catch (error) {
        statusEl.textContent = `❌ Error: ${error.message}`;
        console.error('Parsing error:', error);
    } finally {
        loadFileBtn.disabled = false;
    }
}

function updateStatistics(stats) {
    totalEntriesEl.textContent = stats.total;
    errorCountEl.textContent = stats.errors;
    warningCountEl.textContent = stats.warnings;
    infoCountEl.textContent = stats.info;

    // Update error types breakdown
    const errorTypesBreakdown = document.getElementById('errorTypesBreakdown');
    const errorEntries = allEntries.filter(e => e.level === 'ERROR');
    const typeGroups = {};

    errorEntries.forEach(e => {
        typeGroups[e.errorType] = (typeGroups[e.errorType] || 0) + 1;
    });

    if (Object.keys(typeGroups).length > 0) {
        errorTypesBreakdown.innerHTML = `
            <h4>Error Types</h4>
            ${Object.entries(typeGroups)
                .sort((a, b) => b[1] - a[1])
                .map(([type, count]) => `
                    <div class="error-type-item">
                        <span class="error-type-name">${type}</span>
                        <span class="error-type-count">${count}</span>
                    </div>
                `).join('')}
        `;
    } else {
        errorTypesBreakdown.innerHTML = '';
    }
}

function updateErrorTypeFilter() {
    const types = [...new Set(allEntries.map(e => e.errorType).filter(Boolean))];
    errorTypeFilter.innerHTML = '<option value="">All Types</option>' +
        types.map(t => `<option value="${t}">${t}</option>`).join('');
}

function applyFilters() {
    const levels = [];
    if (filterError.checked) levels.push('ERROR');
    if (filterWarning.checked) levels.push('WARN');
    if (filterInfo.checked) levels.push('INFO');
    if (filterDebug.checked) levels.push('DEBUG');

    const searchTerm = searchInput.value.toLowerCase();
    const errorType = errorTypeFilter.value;

    filteredEntries = allEntries.filter(entry => {
        if (!levels.includes(entry.level)) return false;
        if (searchTerm && !entry.message.toLowerCase().includes(searchTerm)) return false;
        if (errorType && entry.errorType !== errorType) return false;
        return true;
    });

    entryCountEl.textContent = `Showing ${filteredEntries.length} of ${allEntries.length}`;
    displayResults();
}

function setView(view) {
    currentView = view;
    viewTableBtn.classList.toggle('active', view === 'table');
    viewCardsBtn.classList.toggle('active', view === 'cards');
    displayResults();
}

function displayResults() {
    if (filteredEntries.length === 0) {
        if (allEntries.length === 0) {
            resultsEl.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📄</div>
                    <h3>No Log File Loaded</h3>
                    <p>Click "Load Log File" to get started</p>
                </div>
            `;
        } else {
            resultsEl.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">🔍</div>
                    <h3>No Matching Entries</h3>
                    <p>Try adjusting your filters</p>
                </div>
            `;
        }
        return;
    }

    if (currentView === 'table') {
        displayTableView();
    } else {
        displayCardView();
    }
}

function displayTableView() {
    const maxEntries = 500;
    const entriesToShow = filteredEntries.slice(0, maxEntries);

    resultsEl.innerHTML = `
        <table class="log-table">
            <thead>
                <tr>
                    <th>Line</th>
                    <th>Time</th>
                    <th>Level</th>
                    <th>Source</th>
                    <th>Message</th>
                </tr>
            </thead>
            <tbody>
                ${entriesToShow.map(entry => `
                    <tr class="level-${entry.level.toLowerCase()}" data-id="${entry.id}">
                        <td class="line-num">${entry.lineNumber || '-'}</td>
                        <td class="timestamp">${formatTimestamp(entry.timestamp)}</td>
                        <td><span class="level-badge ${entry.level.toLowerCase()}">${entry.level}</span></td>
                        <td class="source">${entry.source || '-'}</td>
                        <td class="message">${escapeHtml(truncate(entry.message, 100))}</td>
                    </tr>
                `).join('')}
            </tbody>
        </table>
        ${filteredEntries.length > maxEntries ? `
            <div class="more-entries">
                Showing ${maxEntries} of ${filteredEntries.length} entries. Use filters to narrow down.
            </div>
        ` : ''}
    `;

    // Add click handlers
    resultsEl.querySelectorAll('tbody tr').forEach(row => {
        row.addEventListener('click', () => {
            const id = parseInt(row.dataset.id);
            showEntryDetails(allEntries.find(e => e.id === id));
        });
    });
}

function displayCardView() {
    const maxEntries = 100;
    const entriesToShow = filteredEntries.slice(0, maxEntries);

    resultsEl.innerHTML = `
        <div class="cards-grid">
            ${entriesToShow.map(entry => `
                <div class="log-card level-${entry.level.toLowerCase()}" data-id="${entry.id}">
                    <div class="card-header">
                        <span class="level-badge ${entry.level.toLowerCase()}">${entry.level}</span>
                        <span class="card-time">${formatTimestamp(entry.timestamp)}</span>
                    </div>
                    <div class="card-body">
                        <p class="card-message">${escapeHtml(truncate(entry.message, 150))}</p>
                    </div>
                    <div class="card-footer">
                        ${entry.source ? `<span class="card-source">📁 ${entry.source}</span>` : ''}
                        ${entry.stackTrace ? '<span class="has-stack">📋 Stack trace</span>' : ''}
                    </div>
                </div>
            `).join('')}
        </div>
        ${filteredEntries.length > maxEntries ? `
            <div class="more-entries">
                Showing ${maxEntries} of ${filteredEntries.length} entries. Use filters to narrow down.
            </div>
        ` : ''}
    `;

    // Add click handlers
    resultsEl.querySelectorAll('.log-card').forEach(card => {
        card.addEventListener('click', () => {
            const id = parseInt(card.dataset.id);
            showEntryDetails(allEntries.find(e => e.id === id));
        });
    });
}

function showEntryDetails(entry) {
    if (!entry) return;

    modalBodyEl.innerHTML = `
        <div class="detail-grid">
            <div class="detail-item">
                <label>Line Number</label>
                <span>${entry.lineNumber || 'N/A'}</span>
            </div>
            <div class="detail-item">
                <label>Timestamp</label>
                <span>${entry.timestamp.toLocaleString()}</span>
            </div>
            <div class="detail-item">
                <label>Level</label>
                <span class="level-badge ${entry.level.toLowerCase()}">${entry.level}</span>
            </div>
            <div class="detail-item">
                <label>Source</label>
                <span>${entry.source || 'N/A'}</span>
            </div>
            ${entry.errorType ? `
                <div class="detail-item">
                    <label>Error Type</label>
                    <span class="error-type-badge">${entry.errorType}</span>
                </div>
            ` : ''}
        </div>
        
        <div class="detail-section">
            <label>Message</label>
            <pre class="detail-content">${escapeHtml(entry.message)}</pre>
        </div>
        
        ${entry.stackTrace ? `
            <div class="detail-section">
                <label>Stack Trace</label>
                <pre class="detail-content stack-trace">${escapeHtml(entry.stackTrace)}</pre>
            </div>
        ` : ''}
        
        <div class="detail-section">
            <label>Raw Data</label>
            <pre class="detail-content raw-data">${escapeHtml(entry.rawData)}</pre>
        </div>
    `;

    modalEl.classList.remove('hidden');
}

function closeModal() {
    modalEl.classList.add('hidden');
}

// Helper functions
function determineErrorType(message) {
    if (!message) return 'Unknown';
    const lower = message.toLowerCase();

    if (lower.includes('nullreferenceexception') || lower.includes('null reference')) return 'NullReference';
    if (lower.includes('argumentexception') || lower.includes('invalid argument')) return 'ArgumentError';
    if (lower.includes('timeout') || lower.includes('timed out')) return 'Timeout';
    if (lower.includes('connection') || lower.includes('network')) return 'Network';
    if (lower.includes('database') || lower.includes('sql')) return 'Database';
    if (lower.includes('file') || lower.includes('io exception')) return 'FileSystem';
    if (lower.includes('permission') || lower.includes('access denied')) return 'Security';
    if (lower.includes('validation') || lower.includes('invalid')) return 'Validation';

    return 'Application';
}

function formatTimestamp(date) {
    if (!date || date.getTime() === new Date(0).getTime()) return 'N/A';
    return date.toLocaleTimeString('en-US', { hour12: false });
}

function truncate(str, length) {
    if (!str) return '';
    return str.length > length ? str.substring(0, length) + '...' : str;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function toggleTheme() {
    document.body.classList.toggle('light-theme');
    const icon = themeToggle.querySelector('.theme-icon');
    icon.textContent = document.body.classList.contains('light-theme') ? '☀️' : '🌙';
}

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeModal();
    if ((e.metaKey || e.ctrlKey) && e.key === 'o') {
        e.preventDefault();
        loadFile();
    }
});