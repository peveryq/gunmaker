mergeInto(LibraryManager.library, {
    GameDistribution_CheckInit: function() {
        return window.gdSdkInit;
    },

    GameDistribution_ShowInterstitialAd: function(openCallback, closeCallback, errorCallback) {
        if (typeof gdsdk !== "undefined" && typeof gdsdk.showAd !== "undefined") {
            window.gdInterstitialCallbacks = {
                open: wasmTable.get(openCallback),
                close: wasmTable.get(closeCallback),
                error: wasmTable.get(errorCallback)
            };

            window.gdCurrentAdType = 'interstitial';

            gdsdk.showAd("interstitial")
                .catch(function(error) {
                    console.error("[GD_JS] Interstitial ad error:", error);
                    if (window.gdInterstitialCallbacks && window.gdInterstitialCallbacks.error) {
                        window.gdInterstitialCallbacks.error();
                    }
                    window.gdCurrentAdType = null;
                });
        } else {
            wasmTable.get(errorCallback)();
        }
    },

    GameDistribution_ShowRewardedAd: function(idPtr, openCallback, rewardedCallback, closeCallback, errorCallback) {
        var id = UTF8ToString(idPtr);

        if (typeof gdsdk !== "undefined" && typeof gdsdk.showAd !== "undefined") {
            window.gdRewardedCallbacks = {
                open: wasmTable.get(openCallback),
                rewarded: wasmTable.get(rewardedCallback),
                close: wasmTable.get(closeCallback),
                error: wasmTable.get(errorCallback),
                id: id
            };

            window.gdCurrentAdType = 'rewarded';

            gdsdk.showAd("rewarded")
                .catch(function(error) {
                    console.error("[GD_JS] Rewarded ad error:", error);
                    if (window.gdRewardedCallbacks && window.gdRewardedCallbacks.error) {
                        window.gdRewardedCallbacks.error();
                    }
                    window.gdCurrentAdType = null;
                });
        } else {
            wasmTable.get(errorCallback)();
        }
    }
});