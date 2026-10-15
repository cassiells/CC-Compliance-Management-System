/* DoComply Interactive Prototype State Engine & UI Logic */

document.addEventListener('DOMContentLoaded', () => {
    initLucideIcons();
    initGlobalSearch();
    initPasswordTrackers();
    initModalSystem();
    initNotificationSystem();
    initLocalTableSearchAndPagination();
    initRoleSwitchers();
    initDynamicPrototypeControls();
});

// Re-initialize Lucide icons safely
function initLucideIcons() {
    if (window.lucide) {
        window.lucide.createIcons();
    }
}

/* ----------------------------------------------------
   1. MOCK DATA & SESSION STATE
---------------------------------------------------- */
const DEFAULT_SYSTEM_STATE = {
    currentRole: 'Admin',
    currentUser: {
        name: 'Juan Dela Cruz',
        role: 'Administrator',
        email: 'juan@cccorp.com'
    },
    notifications: [
        { id: 1, type: 'warning', title: 'Tax Clearance Expiring Soon', message: 'Your Tax Clearance will expire soon. Please submit an updated document.', time: '10 mins ago', unread: true, role: 'Supplier' },
        { id: 2, type: 'danger', title: 'Safety Certificate Expired', message: 'Your Safety Certificate has expired. Please upload a renewed document.', time: '1 hour ago', unread: true, role: 'Supplier' },
        { id: 3, type: 'danger', title: 'Business Permit Rejected', message: 'Your submitted Business Permit was rejected. Please review and submit a valid document.', time: 'Yesterday', unread: true, role: 'Supplier' },
        { id: 4, type: 'info', title: 'New Submission Queued', message: 'ABC Office Supplies submitted Safety Certificate for verification.', time: '2 hours ago', unread: true, role: 'Admin' },
        { id: 5, type: 'info', title: 'Procurement Request Pending', message: 'Staff submitted a new procurement request for Office Materials.', time: '3 hours ago', unread: true, role: 'Admin' }
    ],
    settings: {
        orgName: 'CC Corporation',
        supportEmail: 'compliance@cccorp.com',
        expiryReminders: true,
        expiredAlerts: true,
        verificationOutcomes: true
    }
};

// Initialize Session Storage
function getSessionState() {
    const data = sessionStorage.getItem('docomply_state');
    if (!data) {
        sessionStorage.setItem('docomply_state', JSON.stringify(DEFAULT_SYSTEM_STATE));
        return DEFAULT_SYSTEM_STATE;
    }
    return JSON.parse(data);
}

function updateSessionState(newState) {
    sessionStorage.setItem('docomply_state', JSON.stringify(newState));
}

/* ----------------------------------------------------
   2. ROLE-SCOPED GLOBAL SEARCH ENGINE (ANTI-BYPASS)
---------------------------------------------------- */
function initGlobalSearch() {
    const searchInput = document.getElementById('globalSearchInput');
    const dropdown = document.getElementById('globalSearchDropdown');
    const clearBtn = document.getElementById('globalSearchClear');
    const searchContainer = document.querySelector('.dc-global-search');

    if (!searchInput || !dropdown) return;

    // Detect current role & portal name
    const path = window.location.pathname;
    let currentRole = searchContainer?.dataset?.currentRole || '';
    if (!currentRole) {
        if (path.startsWith('/SuperAdmin')) currentRole = 'SuperAdmin';
        else if (path.startsWith('/Staff')) currentRole = 'Staff';
        else if (path.startsWith('/Supplier')) currentRole = 'Supplier';
        else currentRole = 'Admin';
    }

    const portalName = searchContainer?.dataset?.rolePortal || (
        currentRole === 'SuperAdmin' ? 'Super Administrator' :
        currentRole === 'Staff' ? 'Staff / HR' :
        currentRole === 'Supplier' ? 'Supplier' : 'Administrator'
    );

    // Strictly segregated search indexes per role
    const roleSearchIndexes = {
        Admin: [
            // Admin Dashboard
            { label: 'Admin Dashboard', sub: 'Overview • KPIs, compliance metrics & quick actions', type: 'Dashboard', url: '/Admin/Dashboard', keywords: 'dashboard home overview kpis metrics compliance' },

            // Suppliers Management
            { label: 'Suppliers Directory', sub: 'Directory • Browse & manage registered suppliers', type: 'Suppliers', url: '/Admin/Suppliers', keywords: 'suppliers directory vendors list partners registered' },
            { label: 'ABC Office Supplies', sub: 'Active Supplier • COMPLIANT', type: 'Supplier', url: '/Admin/SupplierDetails?name=ABC+Office+Supplies', keywords: 'supplier vendor abc office supplies compliant' },
            { label: 'XYZ Training Services', sub: 'Active Supplier • NOT COMPLIANT', type: 'Supplier', url: '/Admin/SupplierDetails?name=XYZ+Training+Services', keywords: 'supplier vendor xyz training services non-compliant' },
            { label: 'DEF Equipment Corp.', sub: 'Active Supplier • NOT COMPLIANT', type: 'Supplier', url: '/Admin/SupplierDetails?name=DEF+Equipment+Corp.', keywords: 'supplier vendor def equipment corp non-compliant' },
            { label: 'Global Logistics Inc.', sub: 'Active Supplier • COMPLIANT', type: 'Supplier', url: '/Admin/SupplierDetails?name=Global+Logistics+Inc.', keywords: 'supplier vendor global logistics inc compliant' },
            { label: 'TechHub Solutions', sub: 'Active Supplier • PENDING VERIFICATION', type: 'Supplier', url: '/Admin/SupplierDetails?name=TechHub+Solutions', keywords: 'supplier vendor techhub solutions pending' },

            // Requirements Configuration
            { label: 'Compliance Requirements', sub: 'Configuration • Manage mandatory documents & rules', type: 'Requirements', url: '/Admin/Requirements', keywords: 'requirements config compliance mandatory optional document types' },
            { label: 'Business Permit Requirement', sub: 'Requirement • Expiry Required (Annual LGU Permit)', type: 'Requirement', url: '/Admin/Requirements', keywords: 'business permit requirement annual permit mayor lgu' },
            { label: 'BIR TIN Registration (2303)', sub: 'Requirement • Tax Identification & TIN Validation', type: 'Requirement', url: '/Admin/Requirements', keywords: 'bir registration tin 2303 tax bureau internal revenue' },
            { label: 'Tax Clearance Certificate', sub: 'Requirement • Expiry Required (Annual BIR)', type: 'Requirement', url: '/Admin/Requirements', keywords: 'tax clearance certificate bir expiry' },
            { label: 'Safety & Quality Certificate', sub: 'Requirement • Expiry Required (DOLE/ISO)', type: 'Requirement', url: '/Admin/Requirements', keywords: 'safety certificate dole iso quality compliance' },
            { label: 'Data Privacy Agreement', sub: 'Requirement • No Expiry (One-time compliance)', type: 'Requirement', url: '/Admin/Requirements', keywords: 'data privacy agreement dpa legal compliance' },

            // Verification Queue & Document Review
            { label: 'Verification Queue', sub: 'Queue • Documents Pending Admin Verification', type: 'Verification', url: '/Admin/VerificationQueue', keywords: 'verification queue review pending approve reject audit' },
            { label: 'Document Review & BIR TIN Validation', sub: 'Review • Inspect submissions, OCR & TIN validation', type: 'Verification', url: '/Admin/DocumentReview', keywords: 'document review tin bir ocr inspect validation approve reject' },
            { label: 'Expiry Monitoring', sub: 'Monitoring • Documents expiring within 30 days', type: 'Monitoring', url: '/Admin/ExpiryMonitoring', keywords: 'expiry monitoring expired expiring permits renew alert warning' },

            // Procurement Review Queue
            { label: 'Procurement Approval Queue', sub: 'Purchasing • Review department purchase requests', type: 'Procurement', url: '/Admin/Procurement', keywords: 'procurement approval purchase order po queue request requisitions' },
            { label: 'Office Supplies Purchase Review', sub: 'Procurement Review • ABC Office Supplies (Compliant)', type: 'Procurement', url: '/Admin/ProcurementDetails?id=1&compliant=true&supplier=ABC+Office+Supplies&request=Office+Supplies+Purchase', keywords: 'procurement purchase office supplies abc review po' },
            { label: 'IT Equipment Purchase Review', sub: 'Procurement Review • XYZ Training Services (Non-Compliant)', type: 'Procurement', url: '/Admin/ProcurementDetails?id=2&compliant=false&supplier=XYZ+Training+Services&request=IT+Equipment+Purchase', keywords: 'procurement purchase it equipment xyz review po' },

            // Reports & Notifications
            { label: 'Compliance Audit Reports', sub: 'Analytics & Export • Supplier compliance standings & charts', type: 'Reports', url: '/Admin/Reports', keywords: 'reports compliance audit export analytics chart pdf excel' },
            { label: 'Expired Documents Audit Report', sub: 'Audit Report • Track expired credentials for follow-up', type: 'Reports', url: '/Admin/Reports', keywords: 'reports expired audit download pdf excel' },
            { label: 'Admin Notifications', sub: 'System Notifications • Submissions & system alerts', type: 'Notifications', url: '/Admin/Notifications', keywords: 'notifications alerts messages bell admin' }
        ],
        Staff: [
            // Suppliers Directory
            { label: 'Suppliers Directory', sub: 'Directory • Search active & compliant vendors for requisitions', type: 'Suppliers', url: '/Staff/Suppliers', keywords: 'suppliers directory vendors list partners active compliant' },
            { label: 'ABC Office Supplies', sub: 'Active Supplier • COMPLIANT', type: 'Supplier', url: '/Staff/SupplierDetails?name=ABC+Office+Supplies', keywords: 'supplier vendor abc office supplies compliant' },
            { label: 'Global Logistics Inc.', sub: 'Active Supplier • COMPLIANT', type: 'Supplier', url: '/Staff/SupplierDetails?name=Global+Logistics+Inc.', keywords: 'supplier vendor global logistics inc compliant' },
            { label: 'XYZ Training Services', sub: 'Active Supplier • NOT COMPLIANT', type: 'Supplier', url: '/Staff/SupplierDetails?name=XYZ+Training+Services', keywords: 'supplier vendor xyz training services non-compliant' },

            // Requisitions
            { label: 'New Procurement Request', sub: 'Requisition • Submit new purchase requisition to Admin', type: 'Requisition', url: '/Staff/NewRequest', keywords: 'new request create requisition purchase order add submit pr' },
            { label: 'My Procurement Requests', sub: 'Requisition • Track department submitted requests & approvals', type: 'Requests', url: '/Staff/MyRequests', keywords: 'my requests track requisitions pending approved orders department' },

            // Reports & Notifications
            { label: 'Staff Department Reports', sub: 'Reports & Analytics • Supplier compliance & requisition metrics', type: 'Reports', url: '/Staff/Reports', keywords: 'reports analytics charts staff compliance' },
            { label: 'Staff Notifications', sub: 'Notifications • Purchase approval updates & compliance alerts', type: 'Notifications', url: '/Staff/Notifications', keywords: 'notifications alerts requisitions messages staff' }
        ],
        Supplier: [
            // Compliance & Submissions
            { label: 'My Compliance Standing', sub: 'Overview • Document compliance status & renewal dates', type: 'Compliance', url: '/Supplier/MyCompliance', keywords: 'compliance standing my documents status tier standing summary' },
            { label: 'Upload Document / Credential', sub: 'Submission • Submit business permit, tax clearance, BIR', type: 'Upload', url: '/Supplier/UploadDocument', keywords: 'upload document submit renew credential file' },
            { label: 'Upload Business Permit', sub: 'Submission • Submit latest Mayor\'s / Business Permit', type: 'Upload', url: '/Supplier/UploadDocument?req=Business+Permit', keywords: 'upload business permit mayor lgu annual' },
            { label: 'Upload BIR Registration (Form 2303)', sub: 'Submission • Submit BIR Form 2303 Certificate', type: 'Upload', url: '/Supplier/UploadDocument?req=BIR+Registration', keywords: 'upload bir registration 2303 tin tax certificate' },
            { label: 'Upload Tax Clearance', sub: 'Submission • Submit valid Tax Clearance Certificate', type: 'Upload', url: '/Supplier/UploadDocument?req=Tax+Clearance', keywords: 'upload tax clearance bir certificate' },
            { label: 'Supplier Notifications', sub: 'Notifications • Verification outcomes & upcoming expiry alerts', type: 'Notifications', url: '/Supplier/Notifications', keywords: 'notifications alerts reminders expiry approved rejected' }
        ],
        SuperAdmin: [
            // System Governance
            { label: 'System Overview Dashboard', sub: 'Overview • Health metrics, organization status & security', type: 'Overview', url: '/SuperAdmin/Overview', keywords: 'overview dashboard system health metrics kpis superadmin' },
            { label: 'User Management', sub: 'Security • Manage user accounts, assign roles & reset passwords', type: 'Users', url: '/SuperAdmin/Users', keywords: 'users user management accounts staff admin reset password add user' },
            { label: 'Security Roles & Permissions', sub: 'Security • RBAC role matrix, security permissions & access control', type: 'Roles', url: '/SuperAdmin/Roles', keywords: 'roles security permissions rbac access control rights' },
            { label: 'System Settings & Security Policy', sub: 'Settings • Password policies, organization profile & configs', type: 'Settings', url: '/SuperAdmin/Settings', keywords: 'settings system configuration password policy organization' },
            { label: 'System Security Audit Log', sub: 'Audit • Immutable security audit trail & event history', type: 'Audit', url: '/SuperAdmin/AuditLog', keywords: 'audit log security logs events user activity trail compliance' },
            { label: 'SuperAdmin Notifications', sub: 'Notifications • System-wide health & security notifications', type: 'Notifications', url: '/SuperAdmin/Notifications', keywords: 'notifications security alerts superadmin system' }
        ]
    };

    // The active index is strictly limited to the current user's role
    const activeSearchIndex = roleSearchIndexes[currentRole] || roleSearchIndexes.Admin;

    function getMatches(query) {
        const q = query.toLowerCase().trim();
        if (!q) return [];
        return activeSearchIndex.filter(item =>
            item.label.toLowerCase().includes(q) ||
            item.sub.toLowerCase().includes(q) ||
            item.type.toLowerCase().includes(q) ||
            (item.keywords && item.keywords.toLowerCase().includes(q))
        ).slice(0, 8);
    }

    function checkOtherRolesMatch(query) {
        const q = query.toLowerCase().trim();
        if (!q) return [];
        const otherRoles = Object.keys(roleSearchIndexes).filter(r => r !== currentRole);
        const matches = [];
        for (const r of otherRoles) {
            const list = roleSearchIndexes[r];
            const found = list.filter(item =>
                item.label.toLowerCase().includes(q) ||
                item.sub.toLowerCase().includes(q) ||
                item.type.toLowerCase().includes(q) ||
                (item.keywords && item.keywords.toLowerCase().includes(q))
            );
            if (found.length > 0) {
                matches.push({ roleName: r, count: found.length, sample: found[0].label });
            }
        }
        return matches;
    }

    function renderResults(query) {
        const cleanQuery = query.trim();
        if (!cleanQuery) {
            dropdown.classList.remove('active');
            dropdown.innerHTML = '';
            if (clearBtn) clearBtn.style.display = 'none';
            return;
        }

        if (clearBtn) clearBtn.style.display = 'flex';
        const matches = getMatches(cleanQuery);

        if (matches.length === 0) {
            // Check if query matched other role domains that are restricted
            const restrictedMatches = checkOtherRolesMatch(cleanQuery);
            if (restrictedMatches.length > 0) {
                dropdown.innerHTML = `
                    <div class="dc-search-restricted-notice">
                        <div class="dc-search-denied-badge">
                            <i data-lucide="shield-alert" style="width:14px; height:14px;"></i>
                            <span>Role Restricted Resource</span>
                        </div>
                        <div class="dc-search-denied-text">
                            "<strong>${escapeHtml(cleanQuery)}</strong>" does not exist in your <strong>${portalName}</strong> portal.
                        </div>
                        <div class="dc-search-denied-sub">
                            Role-based access restrictions prevent locating and accessing resources outside your assigned role.
                        </div>
                    </div>
                `;
            } else {
                dropdown.innerHTML = `
                    <div class="dc-search-empty">
                        <i data-lucide="search-x" style="width:22px; height:22px; color:var(--dc-text-muted);"></i>
                        <div>No compliance records matching "<strong>${escapeHtml(cleanQuery)}</strong>" in ${portalName} portal.</div>
                    </div>
                `;
            }
        } else {
            let html = '<div class="dc-search-result-group">';
            html += `<div class="dc-search-result-header">${portalName} Portal Results (${matches.length})</div>`;
            matches.forEach(item => {
                const highlightedLabel = highlightMatch(item.label, cleanQuery);
                html += `
                    <a href="${item.url}" class="dc-search-result-item" data-url="${item.url}" data-label="${escapeHtml(item.label)}">
                        <div class="dc-search-item-info">
                            <span class="dc-search-item-title">${highlightedLabel}</span>
                            <span class="dc-search-item-sub">${item.sub}</span>
                        </div>
                        <span class="dc-search-item-type">${item.type}</span>
                    </a>
                `;
            });
            html += '</div>';
            dropdown.innerHTML = html;
        }

        if (window.lucide && typeof window.lucide.createIcons === 'function') {
            window.lucide.createIcons();
        }
        dropdown.classList.add('active');
    }

    function escapeHtml(str) {
        return (str || '').replace(/[&<>'"]/g, tag => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            "'": '&#39;',
            '"': '&quot;'
        }[tag] || tag));
    }

    function highlightMatch(text, query) {
        const idx = text.toLowerCase().indexOf(query.toLowerCase());
        if (idx === -1) return escapeHtml(text);
        const before = escapeHtml(text.substring(0, idx));
        const matched = escapeHtml(text.substring(idx, idx + query.length));
        const after = escapeHtml(text.substring(idx + query.length));
        return `${before}<mark>${matched}</mark>${after}`;
    }

    // Role-enforced navigation handler: BLOCKS cross-role bypass
    function navigateSafely(url, label) {
        if (!url) return false;

        const allowedPrefix = '/' + currentRole;
        // Verify URL belongs to current role
        const isPermitted = url.startsWith(allowedPrefix);

        if (!isPermitted) {
            dropdown.classList.remove('active');
            if (typeof showToast === 'function') {
                showToast(`Access Denied: "${label || url}" is restricted to other roles and cannot be accessed from your ${portalName} portal.`, 'warning');
            } else {
                alert(`Access Denied: You cannot access "${label || url}" because it is restricted to other roles.`);
            }
            return false;
        }

        dropdown.classList.remove('active');
        window.location.href = url;
        return true;
    }

    // Click handler for search items with role security verification
    dropdown.addEventListener('click', (e) => {
        const item = e.target.closest('.dc-search-result-item');
        if (!item) return;

        e.preventDefault();
        const url = item.getAttribute('data-url') || item.getAttribute('href');
        const label = item.getAttribute('data-label') || '';
        navigateSafely(url, label);
    });

    searchInput.addEventListener('input', (e) => {
        renderResults(e.target.value);
    });

    // Enter key handler: strictly executes within role scope or denies navigation
    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            const query = searchInput.value.trim();
            if (!query) return;

            const matches = getMatches(query);
            if (matches.length > 0) {
                navigateSafely(matches[0].url, matches[0].label);
            } else {
                const restrictedMatches = checkOtherRolesMatch(query);
                if (restrictedMatches.length > 0) {
                    if (typeof showToast === 'function') {
                        showToast(`Access Denied: "${query}" belongs to another role and cannot be opened from the ${portalName} portal.`, 'warning');
                    }
                } else {
                    if (typeof showToast === 'function') {
                        showToast(`No records found for "${query}" in the ${portalName} portal.`, 'info');
                    }
                }
            }
        } else if (e.key === 'Escape') {
            dropdown.classList.remove('active');
        }
    });

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            searchInput.value = '';
            renderResults('');
            searchInput.focus();
        });
    }

    document.addEventListener('click', (e) => {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.classList.remove('active');
        }
    });
}

/* ----------------------------------------------------
   3. PASSWORD TRACKER VALIDATION ENGINE
---------------------------------------------------- */
function initPasswordTrackers() {
    const passwordInputs = document.querySelectorAll('.dc-password-input');
    passwordInputs.forEach(input => {
        const trackerId = input.getAttribute('data-tracker');
        const confirmId = input.getAttribute('data-confirm');
        const submitBtnId = input.getAttribute('data-submit-btn');
        if (!trackerId) return;

        const tracker = document.getElementById(trackerId);
        const confirmInput = confirmId ? document.getElementById(confirmId) : null;
        const submitBtn = submitBtnId ? document.getElementById(submitBtnId) : null;
        const matchError = document.getElementById(trackerId + '-match-error');

        function validatePassword() {
            const val = input.value;
            const confirmVal = confirmInput ? confirmInput.value : '';

            const checks = {
                length: val.length >= 12,
                uppercase: /[A-Z]/.test(val),
                lowercase: /[a-z]/.test(val),
                number: /[0-9]/.test(val),
                special: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(val)
            };

            updateRuleUI(tracker, 'rule-length', checks.length);
            updateRuleUI(tracker, 'rule-uppercase', checks.uppercase);
            updateRuleUI(tracker, 'rule-lowercase', checks.lowercase);
            updateRuleUI(tracker, 'rule-number', checks.number);
            updateRuleUI(tracker, 'rule-special', checks.special);

            const allPassed = Object.values(checks).every(Boolean);
            let confirmPassed = true;

            if (confirmInput) {
                if (confirmVal && val !== confirmVal) {
                    confirmPassed = false;
                    if (matchError) matchError.style.display = 'block';
                } else {
                    if (matchError) matchError.style.display = 'none';
                    confirmPassed = confirmVal.length > 0 && val === confirmVal;
                }
            }

            if (submitBtn) {
                submitBtn.disabled = !(allPassed && confirmPassed);
            }
        }

        input.addEventListener('input', validatePassword);
        if (confirmInput) confirmInput.addEventListener('input', validatePassword);
    });
}

function updateRuleUI(tracker, ruleName, isMet) {
    if (!tracker) return;
    const item = tracker.querySelector(`[data-rule="${ruleName}"]`);
    if (!item) return;

    if (isMet) {
        item.classList.add('met');
        const icon = item.querySelector('.dc-tracker-icon');
        if (icon) icon.innerHTML = '✓';
    } else {
        item.classList.remove('met');
        const icon = item.querySelector('.dc-tracker-icon');
        if (icon) icon.innerHTML = '';
    }
}

/* ----------------------------------------------------
   4. MODAL SYSTEM ENGINE
---------------------------------------------------- */
function initModalSystem() {
    // Open Modal buttons
    document.querySelectorAll('[data-dc-modal-target]').forEach(trigger => {
        trigger.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = trigger.getAttribute('data-dc-modal-target');
            openDcModal(targetId);
        });
    });

    // Close Modal triggers
    document.querySelectorAll('.dc-modal-backdrop, .dc-modal-close, [data-dc-modal-dismiss]').forEach(el => {
        el.addEventListener('click', (e) => {
            if (e.target === el || el.classList.contains('dc-modal-close') || el.hasAttribute('data-dc-modal-dismiss')) {
                const modal = el.closest('.dc-modal-backdrop');
                if (modal) closeDcModal(modal.id);
            }
        });
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            document.querySelectorAll('.dc-modal-backdrop.show').forEach(m => closeDcModal(m.id));
        }
    });
}

function openDcModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.add('show');
    document.body.style.overflow = 'hidden';
    initLucideIcons();
}

function closeDcModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.remove('show');
    document.body.style.overflow = '';
}

/* ----------------------------------------------------
   5. NOTIFICATIONS SYSTEM ENGINE
---------------------------------------------------- */
function initNotificationSystem() {
    // Handled natively by ASP.NET Core Razor Pages forms and database
}

/* ----------------------------------------------------
   6. LOCAL TABLE SEARCH & PAGINATION ENGINE
---------------------------------------------------- */
function initLocalTableSearchAndPagination() {
    document.querySelectorAll('[data-dc-table-paginate]').forEach(tableContainer => {
        const tableId = tableContainer.getAttribute('data-dc-table-paginate');
        const searchInputId = tableContainer.getAttribute('data-dc-search-input');
        const itemsPerPage = parseInt(tableContainer.getAttribute('data-dc-per-page')) || 12;

        const table = document.getElementById(tableId);
        if (!table) return;

        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        const rows = Array.from(tbody.querySelectorAll('tr'));
        let filteredRows = [...rows];
        let currentPage = 1;

        const paginationBar = tableContainer.querySelector('.dc-pagination-bar');
        const searchInput = searchInputId ? document.getElementById(searchInputId) : null;

        function renderTable() {
            const totalItems = filteredRows.length;
            const totalPages = Math.ceil(totalItems / itemsPerPage) || 1;
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            const startIndex = (currentPage - 1) * itemsPerPage;
            const endIndex = Math.min(startIndex + itemsPerPage, totalItems);

            // Hide all rows then show only sliced rows
            rows.forEach(r => r.style.display = 'none');
            for (let i = startIndex; i < endIndex; i++) {
                if (filteredRows[i]) filteredRows[i].style.display = '';
            }

            // Update Pagination Bar UI
            if (paginationBar) {
                const infoText = paginationBar.querySelector('.dc-pagination-info');
                if (infoText) {
                    if (totalItems === 0) {
                        infoText.textContent = 'Showing 0–0 of 0';
                    } else {
                        infoText.textContent = `Showing ${startIndex + 1}–${endIndex} of ${totalItems}`;
                    }
                }

                const controlsContainer = paginationBar.querySelector('.dc-pagination-controls');
                if (controlsContainer) {
                    controlsContainer.innerHTML = generatePaginationButtons(currentPage, totalPages);
                    // Bind click handlers
                    controlsContainer.querySelectorAll('.dc-page-btn').forEach(btn => {
                        btn.addEventListener('click', () => {
                            const targetPage = parseInt(btn.getAttribute('data-page'));
                            if (!isNaN(targetPage)) {
                                currentPage = targetPage;
                                renderTable();
                            }
                        });
                    });
                }
            }
        }

        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                const query = e.target.value.toLowerCase().trim();
                filteredRows = rows.filter(row => row.textContent.toLowerCase().includes(query));
                currentPage = 1;
                renderTable();
            });
        }

        renderTable();
    });
}

function generatePaginationButtons(current, total) {
    let html = `<button class="dc-page-btn" data-page="${current - 1}" ${current <= 1 ? 'disabled' : ''}>Prev</button>`;

    const maxVisiblePages = 5;
    let start = Math.max(1, current - 2);
    let end = Math.min(total, start + maxVisiblePages - 1);

    if (end - start < maxVisiblePages - 1) {
        start = Math.max(1, end - maxVisiblePages + 1);
    }

    if (start > 1) {
        html += `<button class="dc-page-btn" data-page="1">1</button>`;
        if (start > 2) html += `<span style="padding: 0 4px; color: #94A3B8;">...</span>`;
    }

    for (let i = start; i <= end; i++) {
        html += `<button class="dc-page-btn ${i === current ? 'active' : ''}" data-page="${i}">${i}</button>`;
    }

    if (end < total) {
        if (end < total - 1) html += `<span style="padding: 0 4px; color: #94A3B8;">...</span>`;
        html += `<button class="dc-page-btn" data-page="${total}">${total}</button>`;
    }

    html += `<button class="dc-page-btn" data-page="${current + 1}" ${current >= total ? 'disabled' : ''}>Next</button>`;
    return html;
}

/* ----------------------------------------------------
   7. ROLE SWITCHERS (FOR LOGIN & PROTOTYPE DEMO)
---------------------------------------------------- */
function initRoleSwitchers() {
    const roleBtns = document.querySelectorAll('.dc-role-preset-btn');
    roleBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            roleBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            const role = btn.getAttribute('data-role');
            const usernameInput = document.getElementById('loginUsername');
            const passwordInput = document.getElementById('loginPassword');
            const targetRoleInput = document.getElementById('loginTargetRole');

            if (role && usernameInput && passwordInput) {
                switch (role) {
                    case 'SuperAdmin':
                        usernameInput.value = 'superadmin@cccorp.com';
                        passwordInput.value = 'SuperSecure123!';
                        if (targetRoleInput) targetRoleInput.value = 'SuperAdmin';
                        break;
                    case 'Administrator':
                        usernameInput.value = 'juan@cccorp.com';
                        passwordInput.value = 'AdminPass123!';
                        if (targetRoleInput) targetRoleInput.value = 'Administrator';
                        break;
                    case 'Staff':
                        usernameInput.value = 'hr@cccorp.com';
                        passwordInput.value = 'StaffPass123!';
                        if (targetRoleInput) targetRoleInput.value = 'Staff';
                        break;
                    case 'Supplier':
                        usernameInput.value = 'abc@company.com';
                        passwordInput.value = 'SupplierPass123!';
                        if (targetRoleInput) targetRoleInput.value = 'Supplier';
                        break;
                }
            }
        });
    });
}

/* ----------------------------------------------------
   8. DYNAMIC PROTOTYPE INTERACTION HANDLERS
---------------------------------------------------- */
function initDynamicPrototypeControls() {
    // 8a. Supplier Upload Document Modal - allow native form submission to OnPostUploadAsync

    // 8b. Administrator Approve & Reject Document Actions
    const approveDocBtn = document.getElementById('approveDocumentBtn');
    const rejectDocBtn = document.getElementById('rejectDocumentBtn');

    if (approveDocBtn) {
        approveDocBtn.addEventListener('click', () => {
            showToast('Document Approved. Supplier compliance updated to Valid.');
            setTimeout(() => {
                window.location.href = '/Admin/VerificationQueue';
            }, 1200);
        });
    }

    if (rejectDocBtn) {
        rejectDocBtn.addEventListener('click', () => {
            const reasonSection = document.getElementById('rejectionReasonSection');
            if (reasonSection && reasonSection.style.display === 'none') {
                reasonSection.style.display = 'block';
                return;
            }
            showToast('Document Rejected. Supplier notified automatically.');
            setTimeout(() => {
                window.location.href = '/Admin/VerificationQueue';
            }, 1200);
        });
    }

    // 8c. Expiry Monitoring Interactive State Controls
    document.querySelectorAll('[data-dc-expiry-state]').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('[data-dc-expiry-state]').forEach(b => b.classList.remove('dc-btn-primary'));
            btn.classList.add('dc-btn-primary');

            const state = btn.getAttribute('data-dc-expiry-state');
            const targetChip = document.getElementById('expiryDemoStatusChip');
            const targetTime = document.getElementById('expiryDemoTimeRemaining');
            const targetBadge = document.getElementById('expiryDemoComplianceBadge');

            if (state === 'Valid') {
                if (targetChip) targetChip.className = 'dc-status-chip valid';
                if (targetChip) targetChip.innerHTML = '<span class="dc-status-dot"></span> Valid';
                if (targetTime) targetTime.textContent = 'Valid for 120 days';
                if (targetBadge) {
                    targetBadge.innerHTML = `
                        <div class="dc-compliance-badge compliant">
                            <span class="dc-status-dot"></span> COMPLIANT
                        </div>
                    `;
                }
            } else if (state === 'Expiring Soon') {
                if (targetChip) targetChip.className = 'dc-status-chip expiring-soon';
                if (targetChip) targetChip.innerHTML = '<span class="dc-status-dot"></span> Expiring Soon';
                if (targetTime) targetTime.textContent = 'Expires in 30 days';
                if (targetBadge) {
                    targetBadge.innerHTML = `
                        <div class="dc-compliance-badge compliant">
                            <span class="dc-status-dot"></span> COMPLIANT
                        </div>
                        <div class="dc-compliance-reasons">1 document expiring soon</div>
                    `;
                }
            } else if (state === 'Expired') {
                if (targetChip) targetChip.className = 'dc-status-chip expired';
                if (targetChip) targetChip.innerHTML = '<span class="dc-status-dot"></span> Expired';
                if (targetTime) targetTime.textContent = 'Expired 2 days ago';
                if (targetBadge) {
                    targetBadge.innerHTML = `
                        <div class="dc-compliance-badge not-compliant">
                            <span class="dc-status-dot"></span> NOT COMPLIANT
                        </div>
                        <div class="dc-compliance-reasons">1 expired document</div>
                    `;
                }
            }
        });
    });

    // 8d. Supplier Portal Prototype State Quick Switchers
    document.querySelectorAll('[data-dc-supplier-preset]').forEach(btn => {
        btn.addEventListener('click', () => {
            const preset = btn.getAttribute('data-dc-supplier-preset');
            applySupplierPortalPreset(preset);
        });
    });
}

function applySupplierPortalPreset(preset) {
    const table = document.getElementById('supplierMyComplianceTable');
    const badgeContainer = document.getElementById('supplierOverallBadgeContainer');
    if (!table || !badgeContainer) return;

    const tbody = table.querySelector('tbody');
    if (!tbody) return;

    if (preset === 'all-missing') {
        badgeContainer.innerHTML = `
            <div class="dc-compliance-badge not-compliant">
                <span class="dc-status-dot"></span> NOT COMPLIANT
            </div>
            <div class="dc-compliance-reasons">5 missing documents</div>
        `;
        tbody.innerHTML = `
            <tr data-doc-row="Business Permit">
                <td><strong>Business Permit</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Not Submitted</span></td>
                <td class="dc-expiry-cell">—</td>
                <td class="dc-status-cell"><span class="dc-status-chip missing"><span class="dc-status-dot"></span> Missing</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('Business Permit')">Upload</button></td>
            </tr>
            <tr data-doc-row="BIR Registration">
                <td><strong>BIR Registration</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Not Submitted</span></td>
                <td class="dc-expiry-cell">No expiry</td>
                <td class="dc-status-cell"><span class="dc-status-chip missing"><span class="dc-status-dot"></span> Missing</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('BIR Registration')">Upload</button></td>
            </tr>
            <tr data-doc-row="Tax Clearance">
                <td><strong>Tax Clearance</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Not Submitted</span></td>
                <td class="dc-expiry-cell">—</td>
                <td class="dc-status-cell"><span class="dc-status-chip missing"><span class="dc-status-dot"></span> Missing</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('Tax Clearance')">Upload</button></td>
            </tr>
            <tr data-doc-row="Safety Certificate">
                <td><strong>Safety Certificate</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Not Submitted</span></td>
                <td class="dc-expiry-cell">—</td>
                <td class="dc-status-cell"><span class="dc-status-chip missing"><span class="dc-status-dot"></span> Missing</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('Safety Certificate')">Upload</button></td>
            </tr>
            <tr data-doc-row="Data Privacy Agreement">
                <td><strong>Data Privacy Agreement</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Not Submitted</span></td>
                <td class="dc-expiry-cell">No expiry</td>
                <td class="dc-status-cell"><span class="dc-status-chip missing"><span class="dc-status-dot"></span> Missing</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('Data Privacy Agreement')">Upload</button></td>
            </tr>
        `;
    } else if (preset === 'all-valid') {
        badgeContainer.innerHTML = `
            <div class="dc-compliance-badge compliant">
                <span class="dc-status-dot"></span> COMPLIANT
            </div>
            <div class="dc-compliance-reasons">All 5 required documents verified & valid</div>
        `;
        tbody.innerHTML = `
            <tr>
                <td><strong>Business Permit</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>Dec 31, 2026</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>BIR Registration</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>No expiry</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>Tax Clearance</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>Nov 30, 2026</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>Safety Certificate</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>Dec 15, 2026</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>Data Privacy Agreement</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>No expiry</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">Verified ✓</span></td>
            </tr>
        `;
    } else if (preset === 'one-rejected') {
        badgeContainer.innerHTML = `
            <div class="dc-compliance-badge not-compliant">
                <span class="dc-status-dot"></span> NOT COMPLIANT
            </div>
            <div class="dc-compliance-reasons">1 rejected document</div>
        `;
        tbody.innerHTML = `
            <tr>
                <td><strong>Business Permit</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>Dec 31, 2026</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>BIR Registration</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>No expiry</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption">Verified ✓</span></td>
            </tr>
            <tr>
                <td><strong>Tax Clearance</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>Nov 30, 2026</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption">Verified ✓</span></td>
            </tr>
            <tr data-doc-row="Safety Certificate">
                <td><strong>Safety Certificate</strong></td>
                <td><span class="dc-caption" style="color:#C62828;">✗ Rejected</span></td>
                <td>Dec 15, 2026</td>
                <td class="dc-status-cell"><span class="dc-status-chip rejected"><span class="dc-status-dot"></span> Rejected</span></td>
                <td class="dc-action-cell"><button class="dc-btn dc-btn-secondary dc-btn-sm" onclick="openUploadModal('Safety Certificate')">Re-upload</button></td>
            </tr>
            <tr>
                <td><strong>Data Privacy Agreement</strong></td>
                <td><span class="dc-caption" style="color:#1F8A4C;">✓ Submitted</span></td>
                <td>No expiry</td>
                <td><span class="dc-status-chip valid"><span class="dc-status-dot"></span> Valid</span></td>
                <td><span class="dc-caption">Verified ✓</span></td>
            </tr>
        `;
    }
}

function openUploadModal(docName) {
    const sel = document.getElementById('uploadDocNameSelect');
    if (sel) sel.value = docName;
    openDcModal('supplierUploadModal');
}

// Simple Toast Notification
function showToast(message) {
    let container = document.getElementById('dcToastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'dcToastContainer';
        container.style.cssText = 'position:fixed; bottom:24px; right:24px; z-index:3000; display:flex; flex-direction:column; gap:8px;';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.style.cssText = 'background:#12356B; color:#FFFFFF; padding:12px 20px; border-radius:8px; font-size:14px; box-shadow:0 4px 12px rgba(0,0,0,0.15); animation: fadeIn 0.2s ease;';
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}
