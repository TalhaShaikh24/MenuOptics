// policies.js — Policy editor page scripts.

let allPolicies = {};
let editorInstance = null;

$(document).ready(function () {
    initCKEditor();

    $('.tab-btn').on('click', function () {
        const newType = $(this).data('type');
        const currentType = $('#currentPolicyType').val();

        // Warn before discarding unsaved edits in the current tab.
        if (editorInstance && currentType) {
            const currentContent = editorInstance.getData();
            const savedContent = allPolicies[currentType] || '';

            if (currentContent !== savedContent) {
                $.showCustomPopup({
                    title: 'Unsaved Changes',
                    iconClass: 'fas fa-exclamation-triangle',
                    message: `<p>Switching tabs will discard them.</p>`,
                    showButtons: true,
                    okText: 'Yes, Switch',
                    cancelText: 'Cancel',
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

        switchTab(newType, this);
    });

    $('#savePolicyBtn').on('click', function () {
        if (!editorInstance) {
            showNotification('Editor not initialized.', 'error');
            return;
        }

        const type = 'Terms';
        const content = editorInstance.getData();

        if (!content || content.trim() === '' || content.trim() === '<p>&nbsp;</p>') {
            showNotification('Policy content cannot be empty.', 'error');
        } else {
            savePolicy(type, content);
        }
    });
});

function loadAllPolicies() {
    ajaxRequest({
        url: '/api/PolicyApi/GetAllPolicies',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            if (response && response.data && Array.isArray(response.data)) {
                allPolicies = {};

                response.data.forEach(policy => {
                    if (policy && policy.policyType) {
                        allPolicies[policy.policyType] = policy.content || '';
                    }
                });

                const requiredTypes = ['Privacy', 'Terms', 'DataExport'];
                requiredTypes.forEach(type => {
                    if (!allPolicies.hasOwnProperty(type)) {
                        allPolicies[type] = '';
                    }
                });

                const initialContent = allPolicies['Terms'] || '';
                if (editorInstance) {
                    editorInstance.setData(initialContent);
                }

                $('.tab-btn').removeClass('active');
                $('.tab-btn[data-type="Privacy"]').addClass('active');
            }
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch policies:', error);
            showNotification('Failed to load policy data.', 'error');

            allPolicies = { 'Privacy': '', 'Terms': '', 'DataExport': '' };
            if (editorInstance) {
                editorInstance.setData('');
            }
        }
    });
}

function savePolicy(type, content) {
    // Update the local copy first so the unsaved-changes check stays in sync.
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

    if (editorInstance) {
        editorInstance.on('instanceReady', function () {
            loadAllPolicies();
        });
    }
}

function switchTab(type, element) {
    $('.tab-btn').removeClass('active');
    $(element).addClass('active');

    $('#currentPolicyType').val(type);

    const content = allPolicies[type] || '';
    if (editorInstance) {
        editorInstance.setData(content);
    }
}
