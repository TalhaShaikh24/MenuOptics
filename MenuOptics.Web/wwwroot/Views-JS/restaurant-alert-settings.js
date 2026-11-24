// restaurant-alert-settings.js — Price Alert Settings via User CRUD

$(document).ready(function () {
    loadPriceAlertSettings();

    // Toggle switch change
    $('#isPriceAlertOn').on('change', function () {
        updateToggleState($(this).is(':checked'));
    });

    // Preset buttons click
    $(document).on('click', '.btn-preset', function () {
        const pct = parseFloat($(this).data('pct'));
        if (!isNaN(pct)) {
            $('#thresholdInput').val(pct.toFixed(1));
            syncPresetButtons(pct);
        }
    });

    // Input change
    $('#thresholdInput').on('input change', function () {
        const val = parseFloat($(this).val()) || 0;
        syncPresetButtons(val);
    });

    // Save Settings
    $('#btnSaveAlertSettings').on('click', function () {
        const $btn = $(this);
        const threshold = parseFloat($('#thresholdInput').val());
        const isPriceAlertOn = $('#isPriceAlertOn').is(':checked');

        if (isNaN(threshold) || threshold <= 0) {
            showNotification('Please enter a valid percentage greater than 0.', 'error');
            return;
        }

        setBtnLoading($btn, true);
        ajaxRequest({
            url: '/api/UserApi/UpdatePriceAlert',
            method: 'PUT',
            data: JSON.stringify({
                priceAlertThreshold: threshold,
                isPriceAlertOn: isPriceAlertOn
            }),
            contentType: 'application/json',
            onSuccess: function (response) {
                showNotification(response.responseMsg || 'Price alert settings saved successfully!', 'success');
            },
            onError: function (xhr) {
                const msg = xhr.responseJSON?.responseMsg || 'Failed to save price alert settings.';
                showNotification(msg, 'error');
            },
            onComplete: function () {
                setBtnLoading($btn, false);
            }
        });
    });
});

// Load settings using GetCurrentUser
function loadPriceAlertSettings() {
    ajaxRequest({
        url: '/api/UserApi/GetCurrentUser',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const user = response.data;
            if (!user) return;

            const threshold = (user.priceAlertThreshold && user.priceAlertThreshold > 0)
                ? user.priceAlertThreshold
                : 10.0;

            const isAlertOn = (user.isPriceAlertOn !== null && user.isPriceAlertOn !== undefined)
                ? user.isPriceAlertOn
                : true;

            $('#thresholdInput').val(parseFloat(threshold).toFixed(1));
            $('#isPriceAlertOn').prop('checked', isAlertOn);
            updateToggleState(isAlertOn);
            syncPresetButtons(threshold);
        },
        onError: function () {
            syncPresetButtons(10.0);
            updateToggleState(true);
        }
    });
}

function updateToggleState(isOn) {
    if (isOn) {
        $('#alertStatusBadge').removeClass('inactive').addClass('active');
        $('#alertStatusText').text('Enabled');
        $('#thresholdConfigSection').css({ 'opacity': '1', 'pointer-events': 'auto' });
        $('#cronBanner').show();
    } else {
        $('#alertStatusBadge').removeClass('active').addClass('inactive');
        $('#alertStatusText').text('Disabled');
        $('#thresholdConfigSection').css({ 'opacity': '0.5', 'pointer-events': 'none' });
        $('#cronBanner').hide();
    }
}

function syncPresetButtons(val) {
    $('.btn-preset').each(function () {
        const btnPct = parseFloat($(this).data('pct'));
        if (Math.abs(btnPct - val) < 0.05) {
            $(this).addClass('active').css({ 'border-color': '#f08d11', 'color': '#f08d11', 'font-weight': '700' });
        } else {
            $(this).removeClass('active').css({ 'border-color': '', 'color': '', 'font-weight': '' });
        }
    });
}
