/* ============================================================================
 * menuEngineeringAi.js — AI Menu Engineering Intelligence & PDF Generator
 * Handles Full Menu Strategic AI Analysis with Location & Local Area Competitor Market Intelligence.
 * ==========================================================================*/

let currentFullAiReportData = null;

$(document).ready(function () {
    // Open Full Menu AI Modal
    $('#btnOpenAiInsightsModal').on('click', function () {
        openFullAiInsightsModal();
    });

    // Download Full AI PDF Report
    $('#btnDownloadAiPdfModal, #btnDownloadAiPdfFooter').on('click', function () {
        if (currentFullAiReportData) {
            exportAiInsightsPdf(currentFullAiReportData);
        } else {
            showNotification('Report data is still loading. Please wait.', 'warning');
        }
    });
});

/* ── 1. Full Menu AI Strategic Analysis ─────────────────────────────────── */

function openFullAiInsightsModal() {
    openCenteredModal('aiInsightsModal');
    $('#aiLoadingIndicator').show();
    $('#aiReportContainer').hide();

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
                $('#aiLoadingIndicator').hide();
                showNotification(response?.responseMsg || "Failed to generate AI insights.", "error");
            }
        },
        onError: function (xhr) {
            $('#aiLoadingIndicator').hide();
            const msg = xhr.responseJSON?.responseMsg || "Unable to generate AI analysis. Please ensure menu items are added.";
            showNotification(msg, "error");
        }
    });
}

function renderFullAiReport(data) {
    $('#aiModalModelBadge').html(`<i class="fas fa-microchip"></i> ${data.sourceModel || 'MenuOptics AI'}`);
    $('#aiDateRangeBadge').text(data.dateRange || 'Last 30 Days');
    $('#aiLocationBadge').html(`<i class="fas fa-map-marker-alt" style="color:#ef4444;"></i> ${data.location || 'Trade Area'}`);

    const summary = data.executiveSummary || {};
    $('#aiHealthScore').text(`${summary.healthScore || 0}/100`);
    $('#aiProfitUplift').text(`+$${(summary.estimatedMonthlyProfitUplift || 0).toFixed(2)}/mo`);
    $('#aiAvgFoodCost').text(`${(summary.averageFoodCostPct || 0).toFixed(1)}%`);
    $('#aiAnalyzedCount').text(data.totalMenuItemsAnalyzed || 0);

    // Competitor Overview
    $('#aiCompetitorOverview').text(data.competitorMarketOverview || 'Area competitor pricing benchmark indicates healthy margin expansion opportunity.');

    $('#aiExecutiveTakeaway').text(summary.keyTakeaway || 'Execute menu rebalancing, price adjustments, and recipe trimming.');
    $('#aiTopStrength').text(summary.topStrength || '--');
    $('#aiCriticalRisk').text(summary.criticalRisk || '--');

    // Section: Quadrants Breakdown Cards
    const q = data.quadrantBreakdown || {};
    $('#qStarCount').text(`${q.starsCount || 0} items`);
    $('#qStarRev').text(`${(q.starsRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qPlowCount').text(`${q.plowhorsesCount || 0} items`);
    $('#qPlowRev').text(`${(q.plowhorsesRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qPuzzleCount').text(`${q.puzzlesCount || 0} items`);
    $('#qPuzzleRev').text(`${(q.puzzlesRevenueShare || 0).toFixed(1)}% revenue`);
    $('#qDogCount').text(`${q.dogsCount || 0} items`);
    $('#qDogRev').text(`${(q.dogsRevenueShare || 0).toFixed(1)}% revenue`);

    // Section 1: Bottom 10% Removal Table
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

    // Section: Title Improvements Table (Render ONLY if needed)
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
                    <td style="font-size:12px; color:#374151;">${t.reasoning || ''}</td>
                </tr>
            `);
        });
        $titleSection.show();
    } else {
        $titleSection.hide();
    }

    // Section 2: Pricing Optimization Table
    const $priceTbody = $('#aiPricingTable tbody');
    $priceTbody.empty();
    const pricing = data.pricingOptimizations || [];
    if (pricing.length === 0) {
        $priceTbody.append(`<tr><td colspan="7" style="text-align:center; color:#6b7280; padding:16px;">Current pricing is aligned across major categories.</td></tr>`);
    } else {
        pricing.forEach(p => {
            $priceTbody.append(`
                <tr>
                    <td><strong>${p.name || 'Item'}</strong></td>
                    <td>$${(p.currentPrice || 0).toFixed(2)}</td>
                    <td style="color:#059669; font-weight:700; font-size:14px;">$${(p.recommendedPrice || 0).toFixed(2)}</td>
                    <td>$${(p.currentMargin || 0).toFixed(2)}</td>
                    <td style="font-weight:600;">$${(p.projectedMargin || 0).toFixed(2)}</td>
                    <td style="color:#059669; font-weight:700;">+$${(p.estimatedMonthlyProfitIncrease || 0).toFixed(2)}/mo</td>
                    <td style="font-size:12px; color:#374151;">${p.rationale || ''}</td>
                </tr>
            `);
        });
    }

    // Section: Action Plan Timeline
    const $timeline = $('#aiTimelineContainer');
    $timeline.empty();
    const actions = data.actionPlan || [];
    actions.forEach((act, idx) => {
        const cls = idx === 0 ? 'immediate' : (idx === 1 ? 'short' : 'medium');
        $timeline.append(`
            <div class="timeline-item ${cls}">
                <div class="timeline-header">${act.timeframe || ''}: ${act.actionTitle || ''}</div>
                <div class="timeline-desc">${act.details || ''}</div>
                <div class="timeline-impact"><i class="fas fa-arrow-trend-up"></i> Projected Impact: <strong>${act.expectedImpact || ''}</strong></div>
            </div>
        `);
    });

    $('#aiLoadingIndicator').hide();
    $('#aiReportContainer').fadeIn(200);
}

/* ── 2. Centered Modal Controller ───────────────────────────────────────── */

function openCenteredModal(modalId) {
    const $m = $('#' + modalId);
    $m.addClass('show').css({
        'display': 'flex',
        'align-items': 'center',
        'justify-content': 'center'
    }).hide().fadeIn(200);
}

function closeCenteredModal(modalId) {
    $('#' + modalId).fadeOut(200, function () {
        $(this).removeClass('show').css('display', 'none');
    });
}

/* ── 3. Full Menu AI PDF Export ─────────────────────────────────────────── */

function exportAiInsightsPdf(reportData) {
    if (!reportData) {
        showNotification('Report data unavailable for PDF export.', 'error');
        return;
    }

    const summary = reportData.executiveSummary || {};
    const quadrants = reportData.quadrantBreakdown || {};
    const removals = reportData.removalRecommendations || [];
    const titles = reportData.titleImprovements || [];
    const pricing = reportData.pricingOptimizations || [];
    const plan = reportData.actionPlan || [];

    const docDefinition = {
        pageSize: 'A4',
        pageOrientation: 'portrait',
        pageMargins: [36, 36, 36, 36],
        styles: {
            reportTitle: { fontSize: 17, bold: true, color: '#f08d11', margin: [0, 0, 0, 3] },
            reportSubtitle: { fontSize: 10, color: '#4b5563', margin: [0, 0, 0, 10] },
            sectionHeader: { fontSize: 12, bold: true, color: '#111827', margin: [0, 10, 0, 5] },
            tableHeader: { fontSize: 8.5, bold: true, color: '#ffffff', fillColor: '#374151', alignment: 'left' },
            tableCell: { fontSize: 8, color: '#374151', margin: [0, 2, 0, 2] },
            calloutBox: { fontSize: 8.5, color: '#1f2937', lineHeight: 1.3 }
        },
        defaultStyle: { fontSize: 8.5, color: '#374151' },
        content: [
            {
                columns: [
                    { text: 'MENU OPTICS — AI STRATEGIC REPORT', style: 'reportTitle' },
                    { text: `Generated: ${reportData.generatedDate || new Date().toLocaleDateString()}`, alignment: 'right', fontSize: 8, color: '#6b7280' }
                ]
            },
            {
                text: `Restaurant: ${reportData.restaurantName || 'Menu Optics'} | Location: ${reportData.location || 'Local Trade Area'} | Range: ${reportData.dateRange || 'Last 30 Days'} | Engine: ${reportData.sourceModel || 'MenuOptics AI'}`,
                style: 'reportSubtitle'
            },
            { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 523, y2: 0, lineWidth: 1.5, lineColor: '#f08d11' }] },
            { text: '', margin: [0, 4, 0, 0] },

            // Local Competitor Benchmark Callout Box
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
                            { text: 'Est. Monthly Profit Uplift', bold: true, fillColor: '#f9fafb' },
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

            // 4. Strategic Menu Title & Naming Enhancements (If present)
            ...(titles.length > 0 ? [
                { text: '4. STRATEGIC MENU TITLE & NAMING ENHANCEMENTS', style: 'sectionHeader' },
                {
                    table: {
                        widths: ['25%', '30%', '45%'],
                        body: [
                            [
                                { text: 'Current Title', style: 'tableHeader' },
                                { text: 'Suggested High-Converting Title', style: 'tableHeader' },
                                { text: 'Competitor & Appetite Rationale', style: 'tableHeader' }
                            ],
                            ...titles.map(t => [
                                { text: t.currentTitle || '', style: 'tableCell', color: '#6b7280' },
                                { text: t.suggestedTitle || '', bold: true, color: '#6d28d9', style: 'tableCell' },
                                { text: t.reasoning || '', style: 'tableCell', fontSize: 7.5 }
                            ])
                        ]
                    },
                    layout: 'lightHorizontalLines'
                }
            ] : []),

            { text: '5. TACTICAL PRICING & MARGIN OPTIMIZATION', style: 'sectionHeader' },
            {
                table: {
                    widths: ['22%', '12%', '14%', '13%', '14%', '25%'],
                    body: [
                        [
                            { text: 'Menu Item', style: 'tableHeader' },
                            { text: 'Current', style: 'tableHeader' },
                            { text: 'Recommended', style: 'tableHeader' },
                            { text: 'Proj. Margin', style: 'tableHeader' },
                            { text: 'Est. Gain', style: 'tableHeader' },
                            { text: 'Rationale', style: 'tableHeader' }
                        ],
                        ...(pricing.length > 0 ? pricing.map(p => [
                            { text: p.name || '', bold: true, style: 'tableCell' },
                            { text: `$${(p.currentPrice || 0).toFixed(2)}`, style: 'tableCell' },
                            { text: `$${(p.recommendedPrice || 0).toFixed(2)}`, bold: true, color: '#059669', style: 'tableCell' },
                            { text: `$${(p.projectedMargin || 0).toFixed(2)}`, style: 'tableCell' },
                            { text: `+$${(p.estimatedMonthlyProfitIncrease || 0).toFixed(2)}/mo`, bold: true, color: '#059669', style: 'tableCell' },
                            { text: p.rationale || '', style: 'tableCell', fontSize: 7.5 }
                        ]) : [[{ text: 'All prices currently aligned with margin targets.', colSpan: 6, alignment: 'center', style: 'tableCell' }, {}, {}, {}, {}, {}]])
                    ]
                },
                layout: 'lightHorizontalLines'
            },

            { text: '6. 30-60-90 DAY IMPLEMENTATION ROADMAP', style: 'sectionHeader' },
            {
                stack: plan.map(act => ({
                    margin: [0, 2, 0, 5],
                    stack: [
                        { text: `${act.timeframe || ''}: ${act.actionTitle || ''}`, bold: true, color: '#111827' },
                        { text: act.details || '', color: '#4b5563', margin: [0, 1, 0, 1] },
                        { text: `Expected Impact: ${act.expectedImpact || ''}`, color: '#059669', bold: true }
                    ]
                }))
            }
        ]
    };

    const fileName = `Menu_Engineering_AI_Report_${new Date().toISOString().slice(0, 10)}.pdf`;
    pdfMake.createPdf(docDefinition).download(fileName);
    showNotification('AI Menu Engineering Report PDF downloaded successfully!', 'success');
}
