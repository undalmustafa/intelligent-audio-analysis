/**
 * Intelligent Audio Analysis - Frontend Application
 * Shows audio records from ESP32 IoT device, triggers processing, and allows comparison.
 * No microphone recording — audio comes from ESP32 via .NET backend → PostgreSQL.
 */

const API_BASE_URL = 'http://localhost:5076/api';

// ==============================
// DOM Elements
// ==============================
const backendStatusEl = document.getElementById('backend-status');
const pythonStatusEl = document.getElementById('python-status');
const recordCountEl = document.getElementById('record-count');
const comparisonSection = document.getElementById('comparison-section');
const rawAudioPlayer = document.getElementById('raw-audio');
const processedAudioPlayer = document.getElementById('processed-audio');
const recordsList = document.getElementById('records-list');

// ==============================
// Health Checks
// ==============================
async function checkBackendHealth() {
    try {
        const response = await fetch(`${API_BASE_URL}/audio/records`);
        if (response.ok) {
            backendStatusEl.textContent = 'Online';
            backendStatusEl.className = 'status-badge online';
            return true;
        }
    } catch (e) { /* offline */ }

    backendStatusEl.textContent = 'Offline';
    backendStatusEl.className = 'status-badge offline';
    return false;
}

async function checkPythonHealth() {
    try {
        const response = await fetch('http://localhost:5002/health');
        if (response.ok) {
            pythonStatusEl.textContent = 'Online';
            pythonStatusEl.className = 'status-badge online';
            return true;
        }
    } catch (e) { /* offline */ }

    pythonStatusEl.textContent = 'Offline';
    pythonStatusEl.className = 'status-badge offline';
    return false;
}

// ==============================
// API Communication
// ==============================
async function triggerProcessing(recordId) {
    const processBtn = document.querySelector(`[data-process-id="${recordId}"]`);
    if (processBtn) {
        processBtn.disabled = true;
        processBtn.textContent = '⏳ Processing...';
        processBtn.classList.add('processing');
    }

    try {
        const response = await fetch(`${API_BASE_URL}/audio/${recordId}/process`, {
            method: 'POST',
        });

        if (!response.ok) {
            throw new Error(`Processing failed: ${response.status}`);
        }

        const result = await response.json();
        console.log('Processing result:', result);

        // Refresh records list to show updated status
        await loadRecords();

    } catch (error) {
        console.error('Processing error:', error);
        if (processBtn) {
            processBtn.disabled = false;
            processBtn.textContent = '⚙️ Process';
            processBtn.classList.remove('processing');
        }
        alert('Processing failed. Make sure the Python service is running on port 5002.');
    }
}

async function loadRecords() {
    try {
        const response = await fetch(`${API_BASE_URL}/audio/records`);
        if (!response.ok) throw new Error('Failed to load records');

        const records = await response.json();
        recordCountEl.textContent = records.length;
        renderRecords(records);

    } catch (error) {
        console.error('Error loading records:', error);
        recordsList.innerHTML = '<p class="placeholder">⚠️ Cannot connect to backend API.</p>';
    }
}

// ==============================
// Rendering
// ==============================
function renderRecords(records) {
    if (!records || records.length === 0) {
        recordsList.innerHTML = '<p class="placeholder">No recordings yet. Start the ESP32 mock to send audio.</p>';
        return;
    }

    recordsList.innerHTML = records.map(record => {
        const date = new Date(record.recordDate).toLocaleString();
        const statusClass = record.status.toLowerCase();

        // Show process button only for Pending or Failed records
        const showProcessBtn = record.status === 'Pending' || record.status === 'Failed';
        const processButton = showProcessBtn
            ? `<button class="btn btn-process" data-process-id="${record.id}" onclick="triggerProcessing('${record.id}')">⚙️ Process</button>`
            : '';

        // Show spinner for records currently being processed
        const processingIndicator = record.status === 'Processing'
            ? `<span class="btn btn-process processing" disabled>⏳ Processing...</span>`
            : '';

        // Show compare button only for Completed records
        const compareButton = record.hasProcessedAudio
            ? `<button class="btn btn-compare" onclick="selectRecord('${record.id}')">🔊 Compare</button>`
            : '';

        return `
            <div class="record-item" data-id="${record.id}">
                <div class="record-info">
                    <strong>${date}</strong>
                    <span>${record.durationInSeconds}s • ${record.deviceId}</span>
                </div>
                <div class="record-actions">
                    <span class="record-status ${statusClass}">${record.status}</span>
                    ${processButton}
                    ${processingIndicator}
                    ${compareButton}
                </div>
            </div>
        `;
    }).join('');
}

async function selectRecord(recordId) {
    try {
        // Load raw audio
        rawAudioPlayer.src = `${API_BASE_URL}/audio/${recordId}/raw`;

        // Load processed audio
        processedAudioPlayer.src = `${API_BASE_URL}/audio/${recordId}/processed`;

        comparisonSection.classList.remove('hidden');

        // Scroll to comparison section
        comparisonSection.scrollIntoView({ behavior: 'smooth' });

    } catch (error) {
        console.error('Error loading record:', error);
    }
}

// ==============================
// Auto Refresh
// ==============================
let refreshInterval = null;

function startAutoRefresh() {
    // Refresh records every 5 seconds
    refreshInterval = setInterval(async () => {
        await loadRecords();
    }, 5000);
}

// ==============================
// Init
// ==============================
document.addEventListener('DOMContentLoaded', async () => {
    // Initial health checks
    await checkBackendHealth();
    await checkPythonHealth();

    // Load records
    await loadRecords();

    // Start auto-refresh (for incoming ESP32 data)
    startAutoRefresh();

    // Periodic health checks (every 30 seconds)
    setInterval(async () => {
        await checkBackendHealth();
        await checkPythonHealth();
    }, 30000);
});
