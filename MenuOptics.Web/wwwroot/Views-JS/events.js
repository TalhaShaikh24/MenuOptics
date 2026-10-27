/* ============================================================================
 * events.js — Event management page (Views/Event/Index.cshtml)
 * Event table, item cost calculations, notes and customer/staff PDF export.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 1. Constants ─────────────────────────────────────────────────────── */

const profilePic = (() => {
    const value = localStorage.getItem("profilePic");
    return value && value !== "null" ? $('#imageBaseUrl').val() + value : '/images/menu optics_logo.png';
})();

const restaurantAddress = localStorage.getItem("address");
const restaurantName = localStorage.getItem("restaurantName");
const restaurantPhone = localStorage.getItem("phone");

/* ── 2. State ─────────────────────────────────────────────────────────── */

let menuItemsGlobal = [];
let eventOnlyItemsGlobal = [];
let allActiveItems = [];
let allEventsData = [];
let eventTable;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    loadEvents();
    loadMenuItems();
    loadEventOnlyItems();
    initEventCalculations();

    $('#eventSearch').on('keyup', function () {
        eventTable?.search(this.value).draw();
    });

    $('#exportCsvBtn').on('click', () => $('.buttons-csv').click());
    $('#exportPdfBtn').on('click', () => exportEventsPDF($('#customerFacing').is(':checked'), allEventsData));
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

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
            const rawItems = res.data?.data || res.data || [];
            eventOnlyItemsGlobal = rawItems.map(item => ({
                id: item.eventOnlyItemId,
                text: item.name,
                cost: item.cost ?? 0,
                sellingPrice: item.sellingPrice ?? 0
            }));
            populateSelect2('#eventOnlyItemSelect', eventOnlyItemsGlobal);
        }
    });
}

function loadEvents() {
    LoadingComponent.showForTable('.events-table', 'Loading events...');
    ajaxRequest({
        url: '/api/EventApi/GetAllEvents',
        method: 'GET',
        showMessage: false,
        onSuccess: res => {
            allEventsData = res.data;
            rebuildEventsTable(res.data);
        },
        onComplete: function () { LoadingComponent.hideForTable('.events-table'); }
    });
}

function toggleEventActive(id) {
    if (!confirm('Are you sure you want to toggle the active status of this event?')) return;

    ajaxRequest({
        url: `/api/EventApi/ToggleEventActive/${id}`,
        method: 'PUT',
        showMessage: false,
        onSuccess: () => loadEvents(),
        onError: (xhr, status, error) => {
            console.error('Failed to toggle event status:', error);
            showNotification('Failed to toggle event status.', 'error');
        }
    });
}

function saveNewCustomItem() {
    const name = $('#newCustomName').val().trim();
    const cost = parseFloat($('#newCustomCost').val());
    const sellingPrice = parseFloat($('#newCustomSellingPrice').val());

    if (!name || isNaN(cost)) {
        alert('Please fill all fields correctly');
        return;
    }

    const validSellingPrice = isNaN(sellingPrice) ? 0 : sellingPrice;

    ajaxRequest({
        url: '/api/EventOnlyItemApi/Create',
        method: 'POST',
        data: { Name: name, Cost: cost, SellingPrice: validSellingPrice },
        onSuccess: res => {
            closeModal('customItemModal');
            const itemData = res.data || {};
            const itemId = itemData.eventOnlyItemId || itemData.EventOnlyItemId;
            if (!itemId) return;
debugger
            eventOnlyItemsGlobal.push({
                id: itemId,
                text: itemData.name || itemData.Name,
                cost: itemData.cost,
                sellingPrice: itemData.sellingPrice ?? validSellingPrice
            });

            populateSelect2('#eventOnlyItemSelect', eventOnlyItemsGlobal);

            const vals = ($('#eventOnlyItemSelect').val() || []).map(v => v.toString());
            const newIdStr = itemId.toString();
            if (!vals.includes(newIdStr)) vals.push(newIdStr);
            $('#eventOnlyItemSelect').val(vals).trigger('change');
        }
    });
}

function saveEvent() {
    const name = $('#eventName').val().trim();
    const date = $('#eventDate').val();
    const guests = parseFloat($('#guestCount').val()) || 0;
    const price = parseFloat($('#pricePerGuest').val()) || 0;

    if (!name || !date || guests <= 0) {
        return showNotification('Please fill required fields and enter valid guest count.', 'error');
    }

    const items = allActiveItems.map(i => ({
        MenuItemId: i.source === 'Menu' ? i.id : 0,
        EventOnlyItemId: i.source === 'EventOnly' ? i.id : 0,
        ItemName: i.name,
        Qty: i.qty,
        UnitCost: i.cost,
        TotalCost: i.totalCost,
        SellingPrice: i.sellingPrice ?? 0,
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
        Notes: $('#noteText').val().trim(),
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
        onError: err => showNotification('Error saving event: ' + err.statusText, 'error')
    });
}

function editEvent(id) {
    ajaxRequest({
        url: `/api/EventApi/GetEvent/${id}`,
        method: 'GET',
        showMessage: false,
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
            $('#noteText').val(e.notes || '');

            allActiveItems = [];
            const menuIds = [], eventIds = [];

            e.items?.forEach(i => {
                const isMenu = !!(i.menuItemId && i.menuItemId > 0);
                const source = isMenu ? 'Menu' : 'EventOnly';
                const idVal = isMenu ? i.menuItemId : i.eventOnlyItemId;
                let sellingPrice = i.sellingPrice;
                let name = i.itemName;

                if (!isMenu) {
                    const found = eventOnlyItemsGlobal.find(x => x.id == idVal);
                    if (found) {
                        if (!name) name = found.text;
                        if (!sellingPrice && found.sellingPrice) sellingPrice = found.sellingPrice;
                    }
                } else if (!name) {
                    name = menuItemsGlobal.find(x => x.id == idVal)?.text;
                }

                allActiveItems.push({
                    id: idVal,
                    name: name || '',
                    cost: i.unitCost,
                    sellingPrice: sellingPrice ?? 0,
                    qty: i.qty || i.quantity || 1,
                    source
                });
                (isMenu ? menuIds : eventIds).push(idVal);
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

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// ── Events table ──

function rebuildEventsTable(events) {
    if ($.fn.DataTable.isDataTable('.events-table')) {
        $('.events-table').DataTable().destroy();
    }
    const tbody = $('.events-table tbody').empty();

    events.forEach(event => {
        const actionText = event.isActive ? 'Deactivate' : 'Activate';
        const actionIcon = event.isActive ? 'fas fa-ban' : 'fas fa-check';
        tbody.append(`
            <tr data-event-id="${event.eventId}">
                <td>
                    <div class="ingredient-info">
                        <p class="ingredient-name">${event.eventName}</p>
                        <p class="ingredient-meta"><i class="fas fa-calendar"></i> ${DateFormatter.formatWithComma(event.eventDate)}</p>
                    </div>
                </td>
                <td><span class="category-badge">${event.numberOfGuests} Guests</span></td>
                <td>$${event.pricePerGuest.toFixed(2)}</td>
                <td>$${event.totalFoodCost.toFixed(2)}</td>
                <td>$${event.totalRevenue.toFixed(2)}</td>
                <td>$${(event.totalRevenue - event.totalFoodCost).toFixed(2)}</td>
                <td><span class="status-badge ${event.isActive ? 'good' : 'critical'}">${event.isActive ? 'Active' : 'Inactive'}</span></td>
                <td>
                    <div class="dropdown">
                        <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                        <div class="dropdown-menu">
                            <a href="#" onclick="editEvent(${event.eventId})"><i class="fas fa-edit"></i> Edit Details</a>
                            <a href="#" onclick="toggleEventActive(${event.eventId})"><i class="${actionIcon}"></i> ${actionText}</a>
                            <a href="#" onclick="duplicateEvent(${event.eventId})"><i class="fas fa-copy"></i> Duplicate</a>
                            <a href="#" onclick="exportSingleEventPDF(${event.eventId})"><i class="fas fa-file-pdf"></i> Export PDF</a>
                        </div>
                    </div>
                </td>
            </tr>`);
    });

    eventTable = $('.events-table').DataTable({
        paging: true, searching: true, ordering: false, pageLength: 10,
        lengthMenu: [10, 25, 50, 100], language: { search: '', searchPlaceholder: 'Search events...' },
        dom: 'Brtip',
        buttons: [{
            extend: 'csv', text: 'Export CSV', className: 'buttons-csv',
            filename: 'Events_Report_' + new Date().toISOString().split('T')[0],
            exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
        }]
    });
}

// ── Item list / cost calculations ──

function populateSelect2(selector, data) {
    $(selector)
        .off('change')
        .empty()
        .select2({
            data,
            placeholder: 'Select items',
            width: '100%',
            allowClear: true,
            closeOnSelect: false
        })
        .on('change', () => syncListFromSelect2(selector));
}

function syncListFromSelect2(selector) {
    const selectedIds = $(selector).val() || [];
    const isMenu = selector === '#menuItemSelect';
    const source = isMenu ? 'Menu' : 'EventOnly';
    const sourceData = isMenu ? menuItemsGlobal : eventOnlyItemsGlobal;

    allActiveItems = allActiveItems.filter(item =>
        item.source !== source || selectedIds.includes(item.id.toString())
    );

    selectedIds.forEach(selId => {
        if (allActiveItems.some(x => x.id == selId && x.source === source)) return;
        const ref = sourceData.find(x => x.id == selId);
        allActiveItems.push({
            id: ref.id,
            name: ref.text,
            cost: ref.cost,
            sellingPrice: ref.sellingPrice ?? 0,
            qty: 1,
            source
        });
    });
    renderRows();
}

function renderRows() {
    const container = $('#eventItemsContainer').empty();

    allActiveItems.forEach((item, idx) => {
        const badgeClass = item.source === 'Menu' ? 'badge-menu' : 'badge-event';

        const priceCell = item.source === 'EventOnly'
            ? `<div style="display:flex; flex-direction:column; width:95px;">
                   <input type="number" class="form-control item-price" value="${item.sellingPrice ?? 0}" min="0" step="0.01"
                          oninput="updateItemState(${idx}, 'price', this.value)"
                          onchange="updateItemState(${idx}, 'price', this.value)"
                          style="height:30px; text-align:right;" placeholder="0.00">
               </div>`
            : `<span style="min-width:95px; text-align:center; color:#9ca3af; font-size:0.9em;">-</span>`;

        container.append(`
            <div class="event-item-row" data-index="${idx}" style="display:flex; align-items:center; gap:8px; margin-bottom:8px; border-bottom:1px solid #eee; padding-bottom:5px;">
                <span style="min-width:140px; flex-grow:1; font-weight:500;">
                    <span class="${badgeClass}">${item.source}</span> ${item.name}
                </span>
                <span style="min-width:70px; text-align:right; font-size:0.9em;">$${item.cost.toFixed(2)}</span>
                ${priceCell}
                <div style="display:flex; flex-direction:column; width:90px;">
                    <input type="number" class="form-control item-qty" value="${item.qty}" min="0" step="1"
                           oninput="updateItemState(${idx}, 'qty', this.value)"
                           onchange="updateItemState(${idx}, 'qty', this.value)"
                           style="height:30px;">
                </div>
                <span class="row-total" style="min-width:80px; text-align:right; font-weight:bold;">$0.00</span>
                <a href="#" onclick="removeItemByIndex(${idx})" class="text-danger ml-2"><i class="fa-regular fa-trash-can"></i></a>
            </div>`);
    });
    updateAllCosts();
}

function updateItemState(index, field, value) {
    const item = allActiveItems[index];
    if (!item) return;
    if (field === 'qty') item.qty = parseFloat(value) || 0;
    if (field === 'price' || field === 'sellingPrice') item.sellingPrice = parseFloat(value) || 0;
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

function initEventCalculations() {
    $('#guestCount, #pricePerGuest').on('input keyup', updateAllCosts);
}

function updateAllCosts() {
    const guests = parseFloat($('#guestCount').val()) || 0;
    let menuFoodCost = 0;       // Menu items cost
    let eventItemsCost = 0;     // Event-only items: cost * qty
    let eventItemsRevenue = 0;  // Event-only items: sellingPrice * qty

    $('.event-item-row').each(function () {
        const item = allActiveItems[$(this).data('index')];
        if (!item) return;

        const lineCost = item.qty * (item.cost || 0);
        item.totalCost = lineCost;
        $(this).find('.row-total').text(`$${lineCost.toFixed(2)}`);

        if (item.source === 'Menu') {
            menuFoodCost += lineCost;
        } else if (item.source === 'EventOnly') {
            eventItemsCost += lineCost;
            eventItemsRevenue += item.qty * (item.sellingPrice ?? 0);
        }
    });

    const pricePerGuest = parseFloat($('#pricePerGuest').val()) || 0;
    const totalRevenue = guests * pricePerGuest + eventItemsRevenue;
    const totalCost = menuFoodCost + eventItemsCost;
    const margin = totalRevenue - totalCost;

    const costPerGuest = guests > 0 ? totalCost / guests : 0;
    const foodCostPct = totalRevenue > 0 ? (totalCost / totalRevenue) * 100 : 0;
    const marginPct = totalRevenue > 0 ? (margin / totalRevenue) * 100 : 0;

    $('#totalRevenue').text(`$${totalRevenue.toFixed(2)}`);
    $('#totalFoodCost').text(`$${totalCost.toFixed(2)}`);
    $('#costPerGuest').text(`$${costPerGuest.toFixed(2)}`);
    $('#foodCostPercent').text(`${foodCostPct.toFixed(2)}%`).css('color', foodCostPct > 35 ? 'red' : 'green');
    $('#grossMargin').text(`$${margin.toFixed(2)}`);
    $('#grossMarginPercent').text(`${marginPct.toFixed(2)}%`);
}

// ── Modals & tabs ──

function openNewEventModal() {
    allActiveItems = [];
    $('#addEventForm')[0].reset();
    $('#eventId').val('');
    $('#noteText').val('');
    $('#menuItemSelect, #eventOnlyItemSelect').val(null).trigger('change.select2');
    renderRows();
    resetTabs();
    openModal('eventModal');
}

function openCreateCustomItemModal() {
    $('#newCustomName, #newCustomCost, #newCustomSellingPrice').val('');
    openModal('customItemModal');
}

function openTab(evt, tabId) {
    $('.tab-content-area').removeClass('active').hide();
    $('.tab-btn').removeClass('active');
    $('#' + tabId).addClass('active').show();
    $(evt.currentTarget).addClass('active');
}

function resetTabs() {
    $('.tab-content-area').hide().removeClass('active');
    $('#tab-menu-items').show().addClass('active');
    $('.tab-btn').removeClass('active').first().addClass('active');
}

// ── PDF export ──

function exportSingleEventPDF(eventId) {
    if ($('#customerFacing').is(':checked')) {
        $('#exportEventId').val(eventId);
        $('#quoteNumber').val(`INV-${padNumber(eventId, 5)}`);
        $('#quoteDate').val(new Date().toISOString().split('T')[0]);
        $('#quoteValidUntil').val(addDays(new Date(), 14));
        $('#quoteTerms').val('50% deposit required. Balance due on event day.');
        $('#quoteNotes').val('');
        openModal('customerDetailsModal');
    } else {
        const singleEvent = allEventsData.filter(e => e.eventId === eventId);
        if (singleEvent.length) exportEventsPDF(false, singleEvent);
    }
}

function processCustomerQuote() {
    const eventId = parseInt($('#exportEventId').val());
    const details = {
        quoteNumber: $('#quoteNumber').val().trim(),
        quoteDate: $('#quoteDate').val(),
        validUntil: $('#quoteValidUntil').val(),
        terms: $('#quoteTerms').val().trim()
    };

    closeModal('customerDetailsModal');
    generateCustomSinglePDF(eventId, details);
}

async function generateCustomSinglePDF(eventId, customDetails) {
    try {
        const res = await getAjaxPromise({ url: `/api/EventApi/GetEvent/${eventId}`, method: 'GET' });
        exportEventsPDF(true, [{ ...res.data, ...customDetails }]);
    } catch {
        showNotification('Error preparing quote data', 'error');
    }
}

function exportEventsPDF(isCustomerFacing, exportableEvents) {
    if (isCustomerFacing) exportCustomerQuotePDF(exportableEvents);
    else exportInternalReportPDF(exportableEvents);
}

async function exportInternalReportPDF(exportableEvents) {
    let logo = null;
    try { logo = await getImageAsPngBase64(profilePic); } catch { }

    const columns = ['Event Details', 'Guests', 'Price/Guest', 'Total Cost', 'Total Revenue', 'Profit']
        .map(text => ({ text, style: 'tableHeader' }));

    const body = exportableEvents.map(e => [
        { text: `${e.eventName}\n${DateFormatter.formatWithComma(e.eventDate)}`, style: 'itemRow' },
        { text: e.numberOfGuests, alignment: 'center', style: 'itemRow' },
        { text: `$${e.pricePerGuest.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
        { text: `$${e.totalFoodCost.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
        { text: `$${e.totalRevenue.toFixed(2)}`, alignment: 'right', style: 'itemRow' },
        { text: `$${(e.totalRevenue - e.totalFoodCost).toFixed(2)}`, alignment: 'right', style: 'itemRow' }
    ]);

    const totalRevenue = exportableEvents.reduce((sum, e) => sum + e.totalRevenue, 0);
    const totalCost = exportableEvents.reduce((sum, e) => sum + e.totalFoodCost, 0);
    const totalProfit = totalRevenue - totalCost;
    const totalGuests = exportableEvents.reduce((sum, e) => sum + e.numberOfGuests, 0);
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
                            { text: `Total Events: ${exportableEvents.length}`, style: 'summaryText' },
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
}

function exportCustomerQuotePDF(exportableEvents) {
    // Notes now come with the GetEvent response (event.notes)
    Promise.all(exportableEvents.map(e =>
        getAjaxPromise({ url: `/api/EventApi/GetEvent/${e.eventId}`, method: 'GET' })
            .then(res => ({ ...e, ...res.data }))
    )).then(async events => {
        let logo = null;
        try { logo = await getImageAsPngBase64(profilePic); } catch { }

        const content = [];

        events.forEach((event, idx) => {
            if (idx > 0) content.push({ pageBreak: 'before', text: '' });

            // Company header
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

            // Bill to / event details / reference
            content.push({
                columns: [
                    {
                        width: '38%',
                        stack: [
                            { text: 'BILL TO', style: 'sectionHeader' },
                            {
                                text: [
                                    { text: 'Name: ', bold: true },
                                    { text: $("#custName").val().trim() + '\n' },
                                    { text: 'Company: ', bold: true },
                                    { text: $("#custCompany").val().trim() + '\n' },
                                    { text: 'Address: ', bold: true },
                                    { text: $("#custAddress").val().trim() + '\n' },
                                    { text: 'Phone: ', bold: true },
                                    { text: $("#custPhone").val().trim() + '\n' },
                                    { text: 'Email: ', bold: true },
                                    { text: $("#custName").val().trim() }
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

            const rows = [[
                { text: 'Guest(s)', style: 'tableRow' },
                { text: `${event.numberOfGuests}`, style: 'tableRow', alignment: 'center' },
                { text: `$${event.pricePerGuest.toFixed(2)}`, style: 'tableRow', alignment: 'right' },
                { text: `$${(event.numberOfGuests * event.pricePerGuest).toFixed(2)}`, style: 'tableRow', alignment: 'right' }
            ]];

            if (event.items?.length && event.totalFoodCost > 0) {
                event.items.forEach(i => {
                    const isMenu = !!(i.menuItemId && i.menuItemId > 0);
                    const qty = i.qty || i.quantity || 0;

                    if (isMenu) {
                        const foundMenu = menuItemsGlobal.find(x => x.id == i.menuItemId);
                        rows.push([
                            { text: i.itemName || foundMenu?.text || '', style: 'tableRow' },
                            { text: qty.toFixed(0), style: 'tableRow', alignment: 'center' },
                            { text: '', style: 'tableRow', alignment: 'right' },
                            { text: '', style: 'tableRow', alignment: 'right' }
                        ]);
                        return;
                    }

                    const found = eventOnlyItemsGlobal.find(x => x.id == i.eventOnlyItemId);
                    const sellingPrice = i.sellingPrice || found?.sellingPrice || 0;
                    const unitPrice = sellingPrice > 0 ? sellingPrice : (i.unitCost || found?.cost || 0);

                    rows.push([
                        { text: i.itemName || found?.text || '', style: 'tableRow' },
                        { text: qty.toFixed(0), style: 'tableRow', alignment: 'center' },
                        { text: `$${unitPrice.toFixed(2)}`, style: 'tableRow', alignment: 'right' },
                        { text: `$${(qty * unitPrice).toFixed(2)}`, style: 'tableRow', alignment: 'right' }
                    ]);
                });
            } else {
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

            // Totals
            content.push({
                columns: [
                    { width: '*', text: '' },
                    {
                        width: 200,
                        table: {
                            widths: ['*', 80],
                            body: [
                                [
                                    { text: 'Subtotal:', bold: true, alignment: 'right', fontSize: 10 },
                                    { text: `$${event.totalRevenue.toFixed(2)}`, alignment: 'right', fontSize: 10 }
                                ],
                                [
                                    { text: 'Total:', bold: true, fontSize: 12, alignment: 'right', color: '#f08d11' },
                                    { text: `$${event.totalRevenue.toFixed(2)}`, bold: true, fontSize: 12, alignment: 'right', color: '#f08d11' }
                                ]
                            ]
                        },
                        layout: 'noBorders'
                    }
                ],
                margin: [0, 0, 0, 20]
            });

            // Terms, signature & note
            content.push({
                columns: [
                    {
                        width: '55%',
                        stack: [
                            { text: 'Terms & Conditions', style: 'footerHeader' },
                            { text: event.terms || event.paymentTerms || '', style: 'footerText', margin: [0, 0, 0, 25] },
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
                            { text: 'Note', style: 'footerHeader', alignment: 'right' },
                            { text: event.notes || '', style: 'footerText', alignment: 'right' }
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

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

async function getImageAsPngBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = "anonymous";
        img.onload = () => {
            const targetWidth = 350;
            const targetHeight = Math.round(targetWidth * (img.naturalHeight / img.naturalWidth));

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