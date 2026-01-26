// Chat interop functions for Blazor
window.chatInterop = {
    resizeTextarea: function (selector) {
        const ta = document.querySelector(selector);
        if (ta) {
            const lineBreaks = (ta.value.match(/\n/g) || []).length;
            if (lineBreaks > 0 || ta.value.length > 80) {
                ta.style.height = 'auto';
                ta.style.height = Math.min(ta.scrollHeight, 120) + 'px';
            } else {
                ta.style.height = '';
            }
        }
    },

    resetTextareaHeight: function (selector) {
        const ta = document.querySelector(selector);
        if (ta) {
            ta.style.height = '';
        }
    },

    scrollToBottom: function (selector) {
        const container = document.querySelector(selector);
        if (container) {
            container.scrollTop = container.scrollHeight;
        }
    },

    focusElement: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.focus();
        }
    }
};
