window.setThemeAttribute = function (value) {
    document.documentElement.setAttribute('data-theme', value);
};

// Track internal navigation depth for reliable back detection
(function () {
    var navDepth = 0;
    var origPushState = history.pushState;
    var origReplaceState = history.replaceState;
    history.pushState = function () {
        navDepth++;
        return origPushState.apply(this, arguments);
    };
    history.replaceState = function () {
        return origReplaceState.apply(this, arguments);
    };
    window.addEventListener('popstate', function () {
        if (navDepth > 0) navDepth--;
    });
    window._getNavDepth = function () { return navDepth; };
})();

// Returns true if there is browser history to go back to
window.canGoBack = function () {
    return window._getNavDepth() > 0;
};

// Navigate back in the Blazor SPA history
window.goBack = function () {
    if (window._getNavDepth() > 0) {
        window.history.back();
        return true;
    }
    return false;
};
