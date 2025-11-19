mergeInto(LibraryManager.library, {
    SetPixelRatioForMobile_js: function (ratio) {
        if (typeof window !== "undefined" && window.devicePixelRatio) {

            var isMobile = /Android|iPhone|iPad|iPod|Opera Mini|IEMobile|WPDesktop|Mobile/i.test(navigator.userAgent);
            if (isMobile) {
                Object.defineProperty(window, 'devicePixelRatio', {
                    get: function () {
                        return ratio;
                    },
                    configurable: true
                });
                console.log("Custom pixel ratio set to: " + ratio + " on mobile device.");
            } else {
                console.log("Custom pixel ratio not applied: Device is not mobile.");
            }
        } else {
            console.warn("Unable to set custom pixel ratio. devicePixelRatio is not supported.");
        }
    }
});
