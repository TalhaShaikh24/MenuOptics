/* ============================================================================
 * policies.js — Policy editor page (Views/Policies/Index.cshtml)
 * CKEditor-based editing of Privacy / Terms / DataExport policies.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let allPolicies = {};
let editorInstance = null;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize CKEditor on page load
    initCKEditor();

    // Event handler for Tab Switching
    $('.tab-btn').on('click', function () {
        // 1. Get the type of the tab we want to switch TO
        const newType = $(this).data('type');

        // 2. Get the type of the tab we are currently ON (leaving)
        const currentType = $('#currentPolicyType').val();

        // 3. Check if we have unsaved changes in the current tab
        if (editorInstance && currentType) {
            const currentContent = editorInstance.getData();
            // Get the original saved content from memory, default to empty string if undefined
            const savedContent = allPolicies[currentType] || '';

            // Compare: If content is different, ask for confirmation
            if (currentContent !== savedContent) {
                $.showCustomPopup({
                    title: "Unsaved Changes",
                    iconClass: "fas fa-exclamation-triangle",
                    message: `<p>Switching tabs will discard them.</p>`,
                    showButtons: true,
                    okText: "Yes, Switch",
                    cancelText: "Cancel",

                    onConfirm: function () {
                        switchTab();
                    },
                    onCancel: function () {
                        return;
                    }
                });

                return;
            }


        }

        // 4. Perform the switch
        switchTab(newType, this);
    });

    // Event handler for Save Button
    $('#savePolicyBtn').on('click', function () {
        if (!editorInstance) {
            showNotification('Editor not initialized.', 'error');
            return;
        }

        const type = "Terms";
        const content = editorInstance.getData();

        // Validation
        if (!content || content.trim() === '' || content.trim() === '<p>&nbsp;</p>') {
            showNotification('Policy content cannot be empty.', 'error');
        } else {
            savePolicy(type, content);
        }
    });
});

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Function to initialize CKEditor
function initCKEditor() {
    CKEDITOR.replace('policyEditor', {
        height: 450,
        toolbar: [
            { name: 'document', items: ['Source', '-', 'Preview'] },
            { name: 'clipboard', items: ['Cut', 'Copy', 'Paste', 'PasteText', 'PasteFromWord', '-', 'Undo', 'Redo'] },
            { name: 'editing', items: ['Find', 'Replace', '-', 'SelectAll'] },
            '/',
            { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', 'Subscript', 'Superscript', '-', 'RemoveFormat'] },
            { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote'] },
            { name: 'links', items: ['Link', 'Unlink'] },
            { name: 'insert', items: ['Image', 'Table', 'HorizontalRule', 'SpecialChar'] },
            '/',
            { name: 'styles', items: ['Styles', 'Format', 'Font', 'FontSize'] },
            { name: 'colors', items: ['TextColor', 'BGColor'] },
            { name: 'align', items: ['JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock'] },
            { name: 'tools', items: ['Maximize'] }
        ],
        removePlugins: 'elementspath',
        resize_enabled: false
    });

    editorInstance = CKEDITOR.instances.policyEditor;

    // Load policies only after instance is ready
    if (editorInstance) {
        editorInstance.on('instanceReady', function () {
            loadAllPolicies();
        });
    }
}

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Function to load all policies
function loadAllPolicies() {
    ajaxRequest({
        url: '/api/PolicyApi/GetAllPolicies',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            if (response && response.data && Array.isArray(response.data)) {
                // Reset object
                allPolicies = {};

                // Map response to local object
                response.data.forEach(policy => {
                    if (policy && policy.policyType) {
                        allPolicies[policy.policyType] = policy.content || '';
                    }
                });

                // Ensure required types exist
                const requiredTypes = ['Privacy', 'Terms', 'DataExport'];
                requiredTypes.forEach(type => {
                    if (!allPolicies.hasOwnProperty(type)) {
                        allPolicies[type] = '';
                    }
                });

                // Set initial content (Privacy)
                const initialContent = allPolicies['Privacy'] || '';
                if (editorInstance) {
                    editorInstance.setData(initialContent);
                }

                // Set UI state
                $('.tab-btn').removeClass('active');
                $('.tab-btn[data-type="Privacy"]').addClass('active');
            }
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch policies:', error);
            showNotification('Failed to load policy data.', 'error');

            // Fallback initialization
            allPolicies = { 'Privacy': '', 'Terms': '', 'DataExport': '' };
            if (editorInstance) {
                editorInstance.setData('');
            }
        }
    });
}

// Function to save a policy
function savePolicy(type, content) {
    // Update local memory first
    allPolicies[type] = content;

    ajaxRequest({
        url: '/api/PolicyApi/SavePolicy',
        method: 'POST',
        data: {
            PolicyType: type,
            Content: content
        },
        onSuccess: function (response) {
            showNotification(response.responseMsg || 'Policy saved successfully.', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to save policy:', error);
            showNotification('Failed to save policy.', 'error');
        }
    });
}

// Function to handle tab switching logic
function switchTab(type, element) {
    // UI Updates
    $('.tab-btn').removeClass('active');
    $(element).addClass('active');

    // Update hidden input
    $('#currentPolicyType').val(type);

    // Load content from memory into editor
    const content = allPolicies[type] || '';
    if (editorInstance) {
        editorInstance.setData(content);
    }
}