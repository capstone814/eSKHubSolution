window.themeStorage = {
    setDarkMode: function (isDarkMode) {
        localStorage.setItem('isDarkMode', isDarkMode);
    },
    getDarkMode: function () {
        const stored = localStorage.getItem('isDarkMode');
        return stored === 'true';
    }
};
window.downloadFile = function (filename, mimeType, base64Data) {
    const link = document.createElement('a');
    link.href = `data:${mimeType};base64,${base64Data}`;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};