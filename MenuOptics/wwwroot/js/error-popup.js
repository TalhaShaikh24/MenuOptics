/* ============================================================================
 * error-popup.js — Shared popup plugin used for errors and confirmations.
 * Exposes: $.showCustomPopup(options) — supports optional OK/Cancel buttons
 * with onConfirm/onCancel callbacks.
 * ==========================================================================*/

(function ($) {
    $.showCustomPopup = function (options) {
        // Default settings
        var settings = $.extend({
            title: "Something Went Wrong",
            iconClass: "fas fa-redo-alt",
            message: `
                <p>We're currently experiencing an issue.</p>
            `,
            overlayId: "popup-overlay",

            // NEW OPTIONS
            showButtons: false,
            okText: "OK",
            cancelText: "Cancel",
            onConfirm: null,
            onCancel: null
        }, options);

        // Remove existing popup
        $('#' + settings.overlayId).remove();

        // Buttons HTML (only if enabled)
        var buttonsHTML = settings.showButtons ? `
            <div class="popup-actions">
                <button class="btn btn-secondary" id="popup-cancel">
                    ${settings.cancelText}
                </button>
                <button class="btn btn-primary" id="popup-ok">
                    ${settings.okText}
                </button>
            </div>
        ` : '';

        // Popup HTML
        var popupHTML = `
        <div id="${settings.overlayId}" class="popup-overlay show">
            <div class="error-modal">
                <button class="close-btn" id="close-popup">
                    <i class="fas fa-times"></i>
                </button>

                <div class="error-modal-content">
                    <div class="icon-container">
                        <div class="icon-circle">
                            <i class="${settings.iconClass}"></i>
                        </div>
                    </div>

                    <h2 class="error-modal-title">${settings.title}</h2>

                    <div class="error-modal-description">
                        ${settings.message}
                    </div>

                    ${buttonsHTML}
                </div>
            </div>
        </div>`;

        // Append
        $('body').append(popupHTML);
        $('body').css('overflow', 'hidden');

        // Events
        $('#close-popup').on('click', handleCancel);
        $('#popup-cancel').on('click', handleCancel);
        $('#popup-ok').on('click', handleConfirm);

        $('#' + settings.overlayId).on('click', function (e) {
            if (e.target === this) handleCancel();
        });

        $(document).on('keydown.popup', function (e) {
            if (e.key === 'Escape') handleCancel();
        });

        function handleConfirm() {
            closePopup();
            if (typeof settings.onConfirm === "function") {
                settings.onConfirm();
            }
        }

        function handleCancel() {
            closePopup();
            if (typeof settings.onCancel === "function") {
                settings.onCancel();
            }
        }

        function closePopup() {
            $('#' + settings.overlayId).remove();
            $('body').css('overflow', 'auto');
            $(document).off('keydown.popup');
        }
    };
})(jQuery);
