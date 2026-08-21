$(document).ready(function () {
    loadSubscriptionDetails();

    $('#btnRefreshSubscription').on('click', function () {
        loadSubscriptionDetails();
    });

    $('#btnCancelSubscription').on('click', function () {
        openModal('cancelSubscriptionModal');
    });

    $('#btnConfirmCancelSub').on('click', function () {
        cancelSubscription();
    });

    $('#btnRenewSubscription').on('click', function () {
        renewSubscription();
    });

    $('#btnRetryPaymentAlert').on('click', function () {
        retryPayment();
    });
});

let currentSubscriptionData = null;

function loadSubscriptionDetails() {
    $.ajax({
        url: '/api/SubscriptionApi/GetDetails',
        type: 'GET',
        contentType: 'application/json',
        success: function (response) {
            if (response && response.status === 200 && response.data) {
                currentSubscriptionData = response.data;
                renderSubscription(response.data);
            } else {
                showNotification(response?.responseMsg || "Failed to load subscription details.", "error");
            }
        },
        error: function (xhr) {
            const errorMsg = xhr.responseJSON?.responseMsg || "Unable to load subscription details.";
            showNotification(errorMsg, "error");
        }
    });
}

function renderSubscription(data) {
    const status = (data.subscriptionStatus || 'Pending').toLowerCase();
    const isActive = data.isActiveSubscription;

    // Stat 1: Status Text & Icon
    const $statusText = $('#statStatusText');
    const $statusIcon = $('#statStatusIcon');
    $statusIcon.removeClass('active inactive expiring pending');

    if (status === 'active' || status === 'trialing') {
        $statusText.text(status === 'trialing' ? 'Trialing' : 'Active').css('color', '#059669');
        $statusIcon.addClass('active').html('<i class="fas fa-check-circle"></i>');
        $('#statStatusSub').text('All features unlocked');
        $('#paymentAlertCard').hide();
        $('#btnCancelSubscription').show();
        $('#btnRenewSubscription').hide();
    } else if (status === 'pastdue' || status === 'unpaid') {
        $statusText.text('Past Due').css('color', '#d97706');
        $statusIcon.addClass('expiring').html('<i class="fas fa-exclamation-circle"></i>');
        $('#statStatusSub').text('Payment past due');
        showAlertBanner('Payment Past Due', 'Your recent subscription payment could not be processed. Please retry payment to avoid service interruption.');
        $('#btnCancelSubscription').show();
        $('#btnRenewSubscription').show().html('<i class="fas fa-credit-card"></i> Pay Now / Update Payment');
    } else if (status === 'failed' || status === 'expired' || status === 'incomplete_expired') {
        $statusText.text('Expired').css('color', '#dc2626');
        $statusIcon.addClass('inactive').html('<i class="fas fa-times-circle"></i>');
        $('#statStatusSub').text('Subscription inactive');
        showAlertBanner('Subscription Inactive', 'Your subscription has expired or payment failed. Please reactivate now to regain access.');
        $('#btnCancelSubscription').hide();
        $('#btnRenewSubscription').show().html('<i class="fas fa-redo"></i> Reactivate Subscription');
    } else if (status === 'canceled') {
        $statusText.text('Canceled').css('color', '#6b7280');
        $statusIcon.addClass('inactive').html('<i class="fas fa-ban"></i>');
        $('#statStatusSub').text('Subscription canceled');
        showAlertBanner('Subscription Canceled', 'Your subscription is currently canceled. Click reactivate to restore full access.');
        $('#btnCancelSubscription').hide();
        $('#btnRenewSubscription').show().html('<i class="fas fa-redo"></i> Reactivate Subscription');
    } else {
        $statusText.text('Pending').css('color', '#d97706');
        $statusIcon.addClass('pending').html('<i class="fas fa-clock"></i>');
        $('#statStatusSub').text('Pending payment');
        showAlertBanner('Pending Activation', 'Please complete payment to activate your restaurant subscription.');
        $('#btnCancelSubscription').hide();
        $('#btnRenewSubscription').show().html('<i class="fas fa-credit-card"></i> Complete Payment');
    }

    // Stat 2: Plan
    $('#statPlanText').text('Yearly Plan');

    // Stat 3: Renewal Date
    if (data.currentPeriodEnd) {
        const endDt = new Date(data.currentPeriodEnd);
        $('#statRenewalText').text(endDt.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }));
    } else {
        $('#statRenewalText').text('N/A');
    }

    // Stat 4: Amount
    if (data.amount != null) {
        const currency = data.currency || 'USD';
        $('#statAmountText').text(`$${parseFloat(data.amount).toFixed(2)} ${currency}`);
        $('#pkgPriceDisplay').html(`$${parseFloat(data.amount).toFixed(2)} <span>/${(data.interval || 'year')}</span>`);
    } else {
        $('#statAmountText').text('$50 / mo');
        $('#pkgPriceDisplay').html('$50 <span>/month</span>');
    }

    // Right Box Account Details
    $('#infoRestaurantName').text(data.restaurantName || '--');
    $('#infoEmail').text(data.email || '--');
    const interval = data.interval ? (data.interval.charAt(0).toUpperCase() + data.interval.slice(1)) : 'Annual (Yearly)';
    $('#infoBillingCycle').text(interval);

    if (data.paymentMethodBrand && data.paymentMethodLast4) {
        $('#infoPaymentMethod').text(`${data.paymentMethodBrand.toUpperCase()} •••• ${data.paymentMethodLast4}`);
    } else {
        $('#infoPaymentMethod').text('Card on file');
    }

    $('#infoSubId').text(data.stripeSubscriptionId || 'N/A');
    $('#infoCustId').text(data.stripeCustomerId || 'N/A');
}

function showAlertBanner(title, message) {
    const $card = $('#paymentAlertCard');
    $('#alertTitle').html(`<i class="fas fa-exclamation-triangle"></i> ${title}`);
    $('#alertMessage').text(message);
    $card.show();
}

function renewSubscription() {
    const btn = $('#btnRenewSubscription');
    const originalHtml = btn.html();
    btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Preparing Checkout...');

    $.ajax({
        url: '/api/SubscriptionApi/Renew',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ plan: 'professional' }),
        success: function (response) {
            if (response && response.status === 200 && response.data) {
                if (response.data.url) {
                    window.location.href = response.data.url;
                } else if (response.data.sessionId) {
                    window.location.href = `https://checkout.stripe.com/c/pay/${response.data.sessionId}`;
                }
            } else {
                btn.prop('disabled', false).html(originalHtml);
                showNotification(response?.responseMsg || "Failed to initiate checkout session.", "error");
            }
        },
        error: function (xhr) {
            btn.prop('disabled', false).html(originalHtml);
            const errorMsg = xhr.responseJSON?.responseMsg || "Error creating checkout session.";
            showNotification(errorMsg, "error");
        }
    });
}

function retryPayment() {
    const btn = $('#btnRetryPaymentAlert');
    btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    $.ajax({
        url: '/api/SubscriptionApi/RetryPayment',
        type: 'POST',
        contentType: 'application/json',
        success: function (response) {
            if (response && response.status === 200 && response.data && response.data.url) {
                window.location.href = response.data.url;
            } else {
                btn.prop('disabled', false).html('<i class="fas fa-credit-card"></i> Pay Now / Reactivate');
                showNotification(response?.responseMsg || "Failed to initialize payment retry.", "error");
            }
        },
        error: function (xhr) {
            btn.prop('disabled', false).html('<i class="fas fa-credit-card"></i> Pay Now / Reactivate');
            const errorMsg = xhr.responseJSON?.responseMsg || "Error initializing payment retry.";
            showNotification(errorMsg, "error");
        }
    });
}

function cancelSubscription() {
    const $btn = $('#btnConfirmCancelSub');
    $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Canceling...');

    $.ajax({
        url: '/api/SubscriptionApi/Cancel',
        type: 'POST',
        contentType: 'application/json',
        success: function (response) {
            $btn.prop('disabled', false).html('<i class="fas fa-ban"></i> Confirm Cancellation');
            closeModal('cancelSubscriptionModal');

            if (response && response.status === 200) {
                showNotification("Subscription has been canceled successfully.", "success");
                loadSubscriptionDetails();
            } else {
                showNotification(response?.responseMsg || "Failed to cancel subscription.", "error");
            }
        },
        error: function (xhr) {
            $btn.prop('disabled', false).html('<i class="fas fa-ban"></i> Confirm Cancellation');
            closeModal('cancelSubscriptionModal');
            const errorMsg = xhr.responseJSON?.responseMsg || "Error canceling subscription.";
            showNotification(errorMsg, "error");
        }
    });
}
