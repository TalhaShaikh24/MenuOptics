let menuItemsGlobal = [];
let eventOnlyItemsGlobal = [];
let allActiveItems = [];
let activeMenuItems = [];
let activeEventItems = [];
let allEventsData = [];
let eventTable;

const profilePic = (() => {
    const value = localStorage.getItem("profilePic");
    return value && value !== "null" ? value : '/images/menu optics_logo.png';
})();

const restaurantAddress = localStorage.getItem("address");
const restaurantName = localStorage.getItem("restaurantName");
const restaurantPhone = localStorage.getItem("phone");

$(document).ready(function () {
    loadEvents();
    loadMenuItems();
    loadEventOnlyItems();
    initEventCalculations();

    $('#eventSearch').on('keyup', function () {
        eventTable?.search(this.value).draw();
    });

    $('#exportCsvBtn').on('click', () => $('.buttons-csv').click());
    $('#exportPdfBtn').on('click', () => exportEventsPDF($('#customerFacing').is(':checked')));

    // Global Mode Listener
    $(document).on('change', '#globalCalcMode', function () {
        applyGlobalMode(this.value);
    });
});

async function getImageAsPngBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = "anonymous";
        img.onload = () => {
            const targetWidth = 350;
            const aspect = img.naturalHeight / img.naturalWidth;
            const targetHeight = Math.round(targetWidth * aspect);

            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;

            canvas.getContext('2d').drawImage(img, 0, 0, targetWidth, targetHeight);
            resolve(canvas.toDataURL('image/png'));
        };
        img.onerror = () => reject(new Error("Image load failed"));
        img.src = url;
    });
}

function padNumber(num, size) {
    return ('000000' + num).slice(-size);
}

function addDays(dateStr, days) {
    const d = new Date(dateStr);
    d.setDate(d.getDate() + days);
    return d.toISOString().split('T')[0];
}

function getAjaxPromise(options) {
    return new Promise((resolve, reject) => {
        ajaxRequest({
            ...options,
            onSuccess: resolve,
            onError: reject,
            showMessage: false
        });
    });
}

// ────────────────────────────────────────────────
// DATA LOADING
// ────────────────────────────────────────────────

function loadMenuItems() {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems',
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            menuItemsGlobal = res.data.map(item => ({
                id: item.menuItemId,
                text: item.name,
                cost: item.cost
            }));
            populateSelect2('#menuItemSelect', menuItemsGlobal);
        }
    });
}

function loadEventOnlyItems() {
    ajaxRequest({
        url: '/api/EventOnlyItemApi/GetAll',
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            eventOnlyItemsGlobal = res.data.map(item => ({
                id: item.eventOnlyItemId,
                text: item.name,
                cost: item.defaultCost
            }));
            populateSelect2('#eventOnlyItemSelect', eventOnlyItemsGlobal);
        }
    });
}

function populateSelect2(selector, data) {
    $(selector)
        .off('change')
        .select2({
            data,
            placeholder: 'Select items',
            width: '100%',
            allowClear: true,
            closeOnSelect: false
        })
        .on('change', () => syncListFromSelect2(selector));
}

function loadEvents() {
    ajaxRequest({
        url: '/api/EventApi/GetAllEvents',
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            allEventsData = res.data;
            rebuildEventsTable(res.data);
        }
    });
}

function rebuildEventsTable(events) {
    if ($.fn.DataTable.isDataTable('.events-table')) {
        $('.events-table').DataTable().destroy();
    }
    const tbody = $('.events-table tbody').empty();
    events.forEach(e => {
        const hasNote = e.hasNote || false;
        const row = `
            <tr data-event-id="${e.eventId}">
                <td>
                    <div class="ingredient-info">
                        <p class="ingredient-name">${e.eventName}</p>
                        <p class="ingredient-meta"><i class="fas fa-calendar"></i> ${DateFormatter.formatWithComma(e.eventDate)}</p>
                    </div>
                </td>
                <td><span class="category-badge">${e.numberOfGuests} Guests</span></td>
                <td>$${e.pricePerGuest.toFixed(2)}</td>
                <td>$${e.totalFoodCost.toFixed(2)}</td>
                <td>$${e.totalRevenue.toFixed(2)}</td>
                <td>$${(e.totalRevenue - e.totalFoodCost).toFixed(2)}</td>
                <td>
                    <div class="dropdown">
                        <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                        <div class="dropdown-menu">
                            <a href="#" onclick="editEvent(${e.eventId})"><i class="fas fa-edit"></i> Edit Details</a>
                            <a href="#" onclick="duplicateEvent(${e.eventId})"><i class="fas fa-copy"></i> Duplicate</a>
                            <a href="#" onclick="exportSingleEventPDF(${e.eventId})"><i class="fas fa-file-pdf"></i> Export PDF</a>
                            ${hasNote ? `
                                <a href="#" onclick="viewNote(${e.eventId})"><i class="fas fa-eye"></i> View Note</a>
                                <a href="#" onclick="editNote(${e.eventId})"><i class="fas fa-edit"></i> Edit Note</a>
                            ` : `
                                <a href="#" onclick="addNote(${e.eventId})"><i class="fas fa-plus"></i> Add Note</a>
                            `}
                        </div>
                    </div>
                </td>
            </tr>`;
        tbody.append(row);
    });
    eventTable = $('.events-table').DataTable({
        paging: true, searching: true, ordering: true, pageLength: 10,
        lengthMenu: [10, 25, 50, 100], language: { search: '', searchPlaceholder: 'Search events...' },
        dom: 'Brtip', buttons: [{
            extend: 'csv', text: 'Export CSV', className: 'buttons-csv',
            filename: 'Events_Report_' + new Date().toISOString().split('T')[0],
            exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
        }]
    });
}


// ────────────────────────────────────────────────
// ITEM & CALCULATION MANAGEMENT
// ────────────────────────────────────────────────

function applyGlobalMode(mode) {
    allActiveItems.forEach(item => {
        item.type = (mode === 'Manual') ? 'PerEvent' : 'PerGuest';
    });
    renderRows();
}


function exportSingleEventPDF(eventId) {
    if ($('#customerFacing').is(':checked')) {
        $('#exportEventId').val(eventId);

        // Auto-fill default values for the new inputs
        $('#quoteNumber').val(`INV-${padNumber(eventId, 5)}`);
        $('#quoteDate').val(new Date().toISOString().split('T')[0]); // Today's date
        $('#quoteValidUntil').val(addDays(new Date(), 14)); // 14 days from today
        $('#quoteTerms').val('50% deposit required. Balance due on event day.');
        $('#quoteNotes').val('');

        openModal('customerDetailsModal');
    } else {
        const single = allEventsData.filter(e => e.eventId === eventId);
        if (single.length) {
            const backup = allEventsData;
            allEventsData = single;
            exportEventsPDF(false);
            allEventsData = backup;
        }
    }
}

function processCustomerQuote() {
    const eventId = parseInt($('#exportEventId').val());
    const details = {
        quoteNumber: $('#quoteNumber').val().trim(),
        quoteDate: $('#quoteDate').val(),
        validUntil: $('#quoteValidUntil').val(),
        terms: $('#quoteTerms').val().trim(),
        noteText: $('#custNote').val().trim() || ''
    };


    closeModal('customerDetailsModal');
    generateCustomSinglePDF(eventId, details);
}

async function generateCustomSinglePDF(eventId, customDetails) {
    try {
        const res = await getAjaxPromise({ url: `/api/EventApi/GetEvent/${eventId}`, method: 'GET' });
        const eventData = {
            ...res.data,
            quoteNumber: customDetails.quoteNumber,
            quoteDate: customDetails.quoteDate,
            validUntil: customDetails.validUntil,
            terms: customDetails.terms,
            noteText: customDetails.noteText
        };

        const backup = allEventsData;
        allEventsData = [eventData];
        exportEventsPDF(true);
        allEventsData = backup;
    } catch {
        showNotification('Error preparing quote data', 'error');
    }
}

// ────────────────────────────────────────────────
// PDF GENERATION (both internal & customer-facing)
// ────────────────────────────────────────────────

function exportEventsPDF(isCustomerFacing) {
    if (!isCustomerFacing) {
        // Internal report
        (async () => {
            let logo = null;
            try {
                logo = await getImageAsPngBase64(profilePic);
            } catch { }

            const columns = [
                { text: 'Event Details', style: 'tableHeader' },
                { text: 'Guests', style: 'tableHeader' },
                { text: 'Price/Guest', style: 'tableHeader' },
                { text: 'Total Cost', style: 'tableHeader' },
                { text: 'Total Revenue', style: 'tableHeader' },
                { text: 'Profit', style: 'tableHeader' }
            ];

            const body = allEventsData.map(e => [
                { text: `${e.eventName}\n${DateFormatter.formatWithComma(e.eventDate)}`, style: 'itemRow' },
                { text: e.numberOfGuests, alignment: 'center', style: 'itemRow' },
                { text: `$${e.pricePerGuest.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
                { text: `$${e.totalFoodCost.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
                { text: `$${e.totalRevenue.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
                { text: `$${(e.totalRevenue - e.totalFoodCost).toFixed(2)}`, alignment: 'right', style: 'itemRow' }
            ]);

            const totalRevenue = allEventsData.reduce((sum, e) => sum + e.totalRevenue, 0);
            const totalCost = allEventsData.reduce((sum, e) => sum + e.totalFoodCost, 0);
            const totalProfit = totalRevenue - totalCost;
            const totalGuests = allEventsData.reduce((sum, e) => sum + e.numberOfGuests, 0);
            const avgMargin = totalRevenue > 0 ? ((totalProfit / totalRevenue) * 100).toFixed(2) : 0;

            const doc = {
                header: () => ({
                    margin: [40, 25, 40, 0],
                    columns: [
                        {
                            width: '*',
                            stack: [
                                { text: 'INTERNAL EVENTS REPORT', style: 'title', color: '#f08d11' },
                                { text: `Generated: ${new Date().toLocaleDateString()} ${new Date().toLocaleTimeString()}`, style: 'subTitle' }
                            ]
                        },
                        logo ? { width: 50, image: logo, alignment: 'right' } : {}
                    ]
                }),
                content: [
                    {
                        table: {
                            headerRows: 1,
                            widths: ['*', 'auto', 'auto', 'auto', 'auto', 'auto'],
                            body: [columns, ...body]
                        },
                        layout: {
                            fillColor: row => row === 0 ? '#f08d11' : null,
                            hLineColor: '#da7c05',
                            vLineColor: '#da7c05'
                        },
                        margin: [0, 20, 0, 20]
                    },
                    { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 515, y2: 0, lineWidth: 1, lineColor: '#eeeeee' }] },
                    { text: 'EXECUTIVE SUMMARY', style: 'sectionTitle', margin: [0, 15, 0, 10] },
                    {
                        columns: [
                            {
                                width: '33%', stack: [
                                    { text: 'Performance', style: 'billToLabel' },
                                    { text: `Total Events: ${allEventsData.length}`, style: 'summaryText' },
                                    { text: `Total Guests: ${totalGuests}`, style: 'summaryText' }
                                ]
                            },
                            {
                                width: '33%', stack: [
                                    { text: 'Financials', style: 'billToLabel' },
                                    { text: `Revenue: $${totalRevenue.toFixed(2)}`, style: 'summaryText' },
                                    { text: `Costs: $${totalCost.toFixed(2)}`, style: 'summaryText' }
                                ]
                            },
                            {
                                width: '34%', stack: [
                                    { text: 'Profitability', style: 'billToLabel' },
                                    { text: `Total Profit: $${totalProfit.toFixed(2)}`, style: 'summaryText', bold: true, color: '#da7c05' },
                                    { text: `Avg Margin: ${avgMargin}%`, style: 'summaryText' }
                                ]
                            }
                        ]
                    }
                ],
                styles: {
                    title: { fontSize: 18, bold: true },
                    subTitle: { fontSize: 9, color: '#666666' },
                    tableHeader: { color: 'white', bold: true, alignment: 'center', fontSize: 10 },
                    itemRow: { fontSize: 9, margin: [0, 3, 0, 3] },
                    sectionTitle: { fontSize: 12, bold: true, color: '#333333', letterSpacing: 1 },
                    billToLabel: { fontSize: 10, bold: true, color: '#000000', margin: [0, 0, 0, 4] },
                    summaryText: { fontSize: 10, color: '#444444' }
                },
                pageSize: 'A4',
                pageMargins: [40, 90, 40, 60]
            };

            pdfMake.createPdf(doc).download(`Internal_Events_Report_${new Date().toISOString().split('T')[0]}.pdf`);
        })();
    } else {
        // Customer-facing quote
        Promise.all(allEventsData.map(e =>
            Promise.all([
                getAjaxPromise({ url: `/api/EventApi/GetEvent/${e.eventId}`, method: 'GET' }),
                (e.noteText)
                    ? Promise.resolve({ data: { noteText: e.noteText } })
                    : (e.hasNote
                        ? getAjaxPromise({ url: `/api/EventApi/GetEventNote/${e.eventId}`, method: 'GET' })
                        : Promise.resolve({ data: { noteText: "" } }))
            ]).then(([eventRes, noteRes]) => ({
                ...e,
                ...eventRes.data,
                noteText: e.noteText || noteRes.data.noteText || ""
            }))
        )).then(async events => {
            let logo = null;
            try { logo = await getImageAsPngBase64(profilePic); } catch { }

            const content = [];

            events.forEach((event, idx) => {
                if (idx > 0) content.push({ pageBreak: 'before', text: '' });

                content.push({
                    columns: [
                        {
                            width: '*',
                            stack: [
                                { text: restaurantName, style: 'companyTitle' },
                                { text: restaurantAddress, style: 'companyText' },
                                { text: restaurantPhone, style: 'companyText' }
                            ]
                        },
                        logo ? { width: 120, stack: [{ image: logo, width: 70, alignment: 'right' }] } : {}
                    ],
                    margin: [0, 0, 0, 20]
                });

                content.push({
                    columns: [
                        {
                            width: '38%',
                            stack: [
                                { text: 'BILL TO', style: 'sectionHeader' },
                                {
                                    text: [
                                        { text: 'Name: ', bold: true },
                                        { text: ($("#custName").val().trim()) + '\n' },

                                        { text: 'Company: ', bold: true },
                                        { text: ($("#custCompany").val().trim()) + '\n' },

                                        { text: 'Address: ', bold: true },
                                        { text: ($("#custAddress").val().trim()) + '\n' },

                                        { text: 'Phone: ', bold: true },
                                        { text: ($("#custPhone").val().trim()) + '\n' },

                                        { text: 'Email: ', bold: true },
                                        { text: ($("#custName").val().trim()) }
                                    ],
                                    style: 'infoText'
                                }
                            ]
                        },
                        {
                            width: '38%',
                            stack: [
                                { text: 'EVENT DETAILS', style: 'sectionHeader' },
                                {
                                    text: [
                                        { text: 'Event Name: ', bold: true }, { text: event.eventName + '\n' },
                                        { text: 'Date: ', bold: true }, { text: DateFormatter.formatWithComma(event.eventDate) + '\n' },
                                        { text: 'Time: ', bold: true }, { text: (DateFormatter.format12?.(event.eventDate) || "") + '\n' },
                                        { text: 'Guests: ', bold: true }, { text: event.numberOfGuests + '\n' },
                                        { text: 'Location: ', bold: true }, { text: (event.location || 'TBD') }
                                    ],
                                    style: 'infoText'
                                }
                            ]
                        },
                        {
                            width: '24%',
                            stack: [
                                { text: 'REFERENCE', style: 'sectionHeader', alignment: 'right' },
                                { text: `Quote #: ${event.quoteNumber || 'INV-' + padNumber(event.eventId, 5)}`, style: 'metaLabel', alignment: 'right' },
                                // Format the date strings back to locale strings if they exist
                                { text: `Date: ${event.quoteDate ? new Date(event.quoteDate).toLocaleDateString() : new Date().toLocaleDateString()}`, style: 'metaLabel', alignment: 'right' },
                                { text: `Valid Until: ${event.validUntil ? new Date(event.validUntil).toLocaleDateString() : addDays(new Date(), 14)}`, style: 'metaLabel', alignment: 'right' }
                            ]
                        }
                    ],
                    margin: [0, 0, 0, 20]
                });

                // Items table
                const headers = [
                    { text: 'DESCRIPTION', style: 'tableHeader', alignment: 'left' },
                    { text: 'QTY', style: 'tableHeader', alignment: 'center' },
                    { text: 'PRICE', style: 'tableHeader', alignment: 'right' },
                    { text: 'TOTAL', style: 'tableHeader', alignment: 'right' }
                ];

                let subtotal = 0;
                const rows = [];

                if (event.items?.length && event.totalFoodCost > 0) {
                    event.items.forEach(i => {
                        const proportion = i.totalCost / event.totalFoodCost;
                        const sellingTotal = proportion * event.totalRevenue;
                        let qty = i.qty;
                        let desc = i.itemName;

                        if (i.qtyType === 'PerGuest') qty *= event.numberOfGuests;
                        else if (i.qtyType === 'PerEvent') desc += ` (serves ${event.numberOfGuests})`;

                        const unitPrice = qty > 0 ? sellingTotal / qty : 0;
                        subtotal += sellingTotal;

                        rows.push([
                            { text: desc, style: 'tableRow' },
                            { text: qty.toFixed(0), style: 'tableRow', alignment: 'center' },
                            { text: `$${unitPrice.toFixed(2)}`, style: 'tableRow', alignment: 'right' },
                            { text: `$${sellingTotal.toFixed(2)}`, style: 'tableRow', alignment: 'right' }
                        ]);
                    });
                } else {
                    subtotal = event.totalRevenue;
                    rows.push([
                        { text: 'Full Catering Service', style: 'tableRow' },
                        { text: event.numberOfGuests, style: 'tableRow', alignment: 'center' },
                        { text: `$${event.pricePerGuest.toFixed(2)}`, style: 'tableRow', alignment: 'right' },
                        { text: `$${event.totalRevenue.toFixed(2)}`, style: 'tableRow', alignment: 'right' }
                    ]);
                }

                content.push({
                    table: {
                        headerRows: 1,
                        widths: ['*', 'auto', 'auto', 'auto'],
                        body: [headers, ...rows]
                    },
                    layout: {
                        fillColor: row => row === 0 ? '#f08d11' : (row % 2 === 0 ? '#f9f9f9' : null),
                        hLineWidth: i => (i === 0 || i === 1) ? 1 : 0.5,
                        vLineWidth: () => 0,
                        hLineColor: '#da7c05',
                        padding: [8, 6]
                    },
                    margin: [0, 0, 0, 15]
                });

                content.push({
                    columns: [
                        { width: '*', text: '' },
                        {
                            width: 200,
                            table: {
                                widths: ['*', 80],
                                body: [
                                    [{ text: 'Subtotal:', bold: true, alignment: 'right', fontSize: 10 }, { text: `$${subtotal.toFixed(2)}`, alignment: 'right', fontSize: 10 }],
                                    [{ text: 'Total:', bold: true, fontSize: 12, alignment: 'right', color: '#f08d11' }, { text: `$${subtotal.toFixed(2)}`, bold: true, fontSize: 12, alignment: 'right', color: '#f08d11' }]
                                ]
                            },
                            layout: 'noBorders'
                        }
                    ],
                    margin: [0, 0, 0, 20]
                });

                // Aas paas line 351 ke andar:
                const terms = event.terms || event.paymentTerms || '';
                
                const notes = event.noteText || '';

                content.push({
                    columns: [
                        {
                            width: '55%',
                            stack: [
                                { text: 'Terms & Conditions', style: 'footerHeader' },
                                { text: terms, style: 'footerText', margin: [0, 0, 0, 25] },
                                {
                                    stack: [
                                        { canvas: [{ type: 'line', x1: 0, y1: 0, x2: 200, y2: 0, lineWidth: 1, lineColor: '#555' }] },
                                        { text: 'Customer Signature', fontSize: 9, italics: true, color: '#555', margin: [0, 4, 0, 0] }
                                    ]
                                }
                            ]
                        },
                        { width: '5%', text: '' },
                        {
                            width: '40%',
                            stack: [
                                { text: 'Notes', style: 'footerHeader', alignment: 'right' },
                                { text: notes, style: 'footerText', alignment: 'right' }
                            ]
                        }
                    ],
                    margin: [0, 0, 0, 10]
                });

                content.push({ text: 'Thank you for your business!', alignment: 'center', fontSize: 10, italics: true, color: '#777', margin: [0, 15, 0, 0] });
            });

            const doc = {
                content,
                styles: {
                    companyTitle: { fontSize: 16, bold: true, color: '#f08d11' },
                    companyText: { fontSize: 10, color: '#555', lineHeight: 1.3 },
                    sectionHeader: { fontSize: 10, bold: true, color: '#da7c05', margin: [0, 0, 0, 4], decoration: 'underline' },
                    infoText: { fontSize: 10, color: '#333', lineHeight: 1.4 },
                    metaLabel: { fontSize: 10, color: '#555' },
                    tableHeader: { fontSize: 10, bold: true, color: 'white' },
                    tableRow: { fontSize: 10, color: '#333' },
                    footerHeader: { fontSize: 10, bold: true, color: '#f08d11', margin: [0, 0, 0, 3] },
                    footerText: { fontSize: 9, color: '#444' }
                },
                defaultStyle: { font: 'Roboto' },
                pageSize: 'A4',
                pageMargins: [30, 30, 30, 30]
            };

            pdfMake.createPdf(doc).download(`Quote_${new Date().toISOString().split('T')[0]}.pdf`);
        }).catch(() => {
            showNotification('Error generating PDF.', 'error');
        });
    }
}

// ────────────────────────────────────────────────
// ITEM MANAGEMENT
// ────────────────────────────────────────────────

function syncListFromSelect2(selector) {
    const selectedIds = $(selector).val() || [];
    const isMenu = selector === '#menuItemSelect';
    const source = isMenu ? 'Menu' : 'EventOnly';
    const sourceData = isMenu ? menuItemsGlobal : eventOnlyItemsGlobal;
    const currentMode = $('#globalCalcMode').val();

    allActiveItems = allActiveItems.filter(item =>
        item.source !== source || selectedIds.includes(item.id.toString())
    );

    // Keep the respective array in sync with the current selection
    if (isMenu) {
        activeMenuItems = activeMenuItems.filter(item => selectedIds.includes(item.id.toString()));
    } else {
        activeEventItems = activeEventItems.filter(item => selectedIds.includes(item.id.toString()));
    }

    selectedIds.forEach(selId => {
        if (!allActiveItems.some(x => x.id == selId && x.source === source)) {
            const ref = sourceData.find(x => x.id == selId);
            if (ref) {
                const newItem = {
                    id: ref.id, name: ref.text, cost: ref.cost, qty: 1,
                    type: (currentMode === 'Manual') ? 'PerEvent' : (isMenu ? 'PerGuest' : 'PerEvent'),
                    source
                };
                allActiveItems.push(newItem);

                // Push into its respective array
                if (isMenu) {
                    activeMenuItems.push(newItem);
                } else {
                    activeEventItems.push(newItem);
                }
            }
        }
    });
    renderRows();
}

function renderRows() {
    const container = $('#eventItemsContainer').empty();
    const isManual = $('#globalCalcMode').val() === 'Manual';

    allActiveItems.forEach((item, idx) => {
        const badgeClass = item.source === 'Menu' ? 'badge-menu' : 'badge-event';

        const row = `
            <div class="event-item-row" data-index="${idx}" style="display:flex; align-items:center; gap:8px; margin-bottom:8px; border-bottom:1px solid #eee; padding-bottom:5px;">
                <span style="min-width:150px; flex-grow:1; font-weight:500;">
                    <span class="${badgeClass}">${item.source}</span> ${item.name}
                </span>
                <span style="min-width:80px; text-align:right; font-size:0.9em;">$${item.cost.toFixed(2)}</span>
                <div style="display:flex; flex-direction:column; width:100px;">
                    <input type="number" class="form-control item-qty" value="${item.qty}" min="0" step="1"
                           onchange="updateItemState(${idx}, 'qty', this.value)" 
                           style="height:30px; ${isManual ? 'border:1px solid #f08d11;' : ''}">
                </div>
                <span class="row-total" style="min-width:120px; text-align:right; font-weight:bold;">$0.00</span>
                <a href="#" onclick="removeItemByIndex(${idx})" class="text-danger ml-2"><i class="fa-regular fa-trash-can"></i></a>
            </div>`;
        container.append(row);
    });
    updateAllCosts();
}
function updateItemState(index, field, value) {
    const item = allActiveItems[index];
    if (!item) return;
    if (field === 'qty') item.qty = parseFloat(value) || 0;
    updateAllCosts();
}


function removeItemByIndex(index) {
    const item = allActiveItems[index];
    if (!item) return;
    allActiveItems.splice(index, 1);
    const select = item.source === 'Menu' ? '#menuItemSelect' : '#eventOnlyItemSelect';
    const current = $(select).val() || [];
    $(select).val(current.filter(id => id != item.id)).trigger('change.select2');
    renderRows();
}

// ────────────────────────────────────────────────
// CALCULATIONS
// ─────────────────��──────────────────────────────

function initEventCalculations() {
    $('#guestCount, #pricePerGuest').on('input keyup', updateAllCosts);
}

function updateAllCosts() {
    const guests = parseFloat($('#guestCount').val()) || 0;
    const isManualMode = $('#globalCalcMode').val() === 'Manual';
    let menuFoodCost = 0;      // Total Cost -> Menu items only
    let eventOnlyCost = 0;     // Added to Total Revenue

    $('.event-item-row').each(function () {
        const idx = $(this).data('index');
        const item = allActiveItems[idx];
        if (!item) return;

        const lineTotal = item.qty * item.cost;

        item.totalCost = lineTotal;
        $(this).find('.row-total').text(`$${lineTotal.toFixed(2)}`);

        // Total Cost is built only from Menu items
        if (item.source === 'Menu') {
            menuFoodCost += lineTotal;
        }
        // Event-Only items are added to Revenue, not to food cost
        else if (item.source === 'EventOnly') {
            eventOnlyCost += lineTotal;
        }
    });

    const pricePerGuest = parseFloat($('#pricePerGuest').val()) || 0;
    const baseRevenue = guests * pricePerGuest;

    // Total Revenue = (guests x price per guest) + Event-Only items cost
    const totalRevenue = baseRevenue + eventOnlyCost;
    // Total Cost = Menu items only
    const totalCost = menuFoodCost;

    $('#totalRevenue').text(`$${totalRevenue.toFixed(2)}`);
    $('#totalFoodCost').text(`$${totalCost.toFixed(2)}`);

    const costPerGuest = guests > 0 ? totalCost / guests : 0;
    $('#costPerGuest').text(`$${costPerGuest.toFixed(2)}`);
    const foodCostPct = totalRevenue > 0 ? (totalCost / totalRevenue) * 100 : 0;
    $('#foodCostPercent').text(`${foodCostPct.toFixed(2)}%`).css('color', foodCostPct > 35 ? 'red' : 'green');
    const margin = totalRevenue - totalCost;
    $('#grossMargin').text(`$${margin.toFixed(2)}`);
    const marginPct = totalRevenue > 0 ? (margin / totalRevenue) * 100 : 0;
    $('#grossMarginPercent').text(`${marginPct.toFixed(2)}%`);
}

// ────────────────────────────────────────────────
// MODAL & UI HELPERS
// ────────────────────────────────────────────────

function openNewEventModal() {
    allActiveItems = [];
    $('#addEventForm')[0].reset();
    $('#eventId').val('');
    $('#globalCalcMode').val('PerGuest');
    $('#menuItemSelect, #eventOnlyItemSelect').val(null).trigger('change.select2');
    renderRows();
    resetTabs();
    openModal('eventModal');
}
function openCreateCustomItemModal() {
    $('#newCustomName, #newCustomCost').val('');
    openModal('customItemModal');
}

function saveNewCustomItem() {
    const name = $('#newCustomName').val().trim();
    const cost = parseFloat($('#newCustomCost').val());

    if (!name || isNaN(cost)) {
        alert('Please fill all fields correctly');
        return;
    }

    ajaxRequest({
        url: '/api/EventOnlyItemApi/Create',
        method: 'POST',
        data: { Name: name, DefaultCost: cost },
        onSuccess: res => {
            closeModal('customItemModal');
            const item = {
                id: res.data.eventOnlyItemId,
                text: res.data.name,
                cost: res.data.defaultCost
            };
            eventOnlyItemsGlobal.push(item);
            populateSelect2('#eventOnlyItemSelect', eventOnlyItemsGlobal);

            const vals = $('#eventOnlyItemSelect').val() || [];
            vals.push(item.id);
            $('#eventOnlyItemSelect').val(vals).trigger('change');
        }
    });
}

// ────────────────────────────────────────────────
// SAVE & EDIT EVENT
// ────────────────────────────────────────────────

function saveEvent() {
    const name = $('#eventName').val().trim();
    const date = $('#eventDate').val();
    const guests = parseFloat($('#guestCount').val()) || 0;
    const price = parseFloat($('#pricePerGuest').val()) || 0;
    const isManualMode = $('#globalCalcMode').val() === 'Manual';

    // Basic Validations
    if (!name || !date || guests <= 0) {
        return showNotification('Please fill required fields and enter valid guest count.', 'error');
    }

    // 🔹 NEW CONDITION: Total items quantity must match Guest Count in Manual Mode
    if (isManualMode && allActiveItems.length > 0) {
        let totalItemsQty = 0;
        allActiveItems.forEach(item => {
            // Hum sirf un items ko count kar rahe hain jo "Per Guest" (Menu items mostly) wali nature ke hain
            if (item.type === 'PerEvent' || item.type === 'PerGuest') {
                totalItemsQty += item.qty;
            }
        });

        if (totalItemsQty !== guests) {
            return showNotification(`Validation Error: Total items quantity (${totalItemsQty}) must be exactly equal to the Number of Guests (${guests}).`, 'error');
        }
    }

    const items = allActiveItems.map(i => ({
        MenuItemId: i.source === 'Menu' ? i.id : 0,
        EventOnlyItemId: i.source === 'EventOnly' ? i.id : 0,
        ItemName: i.name,
        Qty: i.qty,
        QtyType: i.type,
        UnitCost: i.cost,
        TotalCost: i.totalCost,
        ItemSource: i.source
    }));

    const payload = {
        EventId: $('#eventId').val() || 0,
        EventName: name,
        EventDate: date,
        NumberOfGuests: guests,
        PricePerGuest: price,
        Location: $('#location').val(),
        ContactName: $('#contactName').val(),
        ContactEmail: $('#contactEmail').val(),
        ContactPhone: $('#contactPhone').val(),
        TotalFoodCost: parseFloat($('#totalFoodCost').text().replace('$', '')),
        TotalRevenue: parseFloat($('#totalRevenue').text().replace('$', '')),
        Items: items
    };

    const isUpdate = payload.EventId != 0;
    ajaxRequest({
        url: isUpdate ? '/api/EventApi/UpdateEvent' : '/api/EventApi/CreateEvent',
        method: isUpdate ? 'PUT' : 'POST',
        data: payload,
        onSuccess: () => {
            closeModal('eventModal');
            loadEvents();
            showNotification('Event saved successfully!', 'success');
        },
        onError: (err) => {
            showNotification('Error saving event: ' + err.statusText, 'error');
        }
    });
}

function editEvent(id) {
    ajaxRequest({
        url: `/api/EventApi/GetEvent/${id}`,
        method: 'GET',
        onSuccess: res => {
            const e = res.data;
            openModal('eventModal');
            $('#eventId').val(e.eventId);
            $('#eventName').val(e.eventName);
            $('#eventDate').val(e.eventDate);
            $('#guestCount').val(e.numberOfGuests);
            $('#pricePerGuest').val(e.pricePerGuest);
            $('#location').val(e.location);
            $('#contactName').val(e.contactName);
            $('#contactEmail').val(e.contactEmail);
            $('#contactPhone').val(e.contactPhone);

            allActiveItems = [];
            const menuIds = [], eventIds = [];
            let hasManual = e.items?.some(i => i.qtyType === 'PerEvent' && i.itemSource === 'Menu');
            $('#globalCalcMode').val(hasManual ? 'Manual' : 'PerGuest');

            e.items?.forEach(i => {
                const source = i.itemSource || (i.menuItemId ? 'Menu' : 'EventOnly');
                const idVal = source === 'Menu' ? i.menuItemId : i.eventOnlyItemId;
                allActiveItems.push({ id: idVal, name: i.itemName, cost: i.unitCost, qty: i.qty, type: i.qtyType, source });
                if (source === 'Menu') menuIds.push(idVal); else eventIds.push(idVal);
            });
            $('#menuItemSelect').val(menuIds).trigger('change.select2');
            $('#eventOnlyItemSelect').val(eventIds).trigger('change.select2');
            renderRows();
        }
    });
}

function duplicateEvent(id) {
    if (!confirm("Duplicate this event?")) return;
    ajaxRequest({
        url: `/api/EventApi/DuplicateEvent/${id}`,
        method: 'POST',
        onSuccess: res => {
            showNotification('Event duplicated successfully', 'success');
            loadEvents();
            if (res.data?.eventId) setTimeout(() => editEvent(res.data.eventId), 500);
        }
    });
}

// ────────────────────────────────────────────────
// NOTES
// ────────────────────────────────────────────────

function addNote(eventId) {
    $('#noteEventId').val(eventId);
    $('#noteText').val('').prop('readonly', false);
    $('#noteModalTitle').text('Add Event Note');
    $('#noteMetaInfo').html('');
    $('#deleteNoteBtn').hide();
    openModal('noteModal');
}

function editNote(eventId) {
    $('#noteEventId').val(eventId);
    $('#noteModalTitle').text('Edit Event Note');
    $('#noteText').prop('readonly', false);
    $('#deleteNoteBtn').show();

    ajaxRequest({
        url: `/api/EventApi/GetEventNote/${eventId}`,
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            if (res.data) {
                $('#noteText').val(res.data.noteText || '');
                let meta = '';
                if (res.data.createdDate) meta += `<small><i class="fas fa-clock"></i> Created: ${DateFormatter.formatWithComma(res.data.createdDate)}</small>`;
                if (res.data.modifiedDate) meta += `<br><small><i class="fas fa-edit"></i> Modified: ${DateFormatter.formatWithComma(res.data.modifiedDate)}</small>`;
                $('#noteMetaInfo').html(meta);
                openModal('noteModal');
            }
        }
    });
}

function viewNote(eventId) {
    $('#noteEventId').val(eventId);
    $('#noteModalTitle').text('View Event Note');
    $('#noteText').prop('readonly', true);
    $('#deleteNoteBtn').hide();

    ajaxRequest({
        url: `/api/EventApi/GetEventNote/${eventId}`,
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            if (res.data) {
                $('#noteText').val(res.data.noteText || '');
                let meta = '';
                if (res.data.createdDate) meta += `<small><i class="fas fa-clock"></i> Created: ${DateFormatter.formatWithComma(res.data.createdDate)}</small>`;
                if (res.data.modifiedDate) meta += `<br><small><i class="fas fa-edit"></i> Modified: ${DateFormatter.formatWithComma(res.data.modifiedDate)}</small>`;
                $('#noteMetaInfo').html(meta);
                openModal('noteModal');
            }
        }
    });
}

function saveNote() {
    const eventId = $('#noteEventId').val();
    const text = $('#noteText').val().trim();

    if (!text) return showNotification('Please enter note text', 'warning');

    ajaxRequest({
        url: '/api/EventApi/SaveEventNote',
        method: 'POST',
        data: { EventId: parseInt(eventId), NoteText: text },
        onSuccess: () => {
            closeModal('noteModal');
            showNotification('Note saved', 'success');
            loadEvents();
        },
        onError: () => showNotification('Failed to save note', 'error')
    });
}

function deleteNote() {
    const eventId = $('#noteEventId').val();
    if (!confirm('Delete this note?')) return;

    ajaxRequest({
        url: `/api/EventApi/DeleteEventNote/${eventId}`,
        method: 'DELETE',
        onSuccess: () => {
            closeModal('noteModal');
            showNotification('Note deleted', 'success');
            loadEvents();
        },
        onError: () => showNotification('Failed to delete note', 'error')
    });
}

// ────────────────────────────────────────────────
// TABS
// ────────────────────────────────────────────────

function openTab(evt, tabId) {
    $('.tab-content-area').removeClass('active').hide(); $('.tab-btn').removeClass('active');
    $('#' + tabId).addClass('active').show(); $(evt.currentTarget).addClass('active');
}

function resetTabs() {
    $('.tab-content-area').hide().removeClass('active'); $('#tab-menu-items').show().addClass('active');
    $('.tab-btn').removeClass('active').first().addClass('active');
}