let allEntries = [];
let filteredEntries = [];
const selectFileBtn = document.getElementById("select-file-btn");
const logBody = document.getElementById("log-body");
const statTotal = document.getElementById("stat-total");
const statErrors = document.getElementById("stat-errors");
const statWarnings = document.getElementById("stat-warnings");
const statInfo = document.getElementById("stat-info");
const backendStatus = document.getElementById("backend-status");
const searchInput = document.getElementById("search-input");
const levelFilter = document.getElementById("level-filter");
const fileInfo = document.getElementById("file-info");
const modalOverlay = document.getElementById("modal-overlay");
const modalDetails = document.getElementById("modal-details");
const closeModal = document.getElementById("close-modal");
window.addEventListener("DOMContentLoaded", async () => {
  try {
    const isReady = await window.electron.ipcRenderer.invoke("check-csharp-backend");
    if (isReady) {
      backendStatus.textContent = "Operational ✅";
      backendStatus.style.color = "var(--success)";
    } else {
      backendStatus.textContent = "Backend Missing ❌";
      backendStatus.style.color = "var(--error)";
    }
  } catch (err) {
    backendStatus.textContent = "Error ⚠️";
  }
});
selectFileBtn.addEventListener("click", async () => {
  try {
    selectFileBtn.disabled = true;
    selectFileBtn.innerHTML = "<span>⏳</span> Parsing...";
    const result = await window.electron.ipcRenderer.invoke("select-file");
    if (result) {
      fileInfo.textContent = result.split("/").pop() || result;
      const data = await window.electron.ipcRenderer.invoke("parse-log-csharp", result);
      allEntries = data.entries.map((e, idx) => ({ ...e, id: idx }));
      updateStats(data.statistics);
      applyFilters();
    }
  } catch (error) {
    console.error("Parsing failed:", error);
    alert("Failed to parse log: " + error.message);
  } finally {
    selectFileBtn.disabled = false;
    selectFileBtn.innerHTML = "<span>🚀</span> Open Log File";
  }
});
searchInput.addEventListener("input", () => applyFilters());
levelFilter.addEventListener("change", () => applyFilters());
function applyFilters() {
  const searchTerm = searchInput.value.toLowerCase();
  const level = levelFilter.value;
  filteredEntries = allEntries.filter((entry) => {
    const matchesSearch = entry.message.toLowerCase().includes(searchTerm) || (entry.source || "").toLowerCase().includes(searchTerm);
    const matchesLevel = level === "ALL" || entry.level.toUpperCase() === level.toUpperCase();
    return matchesSearch && matchesLevel;
  });
  renderTable();
}
function updateStats(stats) {
  statTotal.textContent = stats.total.toString();
  statErrors.textContent = stats.errors.toString();
  statWarnings.textContent = stats.warnings.toString();
  statInfo.textContent = stats.info.toString();
}
function renderTable() {
  logBody.innerHTML = "";
  const displayEntries = filteredEntries.slice(0, 1e3);
  displayEntries.forEach((entry) => {
    const row = document.createElement("tr");
    row.className = "log-row";
    row.onclick = () => showDetails(entry);
    const levelClass = `level-${entry.level.toLowerCase()}`;
    row.innerHTML = `
            <td class="timestamp">${new Date(entry.timestamp).toLocaleString()}</td>
            <td><span class="level-tag ${levelClass}">${entry.level}</span></td>
            <td class="message">${escapeHtml(entry.message)}</td>
        `;
    logBody.appendChild(row);
  });
}
function showDetails(entry) {
  modalDetails.innerHTML = `
        <div class="detail-row">
            <span class="detail-label">Level</span>
            <span class="level-tag level-${entry.level.toLowerCase()}">${entry.level}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Timestamp</span>
            <span style="color: white;">${new Date(entry.timestamp).toString()}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Source</span>
            <span style="color: var(--accent-primary);">${entry.source || "Unknown"}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Message</span>
            <div style="color: #fff; line-height: 1.6; background: rgba(0,0,0,0.2); padding: 12px; border-radius: 8px;">
                ${escapeHtml(entry.message)}
            </div>
        </div>
        ${entry.stackTrace ? `
            <div style="margin-top: 20px;">
                <span class="detail-label" style="display: block; margin-bottom: 8px;">Stack Trace</span>
                <pre class="code-block">${escapeHtml(entry.stackTrace)}</pre>
            </div>
        ` : ""}
        ${entry.rawData && entry.rawData !== entry.message ? `
            <div style="margin-top: 20px;">
                <span class="detail-label" style="display: block; margin-bottom: 8px;">Raw Data</span>
                <pre class="code-block" style="color: var(--text-dim);">${escapeHtml(entry.rawData)}</pre>
            </div>
        ` : ""}
    `;
  modalOverlay.classList.add("active");
}
closeModal.onclick = () => modalOverlay.classList.remove("active");
modalOverlay.onclick = (e) => {
  if (e.target === modalOverlay) modalOverlay.classList.remove("active");
};
function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}
