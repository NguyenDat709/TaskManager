let draggedTaskId = null;

function dragStart(ev, taskId) {
    draggedTaskId = taskId;
    ev.dataTransfer.setData("text/plain", taskId);
}

function allowDrop(ev) {
    ev.preventDefault();
    ev.currentTarget.classList.add("bg-blue-50/70", "rounded-lg");
}

function leaveDrop(ev) {
    ev.currentTarget.classList.remove("bg-blue-50/70", "rounded-lg");
}

async function dropCard(ev, newStatus) {
    ev.preventDefault();
    const dropZone = ev.currentTarget;
    dropZone.classList.remove("bg-blue-50/70", "rounded-lg");

    const taskId = ev.dataTransfer.getData("text/plain") || draggedTaskId;
    const card = document.getElementById("task-card-" + taskId);
    if (!card) return;

    dropZone.appendChild(card);
    updateColumnCounts();

    const syncBadge = document.getElementById("syncStatus");
    if (syncBadge) {
        syncBadge.innerHTML = "• Trạng thái: <strong class='text-blue-600'>⏳ Đang lưu vào MySQL...</strong>";
    }

    try {
        const res = await fetch("/Kanban/UpdateStatus", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ taskId: parseInt(taskId), newStatus: newStatus })
        });

        const data = await res.json();
        if (syncBadge) {
            if (res.ok && data.success) {
                syncBadge.innerHTML = "• Trạng thái: <strong class='text-emerald-600'>✅ PRJ-TASK-" + taskId + " ➔ " + newStatus + "</strong>";
            } else {
                syncBadge.innerHTML = "• Trạng thái: <strong class='text-rose-600'>❌ " + (data.message || "Lỗi lưu CSDL") + "</strong>";
            }
        }
    } catch (err) {
        if (syncBadge) {
            syncBadge.innerHTML = "• Trạng thái: <strong class='text-rose-600'>❌ Lỗi kết nối máy chủ!</strong>";
        }
    }
}

function updateColumnCounts() {
    document.querySelectorAll("[data-status]").forEach(zone => {
        const countBadge = zone.parentElement.querySelector(".column-count");
        if (countBadge) {
            const visibleCards = Array.from(zone.querySelectorAll(".task-card")).filter(c => c.style.display !== "none");
            countBadge.innerText = visibleCards.length;
        }
    });
}

function filterCardsClientSide() {
    const tagVal = document.getElementById("tagFilterSelect").value.toLowerCase();
    const deadlineVal = document.getElementById("deadlineFilterSelect").value;

    document.querySelectorAll(".task-card").forEach(card => {
        const title = (card.getAttribute("data-title") || "").toLowerCase();
        const isOverdue = card.getAttribute("data-overdue") === "true";

        const matchTag = (tagVal === "all") || title.includes(tagVal);
        const matchDeadline = (deadlineVal === "all")
            || (deadlineVal === "overdue" && isOverdue)
            || (deadlineVal === "ontime" && !isOverdue);

        card.style.display = (matchTag && matchDeadline) ? "" : "none";
    });

    updateColumnCounts();
}

function copyInviteCode(code) {
    navigator.clipboard.writeText(code);
    alert("Đã sao chép mã mời dự án: " + code);
}

function openCreateModal(defaultStatus) {
    const select = document.getElementById("modalStatusSelect");
    if (select) select.value = defaultStatus;
    document.getElementById("createModal")?.classList.remove("hidden");
}

function closeCreateModal() {
    document.getElementById("createModal")?.classList.add("hidden");
}