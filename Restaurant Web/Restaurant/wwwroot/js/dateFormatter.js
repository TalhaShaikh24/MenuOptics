// dateFormatter.js

const DateFormatter = {
    /**
     * Format date to 'YYYY-MM-DD' (ISO short)
     */
    formatISO: function (date) {
        return new Date(date).toISOString().split('T')[0];
    },

    /**
     * Format date to '26, Jan 2025'
     */
    formatWithComma: function (date) {
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
        const d = new Date(date);
        const options = { month: 'short', day: '2-digit', year: 'numeric' };
        return d.toLocaleDateString('en-US', options);
    },

    /**
     * Format date to 'Jan 26, 2025, 03:45 PM'
     */
  
    formatUSWithTime: function (date) {
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
        const d = new Date(date);
        const options = { month: 'long', day: '2-digit', year: 'numeric' };
        return d.toLocaleDateString('en-US', options);
    }
};
