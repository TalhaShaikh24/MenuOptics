/* ============================================================================
 * globalAjax.js — Shared AJAX wrapper used by every page.
 * Exposes: ajaxRequest(options), executeAjaxRequest(options),
 *          showConfirmDeleteModal(message, onConfirm)
 * DELETE requests go through a confirmation modal; 500 errors trigger the
 * global error popup. Do not change the request/response contract.
 * ==========================================================================*/

// Setup global AJAX headers
(function setupAjaxDefaults() {
    $.ajaxSetup({
        xhrFields: {
            withCredentials: true
        }
    });
})();

// Global AJAX function
function ajaxRequest({
    url,
    method = 'GET',
    data = {},
    headers = {},
    contentType = 'application/json',
    processData = true,
    responseType = 'json',
    isQuery = false,
    onSuccess = () => { },
    onError = () => { },
    onComplete = () => { },
    showMessage = true,
    confirmMessage = 'Are you sure you want to delete this item?'
}) {
    const upperMethod = method.toUpperCase();

    // If DELETE, show confirmation modal
    if (upperMethod === 'DELETE') {
        showConfirmDeleteModal(confirmMessage, () => {
            // Proceed with the AJAX request after confirmation
            executeAjaxRequest({
                url,
                method,
                data,
                headers,
                contentType,
                processData,
                responseType,
                isQuery,
                onSuccess,
                onError,
                onComplete,
                showMessage
            });
        });
        return; // Exit to wait for confirmation
    }

    // For non-DELETE requests, execute immediately
    executeAjaxRequest({
        url,
        method,
        data,
        headers,
        contentType,
        processData,
        responseType,
        isQuery,
        onSuccess,
        onError,
        onComplete,
        showMessage
    });
}

// Extracted AJAX execution logic to avoid duplication
function executeAjaxRequest({
    url,
    method,
    data,
    headers,
    contentType,
    processData,
    responseType,
    isQuery,
    onSuccess,
    onError,
    onComplete,
    showMessage
}) {
    const upperMethod = method.toUpperCase();
    
    // Append query parameters if isQuery is true AND method is GET or DELETE
    if (isQuery || data && (upperMethod === 'GET' || upperMethod === 'DELETE')) {
        const queryString = $.param(data);
        if (queryString) {
            url += (url.includes('?') ? '&' : '?') + queryString;
        }
        data = null; // Prevent sending data in body
    }

    // Detect and handle FormData
    let finalData = data;
    if (data instanceof FormData) {
        contentType = false;
        processData = false;
    } else if (contentType === 'application/json' && data && typeof data === 'object') {
        finalData = JSON.stringify(data);
    }

    $.ajax({
        url,
        type: method,
        data: finalData,
        headers,
        contentType,
        processData,
        dataType: responseType,
        success: function (response, textStatus, xhr) {
            const statusCode = xhr.status;
            const successMsg = response?.responseMsg || "Operation completed successfully.";

            if (showMessage) {
                showNotification(successMsg, "success", statusCode);
            }

            onSuccess(response);
        },
        error: function (xhr, status, error) {
            const statusCode = xhr.status;
            let errorMsg = "An unexpected error occurred";

            try {
                if (xhr.responseJSON) {
                    errorMsg = xhr.responseJSON.responseMsg || xhr.responseJSON.error || JSON.stringify(xhr.responseJSON);
                } else if (xhr.responseText) {
                    errorMsg = xhr.responseText;
                } else if (error) {
                    errorMsg = error;
                }
            } catch {
                errorMsg = "Error parsing error response";
            }

            // ── 401: Session / Token expired — auto logout ─────────────────
            if (statusCode === 401) {
                // Guard against multiple concurrent 401s triggering duplicate redirects
                if (window._sessionExpiredHandled) return;
                window._sessionExpiredHandled = true;

                showNotification('Your session has expired. Please log in again.', 'error');

                setTimeout(function () {
                    window.location.href = '/User/login';
                }, 2500);

                onError(xhr, status, error);
                return;
            }

            if (statusCode === 402) {
                var $stripe = $('#subscriptioStatus');
                $stripe.text(errorMsg).show();
                onError(xhr, status, error);
                return; // skip showNotification for 402
            }

            // Trigger global popup if status is 500
            if (statusCode === 500) {
                $.showCustomPopup({
                    title: "Server Error (500)",
                    iconClass: "fas fa-exclamation-triangle",
                    message: `
                    <p>Something went wrong on the server.</p>
                    <p>Please try again later or contact support at 
                        <a href="javascript:void(0);" class="support-link open-contact-modal" data-reason="Support">
                            support@menuoptics.com
                        </a>
                    </p>`
                });
            }
            
            if (statusCode !== 500) { 
                showNotification(errorMsg, "error", statusCode);
            }
            onError(xhr, status, error);
        },
        complete: function () {
            onComplete();
        }
    });
}

// Show confirmation modal with custom message and confirm callback
function showConfirmDeleteModal(message, onConfirm) {
    $('#confirmDeleteMessage').text(message || 'Are you sure you want to delete this item?');
    $('#confirmDeleteBtn').off('click').on('click', () => {
        onConfirm();
        closeModal('confirmDeleteModal');
    });
    openModal('confirmDeleteModal');
}