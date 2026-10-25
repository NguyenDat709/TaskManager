let criticalOnly = false;

function openScheduleModal(taskId, title, startStr, deadlineStr) {
    document.getElementById('modalTaskId').value = taskId;
    document.getElementById('modalTaskTitle').innerText = '#' + taskId + ' - ' + title;
    document.getElementById('modalStartDate').value = startStr;
    document.getElementById('modalDeadline').value = deadlineStr;
    document.getElementById('modalError').classList.add('hidden');
    document.getElementById('scheduleModal').classList.remove('hidden');
}

function closeScheduleModal() {
    document.getElementById('scheduleModal').classList.add('hidden');
}

async function saveScheduleFromModal() {
    const taskId = parseInt(document.getElementById('modalTaskId').value);
    const startDate = document.getElementById('modalStartDate').value;
    const deadline = document.getElementById('modalDeadline').value;
    await callUpdateScheduleApi(taskId, startDate, deadline);
}

async function shiftSchedule(taskId, startStr, deadlineStr, daysToAdd) {
    const start = new Date(startStr);
    const end = new Date(deadlineStr);
    start.setDate(start.getDate() + daysToAdd);
    end.setDate(end.getDate() + daysToAdd);

    const toLocalIso = (d) => {
        const pad = (n) => n < 10 ? '0' + n : n;
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + 'T' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    };

    await callUpdateScheduleApi(taskId, toLocalIso(start), toLocalIso(end));
}

async function callUpdateScheduleApi(taskId, startDate, deadline) {
    const badge = document.getElementById('apiStatusBadge');
    if (badge) {
        badge.innerHTML = "⏳ Đang lưu MySQL...";
        badge.className = "font-mono text-[10px] font-semibold text-amber-600 bg-amber-50 px-2 py-1 rounded";
    }

    try {
        const res = await fetch('/Gantt/UpdateSchedule', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ taskId, startDate, deadline })
        });

        const data = await res.json();
        if (res.ok && data.success) {
            window.location.reload();
        } else {
            const errBox = document.getElementById('modalError');
            if (errBox) {
                errBox.innerText = data.message || 'Lỗi cập nhật!';
                errBox.classList.remove('hidden');
            }
            if (badge) {
                badge.innerText = "❌ Lỗi";
                badge.className = "font-mono text-[10px] font-semibold text-rose-600 bg-rose-50 px-2 py-1 rounded";
            }
        }
    } catch (e) {
        if (badge) {
            badge.innerText = "❌ Mất kết nối";
            badge.className = "font-mono text-[10px] font-semibold text-rose-600 bg-rose-50 px-2 py-1 rounded";
        }
    }
}

function toggleCriticalPath(forceEnable) {
    criticalOnly = forceEnable !== undefined ? forceEnable : !criticalOnly;
    const btn = document.getElementById('btnCriticalPath');
    if (btn) {
        btn.className = criticalOnly
            ? "px-3 py-1.5 border border-rose-300 bg-rose-50 text-rose-700 font-bold rounded-lg transition"
            : "px-3 py-1.5 border border-slate-200 bg-slate-50 hover:bg-slate-100 rounded-lg transition";
    }

    document.querySelectorAll('.gantt-row').forEach(row => {
        const isCritical = row.getAttribute('data-critical') === 'true';
        row.style.display = (criticalOnly && !isCritical) ? 'none' : '';
    });
}