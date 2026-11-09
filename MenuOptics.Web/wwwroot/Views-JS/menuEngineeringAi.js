// menuEngineeringAi.js — Menu Engineering AI page scripts.

let currentFullAiReportData = null;
let aiAnalysisInProgress = false;
let currentMenuPdfAudit = null;
let currentSuggestionImages = [];
let menuPdfAnalysisInProgress = false;
let aiProgress = null;
let auditProgress = null;
let currentDesignMenu = null;
let currentDesignTemplate = 'midnight';
let lastDesignImageUrl = null;

// TEMPORARY (client request, 2026-11-04): the 'Menu Design & Psychology'
// section is hidden in the report POPUP too. Set to true to restore it.
const SHOW_DESIGN_SECTION_POPUP = false;

$(document).ready(function () {
    $('#btnOpenAiInsightsModal').on('click', function () {
        openFullAiInsightsModal();
    });

    $('#btnOpenMenuAuditModal').on('click', function () {
        openMenuAuditModal();
    });

    $('#btnDownloadAiPdfModal, #btnDownloadAiPdfFooter').on('click', function () {
        if (currentFullAiReportData) {
            exportAiInsightsPdf(currentFullAiReportData);
        } else {
            showNotification('Report data is still loading. Please wait.', 'warning');
        }
    });
});

// Small staged progress bar: creeps towards 90% with rotating step labels
// while the real work runs, completes when done() is called.
function aiProgressHtml(wrapId) {
    return `<div id="${wrapId}" style="max-width: 520px; margin: 16px auto 0;">
        <div style="height: 8px; background: #e5e7eb; border-radius: 99px; overflow: hidden;">
            <div class="ai-progress-bar" style="height: 100%; width: 0%; background: #f08d11; border-radius: 99px; transition: width 0.6s ease;"></div>
        </div>
        <div class="ai-progress-label" style="font-size: 12.5px; color: #4b5563; margin-top: 8px;">Starting...</div>
    </div>`;
}

function startAiProgress($wrap, steps) {
    let pct = 0;
    let stepIdx = 0;
    const $bar = $wrap.find('.ai-progress-bar');
    const $label = $wrap.find('.ai-progress-label');
    $bar.css('width', '0%');
    if (steps && steps.length) $label.text(steps[0]);
    const timer = setInterval(() => {
        if (pct < 90) {
            pct = Math.min(90, pct + Math.max(0.5, (90 - pct) * 0.05));
            $bar.css('width', pct + '%');
        }
    }, 700);
    const stepTimer = setInterval(() => {
        if (steps && steps.length) {
            stepIdx = (stepIdx + 1) % steps.length;
            $label.text(steps[stepIdx]);
        }
    }, 5000);
    return {
        done: function () { clearInterval(timer); clearInterval(stepTimer); $bar.css('width', '100%'); },
        stop: function () { clearInterval(timer); clearInterval(stepTimer); }
    };
}

function openFullAiInsightsModal() {
    openCenteredModal('aiInsightsModal');
    $('#aiLoadingIndicator').hide();

    buildAiStartPanel();
    $('#aiStartPanel').show();

    // Show an existing report/audit from this session under the panel,
    // so the user can still re-run the analysis or upload another file.
    if (currentFullAiReportData || currentMenuPdfAudit) {
        $('#aiReportContainer').show();
    } else {
        $('#aiReportContainer').hide();
    }
}

// The panel shown BEFORE the analysis starts: the info message, the menu
// upload option, and the Start button — so the user gets the chance to
// upload their menu file first instead of the analysis starting at once.
function buildAiStartPanel() {
    if ($('#aiStartPanel').length > 0) return;

    $('#aiLoadingIndicator').before(`
        <div id="aiStartPanel" style="display: none; padding: 4px 0 8px;">
            <div class="ai-section-box" style="border-left: 4px solid #3b82f6; background: #eff6ff; max-width: 680px; margin: 0 auto 12px; text-align: left;">
                <div class="ai-section-title"><i class="fas fa-file-pdf" style="color: #2563eb;"></i> Menu Design Audit — From Your Actual Menu</div>
                <p style="font-size: 13px; color: #374151; margin: 0;">
                    This report is based on your sales data. You can also upload your restaurant's menu PDF, and our AI will analyze the real menu your guests see — its layout, item placement, prices and descriptions — against 14 research-backed menu design principles — for that, use the separate Menu Design Audit button in the page header.
                    <strong>Please note: the menu PDF analysis may take a few minutes to complete.</strong>
                </p>
            </div>
            <div style="max-width: 680px; margin: 0 auto; text-align: center;">
                <button type="button" class="btn btn-primary" id="btnStartAiAnalysis" style="padding: 10px 28px; font-size: 14px;"><i class="fas fa-brain"></i> Start AI Analysis</button>
                <p style="font-size: 12px; color: #6b7280; margin-top: 8px;">Runs the sales-data analysis for the selected dates. This may take a few minutes.</p>
            </div>
        </div>
    `);
}

$(document).on('click', '#btnStartAiAnalysis', function () {
    startFullAiAnalysis();
});

function startFullAiAnalysis() {
    $('#aiStartPanel').hide();
    $('#aiReportContainer').hide();
    $('#aiLoadingIndicator').show();

    // Short wait note while the analysis runs
    if ($('#aiAnalysisWaitMsg').length === 0) {
        $('#aiLoadingIndicator').append(`
            <p id="aiAnalysisWaitMsg" style="font-size: 13px; color: #4b5563; max-width: 640px; margin: 14px auto 0;">
                Analyzing your menu data — <strong>this may take a few minutes</strong>. Please keep this window open.
            </p>
        `);
    }

    aiAnalysisInProgress = true;

    if ($('#aiAnalysisProgress').length === 0) {
        $('#aiLoadingIndicator').append(aiProgressHtml('aiAnalysisProgress'));
    }
    aiProgress = startAiProgress($('#aiAnalysisProgress'), [
        'Collecting your sales data...',
        'Analyzing food costs and popularity...',
        'Checking margins and menu quadrants...',
        'Writing recommendations and your checklist...'
    ]);

    let url = `/api/MenuEngineeringInsightsApi/GetAiAnalysis?`;
    if (typeof startDate !== 'undefined' && startDate) url += `startDate=${startDate}&`;
    if (typeof endDate !== 'undefined' && endDate) url += `endDate=${endDate}`;

    ajaxRequest({
        url: url,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            if (response && response.status === 200 && response.data) {
                currentFullAiReportData = response.data;
                renderFullAiReport(response.data);
            } else {
                aiAnalysisInProgress = false;
                if (aiProgress) { aiProgress.stop(); aiProgress = null; }
                $('#aiLoadingIndicator').hide();
                $('#aiStartPanel').show();
                showNotification(response?.responseMsg || "Failed to generate AI insights.", "error");
            }
        },
        onError: function (xhr) {
            aiAnalysisInProgress = false;
            if (aiProgress) { aiProgress.stop(); aiProgress = null; }
            $('#aiLoadingIndicator').hide();
            $('#aiStartPanel').show();
            const msg = xhr.responseJSON?.responseMsg || "Unable to generate AI analysis. Please ensure menu items are added.";
            showNotification(msg, "error");
        }
    });
}

function renderFullAiReport(data) {
    aiAnalysisInProgress = false;
    if (aiProgress) { aiProgress.done(); aiProgress = null; }
    $('#aiModalModelBadge').html(`<i class="fas fa-microchip"></i> ${data.sourceModel || 'MenuOptics AI'}`);
    $('#aiDateRangeBadge').text(data.dateRange || 'Last 30 Days');
    $('#aiLocationBadge').html(`<i class="fas fa-map-marker-alt" style="color:#ef4444;"></i> ${data.location || 'Trade Area'}`);

    const summary = data.executiveSummary || {};
    $('#aiHealthScore').text(`${summary.healthScore || 0}/100`);
    $('#aiProfitUplift').text(`+$${(summary.estimatedMonthlyProfitUplift || 0).toFixed(2)}/mo`);
    $('#aiAvgFoodCost').text(`${(summary.averageFoodCostPct || 0).toFixed(1)}%`);
    $('#aiAnalyzedCount').text(data.totalMenuItemsAnalyzed || 0);

    $('#aiCompetitorOverview').text(data.competitorMarketOverview || 'Area competitor pricing benchmark indicates healthy margin expansion opportunity.');

    $('#aiExecutiveTakeaway').text(summary.keyTakeaway || 'Execute menu rebalancing, price adjustments, and recipe trimming.');
    $('#aiTopStrength').text(summary.topStrength || '--');
    $('#aiCriticalRisk').text(summary.criticalRisk || '--');

    const q = data.quadrantBreakdown || {};
    $('#qStarCount').text(`${q.starsCount || 0} items`);
    $('#qStarRev').text(`${(q.starsRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qPlowCount').text(`${q.plowhorsesCount || 0} items`);
    $('#qPlowRev').text(`${(q.plowhorsesRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qPuzzleCount').text(`${q.puzzlesCount || 0} items`);
    $('#qPuzzleRev').text(`${(q.puzzlesRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qDogCount').text(`${q.dogsCount || 0} items`);
    $('#qDogRev').text(`${(q.dogsRevenueShare || 0).toFixed(1)}% revenue`);

    const $remTbody = $('#aiRemovalTable tbody');
    $remTbody.empty();
    const removals = data.removalRecommendations || [];
    if (removals.length === 0) {
        $remTbody.append(`<tr><td colspan="7" style="text-align:center; color:#6b7280; padding:16px;">No immediate menu removals required. All items meet operational velocity.</td></tr>`);
    } else {
        removals.forEach(r => {
            $remTbody.append(`
                <tr>
                    <td><strong>${r.name || 'Item'}</strong></td>
                    <td>$${(r.sellingPrice || 0).toFixed(2)}</td>
                    <td>$${(r.cost || 0).toFixed(2)} <span class="badge-pill danger">${(r.foodCostPct || 0).toFixed(1)}% FC</span></td>
                    <td>${r.unitsSold || 0} sold</td>
                    <td><span class="quadrant-tag dog">${getQuadrantIcon(r.quadrant)} ${r.quadrant || 'Dog'}</span></td>
                    <td>
                        <div style="font-size:12px; color:#991b1b; font-weight:600;"><i class="fas fa-exclamation-circle"></i> ${r.primaryReason || ''}</div>
                        <div style="font-size:12px; color:#4b5563; margin-top:3px;"><i class="fas fa-arrow-right"></i> ${r.actionRecommendation || ''}</div>
                    </td>
                    <td style="color:#059669; font-weight:700;">+$${(r.estimatedMonthlyLaborOrWasteSavings || 0).toFixed(2)}/mo</td>
                </tr>
            `);
        });
    }

    const titles = data.titleImprovements || [];
    const $titleSection = $('#aiTitleImprovementSection');
    const $titleTbody = $('#aiTitleTable tbody');
    $titleTbody.empty();

    if (titles.length > 0) {
        titles.forEach(t => {
            $titleTbody.append(`
                <tr>
                    <td><span style="color:#6b7280; text-decoration:line-through; font-weight:500;">${t.currentTitle || ''}</span></td>
                    <td><strong style="color:#7e22ce; font-size:14px;"><i class="fas fa-magic" style="color:#f08d11; margin-right:4px;"></i> ${t.suggestedTitle || ''}</strong></td>
                    <td><span class="badge-pill" style="background:#f3e8ff; color:#6b21a8; font-size:11px;"><i class="fas fa-tag"></i> ${t.trendVerbiageUsed || 'Modern Culinary'}</span></td>
                    <td style="font-size:12px; color:#374151;">${t.reasoning || ''}</td>
                </tr>
            `);
        });
        $titleSection.show();
    } else {
        $titleSection.hide();
    }

    const $priceTbody = $('#aiPricingTable tbody');
    $priceTbody.empty();
    const pricing = data.pricingOptimizations || [];
    if (pricing.length === 0) {
        $priceTbody.append(`<tr><td colspan="6" style="text-align:center; color:#6b7280; padding:16px;">Current pricing is aligned across major categories.</td></tr>`);
    } else {
        pricing.forEach(p => {
            $priceTbody.append(`
                <tr>
                    <td><strong>${p.name || 'Item'}</strong></td>
                    <td>$${(p.currentPrice || 0).toFixed(2)}</td>
                    <td style="color:#059669; font-weight:700; font-size:14px;">$${(p.recommendedPrice || 0).toFixed(2)}</td>
                    <td>
                        <div style="font-size:12px; color:#374151;">$${(p.currentMargin || 0).toFixed(2)} &rarr; <strong style="color:#059669;">$${(p.projectedMargin || 0).toFixed(2)}</strong></div>
                    </td>
                    <td style="color:#059669; font-weight:700;">+$${(p.estimatedMonthlyProfitIncrease || 0).toFixed(2)}/mo</td>
                    <td>
                        <div style="margin-bottom:3px;"><span class="badge-pill" style="background:#ecfdf5; color:#065f46; font-size:10.5px;"><i class="fas fa-balance-scale"></i> ${p.pricingPrinciple || 'Psychological Charm Pricing'}</span></div>
                        <div style="font-size:12px; color:#4b5563;">${p.rationale || ''}</div>
                    </td>
                </tr>
            `);
        });
    }

    renderMenuDesignRecommendations(data);

    const $timeline = $('#aiTimelineContainer');
    $timeline.empty();
    const checklist = data.menuPlanningChecklist || [];
    if (checklist.length === 0) {
        $timeline.append(`<div class="timeline-item"><div class="timeline-desc">No checklist items — the menu is already well planned.</div></div>`);
    } else {
        checklist.forEach(item => {
            $timeline.append(`
            <label class="timeline-item" style="display: flex; gap: 10px; align-items: flex-start; cursor: pointer;">
                <input type="checkbox" style="margin-top: 3px; width: 16px; height: 16px; flex-shrink: 0; accent-color: #f08d11;">
                <span style="flex: 1;">
                    <span class="timeline-header" style="display: block;">${escapeHtml(item.task || '')}
                        ${item.area ? `<span class="badge-pill" style="background: #eef2ff; color: #4338ca; font-size: 10.5px; margin-left: 6px;">${escapeHtml(item.area)}</span>` : ''}
                    </span>
                    ${item.details ? `<span class="timeline-desc" style="display: block;">${escapeHtml(item.details)}</span>` : ''}
                </span>
            </label>
        `);
        });
    }

    $('#aiLoadingIndicator').hide();
    $('#aiReportContainer').fadeIn(200);
}

function renderMenuDesignRecommendations(data) {
    $('#aiDesignSection').remove();

    if (!SHOW_DESIGN_SECTION_POPUP) return;

    const recs = data.menuDesignRecommendations || [];
    if (recs.length === 0) return;

    const cards = recs.map(rec => `
        <div class="timeline-item">
            <div class="timeline-header">
                ${escapeHtml(rec.itemName || rec.category || 'Whole Menu')}
                <span class="badge-pill success" style="margin-left: 8px;"><i class="fas fa-brain"></i> ${escapeHtml(rec.principle || 'Menu Psychology')}</span>
            </div>
            <div class="timeline-desc">${escapeHtml(rec.recommendation || '')}</div>
            <div class="timeline-desc" style="color:#6b7280;">${escapeHtml(rec.reasoning || '')}</div>
            ${rec.expectedImpact ? `<div class="timeline-impact"><i class="fas fa-arrow-trend-up"></i> Expected Impact: <strong>${escapeHtml(rec.expectedImpact)}</strong></div>` : ''}
        </div>`).join('');

    const sectionHtml = `
        <div class="ai-section-box" id="aiDesignSection">
            <div class="ai-section-title">
                <span><i class="fas fa-brain" style="color: #9333ea;"></i> Menu Design &amp; Psychology</span>
                <span class="ai-badge">${recs.length} recommendation${recs.length === 1 ? '' : 's'}</span>
            </div>
            <p style="font-size: 12px; color: #6b7280; margin: 0 0 12px 0;">Placement, formatting and wording advice from Aaron Allen &amp; Associates — \u201CThe Psychology of Menu Design\u201D.</p>
            <div class="ai-action-timeline">
                ${cards}
            </div>
        </div>`;

    const $planBox = $('#aiTimelineContainer').closest('.ai-section-box');
    if ($planBox.length) {
        $planBox.before(sectionHtml);
    } else {
        $('#aiReportContainer').append(sectionHtml);
    }
}

// ================= Menu PDF / Image Design Audit =================

$(document).on('change', '#menuPdfFileInput', function () {
    const file = this.files && this.files[0];
    this.value = '';
    if (file) startMenuPdfAnalysis(file);
});

function setMenuAuditState(state) {
    $('#menuAuditUploadState').toggle(state === 'upload');
    $('#menuAuditProgressState').toggle(state === 'progress');
    $('#menuAuditReportContainer').toggle(state === 'report');
}

function openMenuAuditModal() {
    buildMenuAuditModal();
    openCenteredModal('menuAuditModal');
    if (menuPdfAnalysisInProgress) setMenuAuditState('progress');
    else if (currentMenuPdfAudit) setMenuAuditState('report');
    else setMenuAuditState('upload');
}

function buildMenuAuditModal() {
    if ($('#menuAuditModal').length > 0) return;

    $('body').append(`
    <div class="modal ai-modal-lg" id="menuAuditModal" style="display: none;">
        <div class="modal-content">
            <div class="ai-modal-header">
                <div style="display: flex; align-items: center; gap: 14px;">
                    <img src="/images/menu optics_logo.png" alt="Menu Optics" style="height: 38px; object-fit: contain;" />
                    <div>
                        <h3 style="margin: 0 0 4px 0; font-size: 18px; color: #111827; display: flex; align-items: center; gap: 8px;">
                            <i class="fas fa-file-magnifying-glass" style="color: #f08d11;"></i> Menu Design Audit
                        </h3>
                        <div style="display: flex; gap: 6px; align-items: center;">
                            <span class="ai-badge"><i class="fas fa-file-pdf"></i> From your actual menu file</span>
                        </div>
                    </div>
                </div>
                <div style="display: flex; align-items: center; gap: 10px;">
                    <button class="btn btn-primary btnDownloadMenuAuditPdf" disabled style="opacity: 0.55;">
                        <i class="fas fa-file-pdf"></i> Download PDF Report
                    </button>
                    <button class="modal-close" onclick="closeCenteredModal('menuAuditModal')">&times;</button>
                </div>
            </div>

            <div class="ai-modal-body">
                <div id="menuAuditUploadState">
                    <div class="ai-section-box" style="max-width: 680px; margin: 8px auto 0; text-align: left;">
                        <div class="ai-section-title"><i class="fas fa-upload" style="color: #f08d11;"></i> Upload Your Menu (PDF or Image)</div>
                        <p style="font-size: 13px; color: #374151; margin: 0 0 10px;">
                            Choose your menu PDF or a photo/scan of your menu. Our AI will analyze the real menu your guests see — its layout, item placement, prices and descriptions — against 14 research-backed menu design principles, and create annotated suggestion images you can download.
                            <strong>Please note: this analysis may take a few minutes to complete.</strong>
                        </p>
                        <label class="btn btn-primary" for="menuPdfFileInput" style="cursor: pointer;"><i class="fas fa-file-upload"></i> Choose Menu File</label>
                        <input type="file" id="menuPdfFileInput" accept="application/pdf,image/jpeg,image/png,image/webp,image/gif" style="display: none;">
                        <div id="aiMenuUploadStatus" style="font-size: 12.5px; color: #4b5563; margin-top: 8px;"></div>
                    </div>
                    <div style="max-width: 680px; margin: 18px auto 0; text-align: center;">
                        <div style="display: flex; align-items: center; gap: 12px; color: #9ca3af; font-size: 12px; margin-bottom: 10px;">
                            <span style="flex: 1; height: 1px; background: #e5e7eb;"></span> OR <span style="flex: 1; height: 1px; background: #e5e7eb;"></span>
                        </div>
                        <p style="font-size: 13px; color: #374151; margin: 0 0 10px;">No menu file handy? We can build a fresh premium menu design from the items already in your MenuOptics account.</p>
                        <button type="button" class="btn btn-outline" id="btnDesignFromSystemMenu"><i class="fas fa-wand-magic-sparkles"></i> Generate Design From My Menu Items</button>
                        <div id="aiSystemMenuStatus" style="font-size: 12.5px; color: #4b5563; margin-top: 8px;"></div>
                    </div>
                </div>

                <div id="menuAuditProgressState" class="ai-loading-container" style="display: none;">
                    <i class="fas fa-spinner ai-spinner"></i>
                    <h4 style="margin: 0 0 6px 0; color: #111827;">Auditing Your Menu Design...</h4>
                    <p style="margin: 0; color: #6b7280; font-size: 13px;">Our AI is reading your menu pages. Please keep this window open.</p>
                    ${aiProgressHtml('menuAuditProgress')}
                </div>

                <div id="menuAuditReportContainer" style="display: none;"></div>
            </div>

            <div class="ai-modal-footer">
                <button class="btn btn-outline" onclick="closeCenteredModal('menuAuditModal')">Close</button>
                <button class="btn btn-primary btnDownloadMenuAuditPdf" disabled style="opacity: 0.55;">
                    <i class="fas fa-file-pdf"></i> Download PDF Report
                </button>
            </div>
        </div>
    </div>
    `);
}


// =============== Recreated Premium Menu Design ===============

$(document).on('click', '#btnDesignFromSystemMenu', function () {
    generateDesignFromSystemMenu();
});

$(document).on('click', '.design-template-btn', function () {
    drawRecreatedMenu($(this).data('template'));
});

$(document).on('click', '#btnDownloadRecreatedMenu', function () {
    downloadRecreatedMenuImage();
});

function generateDesignFromSystemMenu() {
    if (menuPdfAnalysisInProgress) return;
    const $status = $('#aiSystemMenuStatus');
    $status.html('<i class="fas fa-spinner fa-spin"></i> Loading your menu items...');
    ajaxRequest({
        url: '/api/MenuEngineeringInsightsApi/GetMenuDesignData',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            if (response && response.status === 200 && response.data) {
                currentDesignMenu = response.data;
                currentMenuPdfAudit = null;
                currentSuggestionImages = [];
                lastDesignImageUrl = null;
                $status.html('');
                $('#menuAuditReportContainer').empty();
                renderMenuDesignSection();
                setMenuAuditState('report');
                $('.btnDownloadMenuAuditPdf').prop('disabled', true).css('opacity', '0.55');
                showNotification('Menu design generated from your items.', 'success');
            } else {
                $status.html(`<span style="color:#dc2626;">${escapeHtml(response?.responseMsg || 'Could not load your menu items.')}</span>`);
            }
        },
        onError: function (xhr) {
            $status.html(`<span style="color:#dc2626;">${escapeHtml(xhr.responseJSON?.responseMsg || 'Could not load your menu items.')}</span>`);
        }
    });
}

function renderMenuDesignSection() {
    const menu = currentDesignMenu;
    if (!menu || !(menu.sections || []).length) return;

    if ($('#menuDesignSection').length === 0) {
        $('#menuAuditReportContainer').append('<div id="menuDesignSection"></div>');
    }

    const fromUpload = !!currentMenuPdfAudit;
    $('#menuDesignSection').html(`
        <div class="ai-section-box" style="border-left: 4px solid #d4a24e; margin-top: 16px;">
            <div class="ai-section-title"><i class="fas fa-wand-magic-sparkles" style="color: #d4a24e;"></i> Your Menu, Recreated — Premium Design</div>
            <p style="font-size: 13px; color: #374151; margin: 0 0 10px;">A brand-new premium menu design generated from ${fromUpload ? 'your uploaded menu' : 'your MenuOptics items'}. Pick a style, then download it as an image.</p>
            <div style="display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap;">
                <button type="button" class="btn btn-outline btn-sm design-template-btn" data-template="midnight">Midnight Gold</button>
                <button type="button" class="btn btn-outline btn-sm design-template-btn" data-template="modern">Modern Light</button>
                <button type="button" class="btn btn-primary btn-sm" id="btnDownloadRecreatedMenu" style="margin-left: auto;"><i class="fas fa-download"></i> Download Menu Image</button>
            </div>
            <div id="recreatedMenuPreview" style="overflow: auto;"></div>
        </div>
    `);
    drawRecreatedMenu(currentDesignTemplate);
}

const MENU_TEMPLATES = {
    midnight: { bg: '#14100d', ink: '#f5ead9', sub: '#c9b18c', accent: '#d4a24e', price: '#d4a24e', divider: '#4a3820', head: "Georgia, 'Times New Roman', serif", body: "Georgia, 'Times New Roman', serif" },
    modern: { bg: '#fffaf2', ink: '#1f2937', sub: '#6b7280', accent: '#f08d11', price: '#ea580c', divider: '#e8cf9f', head: "'Segoe UI', Arial, sans-serif", body: "'Segoe UI', Arial, sans-serif" }
};

function drawRecreatedMenu(templateKey) {
    const menu = currentDesignMenu;
    if (!menu) return;
    currentDesignTemplate = templateKey;
    const t = MENU_TEMPLATES[templateKey] || MENU_TEMPLATES.midnight;

    const sections = (menu.sections || []).map(sec => `
        <div style="margin-top: 28px;">
            <div style="text-align: center; font-family: ${t.head}; letter-spacing: 3px; font-size: 21px; font-weight: 700; color: ${t.accent}; text-transform: uppercase;">${escapeHtml(sec.name || 'Menu')}</div>
            <div style="height: 2px; background: ${t.accent}; opacity: 0.55; margin: 9px auto 6px; width: 120px;"></div>
            ${(sec.items || []).map(it => `
                <div style="display: flex; align-items: baseline; gap: 8px; padding: 7px 2px;">
                    <div>
                        <span style="font-family: ${t.body}; font-size: 15.5px; font-weight: 600; color: ${t.ink};">${escapeHtml(it.name || '')}</span>
                        ${it.description ? `<div style="font-size: 12px; color: ${t.sub}; font-style: italic; margin-top: 1px;">${escapeHtml(it.description)}</div>` : ''}
                    </div>
                    <div style="flex: 1; border-bottom: 2px dotted ${t.divider};"></div>
                    <div style="font-family: ${t.body}; font-size: 15px; font-weight: 700; color: ${t.price}; white-space: nowrap;">${escapeHtml(it.priceText || '')}</div>
                </div>`).join('')}
        </div>`).join('');

    $('#recreatedMenuPreview').html(`
        <div id="recreatedMenuSheet" style="background: ${t.bg}; padding: 38px 42px 30px; border-radius: 6px; max-width: 760px; margin: 0 auto;">
            <div style="text-align: center;">
                <div style="font-family: ${t.head}; font-size: 13px; letter-spacing: 5px; color: ${t.sub}; text-transform: uppercase;">Welcome to</div>
                <div style="font-family: ${t.head}; font-size: 34px; font-weight: 700; color: ${t.ink}; margin-top: 4px;">${escapeHtml(menu.restaurantName || 'Our Restaurant')}</div>
                <div style="height: 3px; background: ${t.accent}; width: 180px; margin: 12px auto 0;"></div>
            </div>
            ${sections}
            <div style="text-align: center; margin-top: 30px; font-size: 11.5px; letter-spacing: 2px; color: ${t.sub};">THANK YOU — PLEASE COME AGAIN</div>
        </div>
    `);

    $('.design-template-btn').each(function () {
        const active = $(this).data('template') === templateKey;
        $(this).css({ 'border-color': active ? '#f08d11' : '#d1d5db', 'background': active ? '#fff7e8' : '#ffffff', 'font-weight': active ? '700' : '400' });
    });
}

let html2canvasPromise = null;
function loadHtml2Canvas() {
    if (window.html2canvas) return Promise.resolve(window.html2canvas);
    if (html2canvasPromise) return html2canvasPromise;
    html2canvasPromise = new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
        s.onload = () => resolve(window.html2canvas);
        s.onerror = reject;
        document.head.appendChild(s);
    });
    return html2canvasPromise;
}

async function downloadRecreatedMenuImage() {
    const sheet = document.getElementById('recreatedMenuSheet');
    if (!sheet) return;
    try {
        const html2canvas = await loadHtml2Canvas();
        const canvas = await html2canvas(sheet, { scale: 2, useCORS: true });
        lastDesignImageUrl = canvas.toDataURL('image/png');
        const a = document.createElement('a');
        a.href = lastDesignImageUrl;
        a.download = 'recreated-menu.png';
        a.click();
        showNotification('Menu design image downloaded.', 'success');
    } catch (e) {
        showNotification('Could not generate the image in this browser. Please try again.', 'error');
    }
}

function startMenuPdfAnalysis(file) {
    if (menuPdfAnalysisInProgress || !file) return;
    if (file.size > 10 * 1024 * 1024) {
        $('#aiMenuUploadStatus').html('<span style="color:#dc2626;">That file is too large — please use a file under 10 MB.</span>');
        return;
    }

    menuPdfAnalysisInProgress = true;
    setMenuAuditState('progress');
    auditProgress = startAiProgress($('#menuAuditProgress'), [
        'Reading your menu pages...',
        'Checking layout, placement and prices...',
        'Scoring the 14 design principles...',
        'Preparing your annotated suggestion images...'
    ]);

    const fail = function (msg) {
        if (auditProgress) { auditProgress.stop(); auditProgress = null; }
        menuPdfAnalysisInProgress = false;
        setMenuAuditState('upload');
        $('#aiMenuUploadStatus').html(`<span style="color:#dc2626;">${escapeHtml(msg)}</span>`);
    };

    const reader = new FileReader();
    reader.onload = function () {
        const base64 = String(reader.result || '').split(',')[1] || '';
        ajaxRequest({
            url: '/api/MenuEngineeringInsightsApi/AnalyzeMenuPdf',
            method: 'POST',
            data: JSON.stringify({ fileName: file.name, mediaType: file.type, base64Data: base64 }),
            showMessage: false,
            onSuccess: async function (response) {
                if (response && response.status === 200 && response.data) {
                    currentMenuPdfAudit = response.data;
                    await buildSuggestionImages(file, response.data);
                    if (auditProgress) { auditProgress.done(); auditProgress = null; }
                    menuPdfAnalysisInProgress = false;
                    renderMenuPdfAuditSection();
                    currentDesignMenu = response.data.extractedMenu || null;
                    lastDesignImageUrl = null;
                    renderMenuDesignSection();
                    setMenuAuditState('report');
                    $('.btnDownloadMenuAuditPdf').prop('disabled', false).css('opacity', '');
                    showNotification('Menu design audit generated successfully.', 'success');
                } else {
                    fail(response?.responseMsg || 'Menu analysis failed.');
                }
            },
            onError: function (xhr) {
                fail(xhr.responseJSON?.responseMsg || 'Unable to analyze the menu file. Please try again.');
            }
        });
    };
    reader.onerror = function () { fail('Could not read that file. Please try again.'); };
    reader.readAsDataURL(file);
}

let pdfJsLoaderPromise = null;
function loadPdfJs() {
    if (window.pdfjsLib) return Promise.resolve(window.pdfjsLib);
    if (pdfJsLoaderPromise) return pdfJsLoaderPromise;
    pdfJsLoaderPromise = new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js';
        s.onload = function () {
            window.pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
            resolve(window.pdfjsLib);
        };
        s.onerror = reject;
        document.head.appendChild(s);
    });
    return pdfJsLoaderPromise;
}

// Plans which highlights actually get drawn on a page: page-sized boxes are
// dropped (menu-wide advice lives in the text), heavily overlapping boxes are
// merged into one box that carries all their numbers, smallest first.
function planAnnotations(highlights) {
    const items = (highlights || [])
        .map(h => ({ ...h, area: (h.width * h.height) / 10000 }))
        .filter(h => h.width >= 3 && h.height >= 2 && h.area <= 0.60)
        .sort((a, b) => a.area - b.area);

    const accepted = [];
    items.forEach(h => {
        let host = null;
        for (const a of accepted) {
            const ox = Math.max(0, Math.min(h.x + h.width, a.x + a.width) - Math.max(h.x, a.x));
            const oy = Math.max(0, Math.min(h.y + h.height, a.y + a.height) - Math.max(h.y, a.y));
            const overlap = (ox * oy) / Math.min(h.width * h.height, a.width * a.height);
            if (overlap > 0.45) { host = a; break; }
        }
        if (host) host.ns.push(h.n);
        else if (accepted.length < 8) accepted.push({ x: h.x, y: h.y, width: h.width, height: h.height, ns: [h.n] });
    });
    return accepted;
}

function roundedRectPath(ctx, x, y, w, h, r) {
    const rr = Math.min(r, w / 2, h / 2);
    ctx.beginPath();
    ctx.moveTo(x + rr, y);
    ctx.lineTo(x + w - rr, y);
    ctx.arcTo(x + w, y, x + w, y + rr, rr);
    ctx.lineTo(x + w, y + h - rr);
    ctx.arcTo(x + w, y + h, x + w - rr, y + h, rr);
    ctx.lineTo(x + rr, y + h);
    ctx.arcTo(x, y + h, x, y + h - rr, rr);
    ctx.lineTo(x, y + rr);
    ctx.arcTo(x, y, x + rr, y, rr);
    ctx.closePath();
}

// Draws the planned boxes: soft fill, one clean outline, and a single pill
// badge on the top-left corner carrying the recommendation number(s).
function drawMenuAnnotations(ctx, planned, W, H) {
    planned.forEach(a => {
        const x = (a.x / 100) * W, y = (a.y / 100) * H;
        const w = (a.width / 100) * W, h = (a.height / 100) * H;

        roundedRectPath(ctx, x, y, w, h, Math.max(8, W / 90));
        ctx.fillStyle = 'rgba(240, 141, 17, 0.10)';
        ctx.fill();
        ctx.strokeStyle = '#f08d11';
        ctx.lineWidth = Math.max(2, W / 450);
        ctx.stroke();

        const label = a.ns.join(' · ');
        const fontPx = Math.max(13, W / 52);
        ctx.font = `bold ${fontPx}px Arial`;
        const pillW = ctx.measureText(label).width + fontPx * 1.3;
        const pillH = fontPx * 1.7;
        const px = Math.min(Math.max(x - pillW / 2, 2), W - pillW - 2);
        const py = Math.min(Math.max(y - pillH / 2, 2), H - pillH - 2);

        roundedRectPath(ctx, px, py, pillW, pillH, pillH / 2);
        ctx.fillStyle = '#f08d11';
        ctx.fill();
        ctx.lineWidth = 3;
        ctx.strokeStyle = '#ffffff';
        ctx.stroke();
        ctx.fillStyle = '#ffffff';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(label, px + pillW / 2, py + pillH / 2 + 1);
    });
}

// Adds a clean legend strip under the page so a downloaded image explains
// itself: marker number -> principle name, for this page's highlights only.
function finalizeAnnotatedCanvas(pageCanvas, highlights, audit, pageNo) {
    const planned = planAnnotations(highlights);
    drawMenuAnnotations(pageCanvas.getContext('2d'), planned, pageCanvas.width, pageCanvas.height);

    const entries = [];
    (audit.recommendations || []).forEach((rec, idx) => {
        const hl = rec.highlight;
        if (hl && (hl.page || 1) === pageNo && hl.width > 0 && hl.height > 0) {
            entries.push(`${idx + 1} — ${rec.principle || 'Suggestion'}`);
        }
    });
    if (entries.length === 0) return pageCanvas.toDataURL('image/png');

    const W = pageCanvas.width;
    const fontPx = Math.max(14, W / 46);
    const lineH = fontPx * 1.55;
    const pad = fontPx * 1.1;
    const meas = document.createElement('canvas').getContext('2d');
    meas.font = `${fontPx}px Arial`;
    const lines = [];
    let line = '';
    entries.forEach(e => {
        const trial = line ? line + '      ' + e : e;
        if (meas.measureText(trial).width > W - pad * 2 && line) { lines.push(line); line = e; }
        else line = trial;
    });
    if (line) lines.push(line);

    const footerH = pad * 1.6 + lineH * (lines.length + 1);
    const out = document.createElement('canvas');
    out.width = W;
    out.height = pageCanvas.height + footerH;
    const ctx = out.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, out.width, out.height);
    ctx.drawImage(pageCanvas, 0, 0);
    ctx.fillStyle = '#f08d11';
    ctx.fillRect(0, pageCanvas.height, W, 3);

    ctx.textAlign = 'left';
    ctx.textBaseline = 'alphabetic';
    ctx.fillStyle = '#6b7280';
    ctx.font = `bold ${Math.round(fontPx * 0.78)}px Arial`;
    ctx.fillText('SUGGESTIONS ON THIS PAGE', pad, pageCanvas.height + pad * 1.15);
    ctx.fillStyle = '#111827';
    ctx.font = `${fontPx}px Arial`;
    lines.forEach((l, i) => {
        ctx.fillText(l, pad, pageCanvas.height + pad * 1.15 + lineH * (i + 1));
    });

    return out.toDataURL('image/png');
}

// Builds the annotated "suggestion images" from the uploaded file itself:
// PDF pages are rendered in the browser, photos are drawn onto a canvas,
// then the AI's highlight areas are marked on top.
async function buildSuggestionImages(file, audit) {
    currentSuggestionImages = [];
    const byPage = {};
    (audit.recommendations || []).forEach((rec, idx) => {
        const hl = rec.highlight;
        if (hl && hl.width > 0 && hl.height > 0) {
            const pg = hl.page || 1;
            (byPage[pg] = byPage[pg] || []).push({ ...hl, n: idx + 1 });
        }
    });
    const pages = Object.keys(byPage).map(Number).sort((a, b) => a - b).slice(0, 4);
    if (pages.length === 0) return;

    try {
        if (file.type === 'application/pdf') {
            const pdfjsLib = await loadPdfJs();
            const buf = await file.arrayBuffer();
            const pdf = await pdfjsLib.getDocument({ data: buf }).promise;
            for (const pg of pages) {
                if (pg < 1 || pg > pdf.numPages) continue;
                const page = await pdf.getPage(pg);
                const baseViewport = page.getViewport({ scale: 1 });
                const scale = Math.min(2, 1100 / baseViewport.width);
                const viewport = page.getViewport({ scale });
                const canvas = document.createElement('canvas');
                canvas.width = viewport.width;
                canvas.height = viewport.height;
                await page.render({ canvasContext: canvas.getContext('2d'), viewport }).promise;
                currentSuggestionImages.push({ page: pg, dataUrl: finalizeAnnotatedCanvas(canvas, byPage[pg], audit, pg) });
            }
        } else if (file.type.indexOf('image/') === 0) {
            const url = URL.createObjectURL(file);
            try {
                const img = await new Promise((resolve, reject) => {
                    const i = new Image();
                    i.onload = () => resolve(i);
                    i.onerror = reject;
                    i.src = url;
                });
                const natW = img.naturalWidth || img.width;
                const natH = img.naturalHeight || img.height;
                const scale = Math.min(1, 1300 / natW);
                const canvas = document.createElement('canvas');
                canvas.width = Math.round(natW * scale);
                canvas.height = Math.round(natH * scale);
                canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
                currentSuggestionImages.push({ page: 1, dataUrl: finalizeAnnotatedCanvas(canvas, byPage[1] || [], audit, 1) });
            } finally {
                URL.revokeObjectURL(url);
            }
        }
    } catch (e) {
        // Suggestion images are a bonus — the text audit still renders.
        currentSuggestionImages = [];
    }
}

function renderMenuPdfAuditSection() {
    const audit = currentMenuPdfAudit;
    if (!audit) return;
    $('#aiMenuPdfAuditSection').remove();

    const score = audit.designScore || 0;
    const scoreColor = score >= 75 ? '#059669' : (score >= 50 ? '#d97706' : '#dc2626');
    const recs = audit.recommendations || [];

    const cards = recs.map((rec, idx) => `
        <div class="timeline-item">
            <div class="timeline-header">
                <span class="badge-pill" style="background: #f08d11; color: #fff;">${idx + 1}</span>
                ${escapeHtml(rec.itemName || rec.category || 'Whole Menu')}
                <span class="badge-pill success" style="margin-left: 8px;"><i class="fas fa-brain"></i> ${escapeHtml(rec.principle || 'Menu Psychology')}</span>
                ${rec.highlight ? `<span class="badge-pill" style="background: #eff6ff; color: #2563eb; margin-left: 6px;">Page ${rec.highlight.page || 1}</span>` : ''}
            </div>
            <div class="timeline-desc">${escapeHtml(rec.recommendation || '')}</div>
            <div class="timeline-desc" style="color:#6b7280;">${escapeHtml(rec.reasoning || '')}</div>
            ${rec.expectedImpact ? `<div class="timeline-impact"><i class="fas fa-arrow-trend-up"></i> Expected Impact: <strong>${escapeHtml(rec.expectedImpact)}</strong></div>` : ''}
        </div>`).join('');

    const strengths = (audit.strengths || []).map(s => `<li style="margin-bottom: 3px;"><i class="fas fa-check-circle" style="color: #059669;"></i> ${escapeHtml(s)}</li>`).join('');
    const issues = (audit.criticalIssues || []).map(s => `<li style="margin-bottom: 3px;"><i class="fas fa-exclamation-circle" style="color: #dc2626;"></i> ${escapeHtml(s)}</li>`).join('');

    const images = currentSuggestionImages.map(img => `
        <div style="margin-bottom: 14px;">
            <img src="${img.dataUrl}" alt="Annotated menu page ${img.page}" style="width: 100%; border: 1px solid #e5e7eb; border-radius: 8px;">
            <div style="margin-top: 6px;">
                <a class="btn btn-outline btn-sm" href="${img.dataUrl}" download="menu-suggestions-page-${img.page}.png"><i class="fas fa-download"></i> Download annotated page ${img.page}</a>
            </div>
        </div>`).join('');

    const html = `
        <div class="ai-section-box" id="aiMenuPdfAuditSection" style="border-left: 4px solid #f08d11;">
            <div class="ai-section-title"><i class="fas fa-file-magnifying-glass" style="color: #f08d11;"></i> Menu Design Audit — From Your Menu File
                <span class="badge-pill" style="background: ${scoreColor}; color: #fff; margin-left: 8px;">Design Score: ${score}/100</span>
            </div>
            <p style="font-size: 13px; color: #374151; margin: 0 0 10px;">${escapeHtml(audit.summary || '')}</p>
            <div style="display: flex; gap: 20px; flex-wrap: wrap; margin-bottom: 6px;">
                ${strengths ? `<div style="flex: 1; min-width: 220px;"><strong style="font-size: 12.5px; color: #065f46;">What is working</strong><ul style="list-style: none; padding: 0; margin: 6px 0 0; font-size: 12.5px; color: #374151;">${strengths}</ul></div>` : ''}
                ${issues ? `<div style="flex: 1; min-width: 220px;"><strong style="font-size: 12.5px; color: #991b1b;">Needs attention first</strong><ul style="list-style: none; padding: 0; margin: 6px 0 0; font-size: 12.5px; color: #374151;">${issues}</ul></div>` : ''}
            </div>
            ${cards}
            ${images ? `<div style="margin-top: 12px;"><strong style="font-size: 13px; color: #111827;">Your menu, annotated</strong><p style="font-size: 12px; color: #6b7280; margin: 2px 0 10px;">Numbered markers match the recommendations above — each box shows where that suggestion applies.</p>${images}</div>` : ''}
        </div>
    `;

    $('#menuAuditReportContainer').html(html);
}

// Standalone PDF for the menu design audit (separate from the AI analysis report)
async function exportMenuAuditPdf() {
    const audit = currentMenuPdfAudit;
    if (!audit) {
        showNotification('Please run the menu design audit first.', 'warning');
        return;
    }

    let logoBase64 = null;
    try {
        logoBase64 = await getImageAsPngBase64('/images/menu optics_logo.png');
    } catch (e) {
        console.warn('Menu Optics logo load failed for PDF', e);
    }

    const score = audit.designScore || 0;
    const scoreColor = score >= 75 ? '#059669' : (score >= 50 ? '#d97706' : '#dc2626');

    const docDefinition = {
        pageSize: 'A4',
        pageOrientation: 'portrait',
        pageMargins: [36, 36, 36, 36],
        styles: {
            reportTitle: { fontSize: 16, bold: true, color: '#f08d11', margin: [0, 0, 0, 3] },
            reportSubtitle: { fontSize: 9.5, color: '#4b5563', margin: [0, 0, 0, 4] },
            sectionHeader: { fontSize: 11.5, bold: true, color: '#111827', margin: [0, 10, 0, 5] },
            tableHeader: { fontSize: 8.5, bold: true, color: '#ffffff', fillColor: '#374151', alignment: 'left' },
            tableCell: { fontSize: 8, color: '#374151', margin: [0, 2, 0, 2] }
        },
        defaultStyle: { fontSize: 8.5, color: '#374151' },
        content: [
            {
                columns: [
                    {
                        stack: [
                            { text: 'MENU OPTICS — MENU DESIGN AUDIT', style: 'reportTitle' },
                            { text: `File: ${audit.fileName || 'Menu file'} | Analyzed: ${audit.analyzedDate || ''} | Model: ${audit.sourceModel || 'MenuOptics AI'}`, style: 'reportSubtitle' },
                            { text: 'An audit of the actual menu guests see — layout, placement, prices and descriptions — against 14 research-backed menu design principles.', fontSize: 8.5, color: '#6b7280' }
                        ]
                    },
                    logoBase64 ? { image: logoBase64, width: 110, alignment: 'right' } : {}
                ]
            },
            { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 523, y2: 0, lineWidth: 1.5, lineColor: '#f08d11' }], margin: [0, 6, 0, 4] },

            { text: 'DESIGN SCORE & SUMMARY', style: 'sectionHeader' },
            {
                text: [
                    { text: 'Design Score: ', bold: true, color: '#111827' },
                    { text: `${score}/100`, bold: true, color: scoreColor, fontSize: 12 }
                ],
                margin: [0, 0, 0, 4]
            },
            { text: audit.summary || '', margin: [0, 0, 0, 4] },
            ...((audit.strengths || []).length > 0 ? [
                { text: [{ text: 'What is working:  ', bold: true, color: '#065f46' }, (audit.strengths || []).join('  •  ')], margin: [0, 0, 0, 3] }
            ] : []),
            ...((audit.criticalIssues || []).length > 0 ? [
                { text: [{ text: 'Needs attention first:  ', bold: true, color: '#991b1b' }, (audit.criticalIssues || []).join('  •  ')], margin: [0, 0, 0, 3] }
            ] : []),

            { text: 'RECOMMENDATIONS — 14 DESIGN PRINCIPLES', style: 'sectionHeader' },
            {
                table: {
                    headerRows: 1,
                    widths: ['18%', '16%', '33%', '33%'],
                    body: [
                        [
                            { text: 'Principle', style: 'tableHeader' },
                            { text: 'Applies To', style: 'tableHeader' },
                            { text: 'Recommendation', style: 'tableHeader' },
                            { text: 'Reasoning & Impact', style: 'tableHeader' }
                        ],
                        ...(audit.recommendations || []).map((r, idx) => [
                            { text: `${idx + 1}. ${r.principle || ''}`, style: 'tableCell', bold: true },
                            { text: r.itemName || r.category || 'Whole Menu', style: 'tableCell' },
                            { text: r.recommendation || '', style: 'tableCell' },
                            { text: `${r.reasoning || ''}${r.expectedImpact ? ` Impact: ${r.expectedImpact}` : ''}`, style: 'tableCell', fontSize: 7.5 }
                        ])
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            ...currentSuggestionImages.slice(0, 4).flatMap(img => [
                { text: `Annotated menu — page ${img.page} (numbered markers match the recommendations above)`, style: 'sectionHeader' },
                { image: img.dataUrl, width: 500, margin: [0, 0, 0, 6] }
            ]),

            ...(lastDesignImageUrl ? [
                { text: 'RECREATED MENU DESIGN', style: 'sectionHeader' },
                { image: lastDesignImageUrl, width: 500, margin: [0, 0, 0, 6] }
            ] : []),

            { text: '', margin: [0, 6, 0, 0] },
            {
                table: {
                    widths: ['100%'],
                    body: [
                        [
                            {
                                stack: [
                                    { text: 'Disclaimer: Informational Purposes & Business Discretion', bold: true, fontSize: 7.5, color: '#374151', margin: [0, 0, 0, 2] },
                                    { text: 'This audit was generated using artificial intelligence from the menu file you provided. All observations and suggestions are informational guidance based on published menu-design research; final menu design and pricing decisions rest entirely with the business owner and management.', fontSize: 6.8, color: '#6b7280' }
                                ],
                                fillColor: '#f9fafb',
                                margin: [6, 5, 6, 5]
                            }
                        ]
                    ]
                },
                layout: {
                    hLineWidth: function () { return 0.5; },
                    vLineWidth: function () { return 0.5; },
                    hLineColor: function () { return '#e5e7eb'; },
                    vLineColor: function () { return '#e5e7eb'; }
                }
            }
        ]
    };

    const fileName = `Menu_Design_Audit_${new Date().toISOString().slice(0, 10)}.pdf`;
    pdfMake.createPdf(docDefinition).download(fileName);
    showNotification('Menu Design Audit PDF downloaded successfully!', 'success');
}

$(document).on('click', '.btnDownloadMenuAuditPdf', function () {
    exportMenuAuditPdf();
});

async function exportAiInsightsPdf(reportData) {
    if (!reportData) {
        showNotification('Report data unavailable for PDF export.', 'error');
        return;
    }

    const summary = reportData.executiveSummary || {};
    const quadrants = reportData.quadrantBreakdown || {};
    const removals = reportData.removalRecommendations || [];
    const titles = reportData.titleImprovements || [];
    const pricing = reportData.pricingOptimizations || [];
    const checklist = reportData.menuPlanningChecklist || [];
    const design = reportData.menuDesignRecommendations || [];

    // TEMPORARY (client request, 2026-11-04): the PDF section 'MENU DESIGN &
    // PSYCHOLOGY RECOMMENDATIONS' is hidden. Set this to true to restore it.
    const SHOW_DESIGN_SECTION_PDF = false;
    const checklistSectionNo = SHOW_DESIGN_SECTION_PDF ? 7 : 6;

    let logoBase64 = null;
    try {
        logoBase64 = await getImageAsPngBase64('/images/menu optics_logo.png');
    } catch (e) {
        console.warn('Menu Optics logo load failed for PDF', e);
    }

    const docDefinition = {
        pageSize: 'A4',
        pageOrientation: 'portrait',
        pageMargins: [36, 36, 36, 36],
        styles: {
            reportTitle: { fontSize: 16, bold: true, color: '#f08d11', margin: [0, 0, 0, 3] },
            reportSubtitle: { fontSize: 9.5, color: '#4b5563', margin: [0, 0, 0, 4] },
            sectionHeader: { fontSize: 11.5, bold: true, color: '#111827', margin: [0, 10, 0, 5] },
            tableHeader: { fontSize: 8.5, bold: true, color: '#ffffff', fillColor: '#374151', alignment: 'left' },
            tableCell: { fontSize: 8, color: '#374151', margin: [0, 2, 0, 2] },
            calloutBox: { fontSize: 8.5, color: '#1f2937', lineHeight: 1.3 }
        },
        defaultStyle: { fontSize: 8.5, color: '#374151' },
        content: [
            {
                columns: [
                    {
                        stack: [
                            { text: 'MENU OPTICS — AI STRATEGIC REPORT', style: 'reportTitle' },
                            {
                                text: `Restaurant: ${reportData.restaurantName || 'Menu Optics'} | Location: ${reportData.location || 'Local Trade Area'} | Range: ${reportData.dateRange || 'Last 30 Days'}`,
                                style: 'reportSubtitle'
                            },
                            {
                                text: `Generated: ${reportData.generatedDate || new Date().toLocaleDateString()} | Engine: ${reportData.sourceModel || 'MenuOptics AI'}`,
                                fontSize: 8,
                                color: '#6b7280'
                            }
                        ]
                    },
                    logoBase64 ? { image: logoBase64, width: 85, alignment: 'right', margin: [0, 0, 0, 5] } : { text: '' }
                ],
                margin: [0, 0, 0, 8]
            },
            { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 523, y2: 0, lineWidth: 1.5, lineColor: '#f08d11' }] },
            { text: '', margin: [0, 4, 0, 0] },

            {
                table: {
                    widths: ['100%'],
                    body: [
                        [
                            {
                                stack: [
                                    { text: `LOCAL COMPETITOR & MARKET INTELLIGENCE (${reportData.location || 'Trade Area'}):`, bold: true, fontSize: 9, color: '#6d28d9' },
                                    { text: reportData.competitorMarketOverview || '', margin: [0, 2, 0, 0], color: '#374151', fontSize: 8.5 }
                                ],
                                fillColor: '#f5f3ff',
                                margin: [6, 6, 6, 6]
                            }
                        ]
                    ]
                },
                layout: 'noBorders'
            },
            { text: '', margin: [0, 4, 0, 0] },

            { text: '1. EXECUTIVE SUMMARY & KEY METRICS', style: 'sectionHeader' },
            {
                table: {
                    widths: ['25%', '25%', '25%', '25%'],
                    body: [
                        [
                            { text: 'Health Score', bold: true, fillColor: '#f9fafb' },
                            { text: 'Potential Monthly Increase', bold: true, fillColor: '#f9fafb' },
                            { text: 'Average Food Cost %', bold: true, fillColor: '#f9fafb' },
                            { text: 'Analyzed Items', bold: true, fillColor: '#f9fafb' }
                        ],
                        [
                            { text: `${summary.healthScore || 0}/100`, fontSize: 12, bold: true, color: '#111827' },
                            { text: `+$${(summary.estimatedMonthlyProfitUplift || 0).toFixed(2)}/mo`, fontSize: 12, bold: true, color: '#059669' },
                            { text: `${(summary.averageFoodCostPct || 0).toFixed(1)}%`, fontSize: 12, bold: true, color: '#111827' },
                            { text: `${reportData.totalMenuItemsAnalyzed || 0}`, fontSize: 12, bold: true, color: '#111827' }
                        ]
                    ]
                },
                layout: 'lightHorizontalLines'
            },
            { text: '', margin: [0, 3, 0, 0] },
            {
                table: {
                    widths: ['100%'],
                    body: [
                        [
                            {
                                stack: [
                                    { text: `Strategic Takeaway: ${summary.keyTakeaway || ''}`, bold: true, style: 'calloutBox' },
                                    { text: `Top Strength: ${summary.topStrength || ''}`, margin: [0, 2, 0, 0], color: '#166534' },
                                    { text: `Key Risk: ${summary.criticalRisk || ''}`, margin: [0, 1, 0, 0], color: '#991b1b' }
                                ],
                                fillColor: '#faf5ff',
                                margin: [6, 6, 6, 6]
                            }
                        ]
                    ]
                },
                layout: 'noBorders'
            },

            { text: '2. MENU MATRIX DISTRIBUTION', style: 'sectionHeader' },
            {
                table: {
                    widths: ['25%', '25%', '25%', '25%'],
                    body: [
                        [
                            { text: 'Stars ⭐ (High Profit, High Pop)', style: 'tableHeader' },
                            { text: 'Plowhorses 🐴 (Low Profit, High Pop)', style: 'tableHeader' },
                            { text: 'Puzzles 🧩 (High Profit, Low Pop)', style: 'tableHeader' },
                            { text: 'Dogs 🐶 (Low Profit, Low Pop)', style: 'tableHeader' }
                        ],
                        [
                            { text: `${quadrants.starsCount || 0} items (${(quadrants.starsRevenueShare || 0).toFixed(1)}% rev)` },
                            { text: `${quadrants.plowhorsesCount || 0} items (${(quadrants.plowhorsesRevenueShare || 0).toFixed(1)}% rev)` },
                            { text: `${quadrants.puzzlesCount || 0} items (${(quadrants.puzzlesRevenueShare || 0).toFixed(1)}% rev)` },
                            { text: `${quadrants.dogsCount || 0} items (${(quadrants.dogsRevenueShare || 0).toFixed(1)}% rev)` }
                        ]
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            { text: '3. RECOMMENDED MENU RETIREMENTS (MAX 10% CONSTRAINT)', style: 'sectionHeader' },
            {
                table: {
                    widths: ['24%', '11%', '14%', '10%', '29%', '12%'],
                    body: [
                        [
                            { text: 'Menu Item', style: 'tableHeader' },
                            { text: 'Price', style: 'tableHeader' },
                            { text: 'Cost (FC %)', style: 'tableHeader' },
                            { text: 'Sold', style: 'tableHeader' },
                            { text: 'Deprecation Rationale', style: 'tableHeader' },
                            { text: 'Est. Savings', style: 'tableHeader' }
                        ],
                        ...(removals.length > 0 ? removals.map(r => [
                            { text: r.name || '', bold: true, style: 'tableCell' },
                            { text: `$${(r.sellingPrice || 0).toFixed(2)}`, style: 'tableCell' },
                            { text: `$${(r.cost || 0).toFixed(2)} (${(r.foodCostPct || 0).toFixed(1)}%)`, style: 'tableCell' },
                            { text: `${r.unitsSold || 0}`, style: 'tableCell' },
                            { text: `${r.primaryReason || ''} ${r.actionRecommendation || ''}`, style: 'tableCell', fontSize: 7.5 },
                            { text: `+$${(r.estimatedMonthlyLaborOrWasteSavings || 0).toFixed(2)}/mo`, bold: true, color: '#059669', style: 'tableCell' }
                        ]) : [[{ text: 'No menu removals recommended.', colSpan: 6, alignment: 'center', style: 'tableCell' }, {}, {}, {}, {}, {}]])
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            { text: '4. TREND CULINARY VERBIAGE & TITLE ENHANCEMENTS', style: 'sectionHeader' },
            {
                table: {
                    widths: ['22%', '28%', '22%', '28%'],
                    body: [
                        [
                            { text: 'Current Title', style: 'tableHeader' },
                            { text: 'Suggested Trend Title', style: 'tableHeader' },
                            { text: 'Trend Verbiage', style: 'tableHeader' },
                            { text: 'Competitor & Appetite Rationale', style: 'tableHeader' }
                        ],
                        ...(titles.length > 0 ? titles.map(t => [
                            { text: t.currentTitle || '', style: 'tableCell', color: '#6b7280' },
                            { text: t.suggestedTitle || '', bold: true, color: '#6d28d9', style: 'tableCell' },
                            { text: t.trendVerbiageUsed || 'Modern Culinary', style: 'tableCell', color: '#6b21a8' },
                            { text: t.reasoning || '', style: 'tableCell', fontSize: 7.5 }
                        ]) : [[{ text: 'All current titles already use strong culinary verbiage.', colSpan: 4, alignment: 'center', style: 'tableCell' }, {}, {}, {}]])
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            { text: '5. RESEARCHED MENU ENGINEERING PRICING & MARGIN OPTIMIZATION', style: 'sectionHeader' },
            {
                table: {
                    widths: ['20%', '11%', '13%', '12%', '14%', '30%'],
                    body: [
                        [
                            { text: 'Menu Item', style: 'tableHeader' },
                            { text: 'Current', style: 'tableHeader' },
                            { text: 'Recommended', style: 'tableHeader' },
                            { text: 'Proj. Margin', style: 'tableHeader' },
                            { text: 'Est. Gain', style: 'tableHeader' },
                            { text: 'Pricing Principle & Rationale', style: 'tableHeader' }
                        ],
                        ...(pricing.length > 0 ? pricing.map(p => [
                            { text: p.name || '', bold: true, style: 'tableCell' },
                            { text: `$${(p.currentPrice || 0).toFixed(2)}`, style: 'tableCell' },
                            { text: `$${(p.recommendedPrice || 0).toFixed(2)}`, bold: true, color: '#059669', style: 'tableCell' },
                            { text: `$${(p.projectedMargin || 0).toFixed(2)}`, style: 'tableCell' },
                            { text: `+$${(p.estimatedMonthlyProfitIncrease || 0).toFixed(2)}/mo`, bold: true, color: '#059669', style: 'tableCell' },
                            { text: `[${p.pricingPrinciple || 'Charm Pricing'}]: ${p.rationale || ''}`, style: 'tableCell', fontSize: 7.5 }
                        ]) : [[{ text: 'All prices currently aligned with margin targets.', colSpan: 6, alignment: 'center', style: 'tableCell' }, {}, {}, {}, {}, {}]])
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            ...(SHOW_DESIGN_SECTION_PDF && design.length > 0 ? [
                { text: '6. MENU DESIGN & PSYCHOLOGY RECOMMENDATIONS', style: 'sectionHeader' },
                {
                    table: {
                        widths: ['18%', '20%', '32%', '30%'],
                        body: [
                            [
                                { text: 'Principle', style: 'tableHeader' },
                                { text: 'Applies To', style: 'tableHeader' },
                                { text: 'Recommendation', style: 'tableHeader' },
                                { text: 'Research Reasoning & Impact', style: 'tableHeader' }
                            ],
                            ...design.map(d => [
                                { text: d.principle || 'Menu Psychology', bold: true, style: 'tableCell', color: '#6d28d9' },
                                { text: d.itemName || d.category || 'Whole Menu', style: 'tableCell' },
                                { text: d.recommendation || '', style: 'tableCell' },
                                { text: `${d.reasoning || ''}${d.expectedImpact ? ` Impact: ${d.expectedImpact}` : ''}`, style: 'tableCell', fontSize: 7.5 }
                            ])
                        ]
                    },
                    layout: 'lightHorizontalLines'
                }
            ] : []),

            { text: checklistSectionNo + '. MENU PLANNING CHECKLIST', style: 'sectionHeader' },
            ...(checklist.length > 0 ? [{
                table: {
                    widths: [14, '*'],
                    body: checklist.map(item => [
                        {
                            canvas: [{ type: 'rect', x: 0, y: 2.5, w: 9, h: 9, lineWidth: 0.9, lineColor: '#374151' }],
                            border: [false, false, false, false]
                        },
                        {
                            border: [false, false, false, false],
                            margin: [0, 1, 0, 3],
                            text: [
                                { text: `${item.task || ''}`, bold: true, color: '#111827' },
                                ...(item.area ? [{ text: `  [${item.area}]`, fontSize: 7.5, color: '#4338ca', bold: true }] : []),
                                ...(item.details ? [{ text: ` \u2014 ${item.details}`, color: '#4b5563' }] : [])
                            ]
                        }
                    ])
                },
                layout: 'noBorders'
            }] : [{ text: 'No checklist items \u2014 the menu is already well planned.', color: '#6b7280' }]),

            { text: '', margin: [0, 6, 0, 0] },
            {
                table: {
                    widths: ['100%'],
                    body: [
                        [
                            {
                                stack: [
                                    { text: 'Disclaimer: Informational Purposes & Business Discretion', bold: true, fontSize: 7.5, color: '#374151', margin: [0, 0, 0, 2] },
                                    { text: 'This report was generated using artificial intelligence and data-driven analysis to provide high-level insights into local market trends, competitive benchmarks, and menu pricing suggestions.', fontSize: 6.8, color: '#6b7280', margin: [0, 0, 0, 2] },
                                    {
                                        ul: [
                                            { text: [{ text: 'Guidance, Not Mandate: ', bold: true }, 'All pricing models, financial estimates, and market observations contained in this report are informational suggestions intended solely as strategic guidance. They do not constitute formal financial, operational, or legal advice.'], fontSize: 6.5, color: '#6b7280', margin: [0, 0, 0, 1.5] },
                                            { text: [{ text: 'Operator Responsibility: ', bold: true }, 'Every hospitality operation carries unique overhead structures, labor costs, supplier agreements, and margin requirements. Final menu pricing, positioning, and implementation decisions rest entirely with the business owner and management.'], fontSize: 6.5, color: '#6b7280', margin: [0, 0, 0, 1.5] },
                                            { text: [{ text: 'Market Dynamics: ', bold: true }, 'Competitive data and market benchmarks reflect conditions at the time of analysis and are subject to rapid change. We recommend validating these findings against your internal recipe costing, vendor contracts, and target contribution margins before updating your menus.'], fontSize: 6.5, color: '#6b7280' }
                                        ]
                                    }
                                ],
                                fillColor: '#f9fafb',
                                margin: [6, 5, 6, 5]
                            }
                        ]
                    ]
                },
                layout: {
                    hLineWidth: function () { return 0.5; },
                    vLineWidth: function () { return 0.5; },
                    hLineColor: function () { return '#e5e7eb'; },
                    vLineColor: function () { return '#e5e7eb'; }
                }
            }
        ]
    };

    const fileName = `Menu_Engineering_AI_Report_${new Date().toISOString().slice(0, 10)}.pdf`;
    pdfMake.createPdf(docDefinition).download(fileName);
    showNotification('AI Menu Engineering Report PDF downloaded successfully!', 'success');
}

function openCenteredModal(modalId) {
    const $m = $('#' + modalId);
    $m.addClass('show').css({
        'display': 'flex',
        'align-items': 'center',
        'justify-content': 'center'
    }).hide().fadeIn(200);
}

function closeCenteredModal(modalId) {
    // While an analysis is running, warn before closing its modal
    if (modalId === 'aiInsightsModal' && aiAnalysisInProgress) { showAiCloseConfirm(modalId); return; }
    if (modalId === 'menuAuditModal' && menuPdfAnalysisInProgress) { showAiCloseConfirm(modalId); return; }
    $('#' + modalId).fadeOut(200, function () {
        $(this).removeClass('show').css('display', 'none');
    });
}

function showAiCloseConfirm(modalId) {
    const $modal = $('#' + modalId);
    if ($modal.find('.ai-close-confirm-overlay').length === 0) {
        $modal.append(`
            <div class="ai-close-confirm-overlay" style="position: absolute; inset: 0; background: rgba(0,0,0,0.45); display: none; align-items: center; justify-content: center; z-index: 60;">
                <div style="background: #fff; border-radius: 12px; padding: 24px; max-width: 400px; width: calc(100% - 48px); text-align: center; box-shadow: 0 20px 50px rgba(0,0,0,0.3);">
                    <div style="font-size: 30px; color: #f59e0b; margin-bottom: 8px;"><i class="fas fa-triangle-exclamation"></i></div>
                    <div style="font-weight: 700; font-size: 16px; color: #111827; margin-bottom: 6px;">Analysis in Progress</div>
                    <p style="font-size: 13px; color: #4b5563; margin: 0 0 18px;">Your AI analysis is still being generated. If you close this window now, the analysis will be interrupted and the report will not be completed.</p>
                    <div style="display: flex; gap: 10px; justify-content: center;">
                        <button type="button" class="btn btn-primary btnKeepWaitingAi">Keep Waiting</button>
                        <button type="button" class="btn btn-outline btnCloseAnywayAi" style="color: #dc2626; border-color: #dc2626;">Close Anyway</button>
                    </div>
                </div>
            </div>
        `);
    }
    $modal.find('.ai-close-confirm-overlay').css('display', 'flex');
}

$(document).on('click', '.btnKeepWaitingAi', function () {
    $(this).closest('.ai-close-confirm-overlay').css('display', 'none');
});

$(document).on('click', '.btnCloseAnywayAi', function () {
    const $overlay = $(this).closest('.ai-close-confirm-overlay');
    const modalId = $overlay.closest('.modal').attr('id');
    $overlay.css('display', 'none');
    if (modalId === 'menuAuditModal') menuPdfAnalysisInProgress = false;
    else aiAnalysisInProgress = false;
    closeCenteredModal(modalId);
});

async function getImageAsPngBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = 'Anonymous';
        img.onload = function () {
            const canvas = document.createElement('canvas');
            canvas.width = img.naturalWidth || img.width;
            canvas.height = img.naturalHeight || img.height;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);
            resolve(canvas.toDataURL('image/png'));
        };
        img.onerror = function (e) {
            reject(e);
        };
        img.src = url;
    });
}
