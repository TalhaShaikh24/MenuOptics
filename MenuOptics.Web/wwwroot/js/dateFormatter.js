/* ============================================================================
 * dateFormatter.js — Shared date formatting utility used by every page.
 * Exposes the global DateFormatter object. Do not change output formats.
 * ==========================================================================*/

const DateFormatter = {
    /**
     * Format date to 'YYYY-MM-DD' (ISO short)
     */
    formatISO: function (date) {
        if (!date) return null;  // Check for null, undefined or empty string
        return new Date(date).toISOString().split('T')[0];
    },

    /**
     * Format date to '26, Jan 2025'
     */
    formatWithComma: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const day = d.getDate().toString().padStart(2, '0');
        const month = d.toLocaleString('en-GB', { month: 'short' });
        const year = d.getFullYear();
        return `${day}, ${month} ${year}`;
    },

    /**
     * Format date to 'Jan 26, 2025'
     */
    formatUS: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const options = { month: 'short', day: '2-digit', year: 'numeric' };
        return d.toLocaleDateString('en-US', options);
    },

    /**
     * Format date to 'Jan 26, 2025, 03:45 PM'
     */
    formatUSWithTime: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const options = {
            month: 'short',
            day: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true // for AM/PM format
        };
        return d.toLocaleString('en-US', options);
    },

    /**
     * Format date to 'DD/MM/YYYY'
     */
    formatEU: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        return `${day}/${month}/${year}`;
    },

    /**
     * Format date to 'MM-DD-YYYY'
     */
    formatDashUS: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        const year = d.getFullYear();
        return `${month}-${day}-${year}`;
    },

    /**
     * Format date to 'January 26, 2025'
     */
    formatFullMonth: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const options = { month: 'long', day: '2-digit', year: 'numeric' };
        return d.toLocaleDateString('en-US', options);
    },




    /**
     * Format time to 'HH:mm' (24-hour)
     * Example: 18:45
     */
    format24: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const hours = String(d.getHours()).padStart(2, '0');
        const minutes = String(d.getMinutes()).padStart(2, '0');
        return `${hours}:${minutes}`;
    },

    /**
     * Format time to 'HH:mm:ss' (24-hour with seconds)
     * Example: 18:45:30
     */
    format24WithSeconds: function (date) {
        if (!date) return null;
        const d = new Date(date);
        const h = String(d.getHours()).padStart(2, '0');
        const m = String(d.getMinutes()).padStart(2, '0');
        const s = String(d.getSeconds()).padStart(2, '0');
        return `${h}:${m}:${s}`;
    },

    /**
     * Format time to 'hh:mm AM/PM'
     * Example: 06:45 PM
     */
    format12: function (date) {
        if (!date) return null;
        const d = new Date(date);
        return d.toLocaleString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
    },

    /**
     * Format time to 'hh:mm:ss AM/PM'
     * Example: 06:45:30 PM
     */
    format12WithSeconds: function (date) {
        if (!date) return null;
        const d = new Date(date);
        return d.toLocaleString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
        });
    },

    /**
     * Format time to ISO time 'HH:mm:ss'
     */
    formatISOTime: function (date) {
        if (!date) return null;
        return new Date(date).toISOString().split('T')[1].split('.')[0];
    }
};
