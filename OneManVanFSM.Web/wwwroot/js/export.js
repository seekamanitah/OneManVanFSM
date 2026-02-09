window.downloadCsvFile = function (fileName, csvContent) {
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

window.getLocalStorage = function (key) {
    return localStorage.getItem(key);
};

window.setLocalStorage = function (key, value) {
    localStorage.setItem(key, value);
};

window.triggerPrint = function () {
    setTimeout(function () { window.print(); }, 50);
};

window.openMailto = function (to, subject, body) {
    var mailto = 'mailto:' + encodeURIComponent(to)
        + '?subject=' + encodeURIComponent(subject)
        + '&body=' + encodeURIComponent(body);
    window.location.href = mailto;
};

window.isMobile = function () {
    return window.matchMedia('(max-width: 768px)').matches;
};
