mergeInto(LibraryManager.library, {

  WebGLInitHistory: function () {
    // 1. CUSTOM HISTORY TRACKER
    window._historyIndex = 0;
    window._maxHistoryIndex = 0;
    window._portfolioCurrentState = 'intro';
    window._isSwipeLocked = false;

    window.history.replaceState({ portfolioView: 'intro', index: 0 }, '', window.location.pathname);

    window._isRepairingHistory = false;

    window.addEventListener('popstate', function (event) {
      if (window._isRepairingHistory) {
          window._isRepairingHistory = false;
          return; 
      }

      window._lastKnownIndex = window._historyIndex;
      
      var currentView = (event.state && event.state.portfolioView) ? event.state.portfolioView : 'intro';
      window._historyIndex = (event.state && event.state.index !== undefined) ? event.state.index : 0;
      window._portfolioCurrentState = currentView;
      
      try {
        if (typeof window.unityInstance !== 'undefined') {
          window.unityInstance.SendMessage('UIRoot', 'OnBrowserStateChanged', currentView);
        } else if (typeof SendMessage !== 'undefined') {
          SendMessage('UIRoot', 'OnBrowserStateChanged', currentView);
        }
      } catch (e) {
        console.warn("History caught before Unity loaded:", e);
      }
    });

    var lastNavTime = 0;
    function triggerNav(direction) {
      var now = Date.now();
      if (now - lastNavTime < 50) return;
      lastNavTime = now; 

      if (direction === 'back') window.history.back();
      else if (direction === 'forward') window.history.forward();
    }

    function sendVisualUpdate(method, data) {
        try {
            if (typeof window.unityInstance !== 'undefined') {
                window.unityInstance.SendMessage('UIRoot', method, data);
            } else if (typeof SendMessage !== 'undefined') {
                SendMessage('UIRoot', method, data);
            }
        } catch (e) {
            // Silently swallow error during loading
        }
    }

    // ==========================================
    // TRACKPAD (Decay Gravity Well)
    // ==========================================
    var trackpadAccX = 0;
    var trackpadTimer = null;
    var commitThreshold = 800;   
    var isTpSwiping = false;
    var tpDir = null;

    var isTrackpadLocked = false;
    var trackpadLockoutTimer = null;
    var prevMag = 0; 
    var slowDragAccX = 0; 

    window.addEventListener('wheel', function (e) {      
        var currentMag = Math.abs(e.deltaX);
        var currentDir = (e.deltaX < 0) ? 'back' : 'forward';

        if (Math.abs(e.deltaY) > 2 && currentMag <= Math.abs(e.deltaY) * 3.5) {
            if (isTrackpadLocked) {
                isTrackpadLocked = false;
                clearTimeout(trackpadLockoutTimer);
            }
            prevMag = currentMag; 
            slowDragAccX = 0;
            e.preventDefault();
            return; 
        }
        if (window._isSwipeLocked) {
            e.preventDefault(); 
            return;
        }

        if (isTrackpadLocked) {
            e.preventDefault(); 
            prevMag = currentMag; 
            slowDragAccX = 0;
            return; 
        }

        if (!isTpSwiping) {
            if ((currentDir === 'back' && slowDragAccX <= 0) || (currentDir === 'forward' && slowDragAccX >= 0)) {
                // Direction matches
            } else {
                slowDragAccX = 0; 
            }

            if (currentMag >= prevMag) {
                slowDragAccX += e.deltaX; 
            } else {
                var decay = currentMag + 5; 
                if (slowDragAccX > 0) {
                    slowDragAccX -= decay;
                    if (slowDragAccX < 15) slowDragAccX = 0; 
                } else if (slowDragAccX < 0) {
                    slowDragAccX += decay;
                    if (slowDragAccX > -15) slowDragAccX = 0;
                }
            }

            var isSurging = currentMag >= prevMag + 25;
            var isObviouslyFast = currentMag >= 130;
            var isDeliberateSlowDrag = Math.abs(slowDragAccX) > 250; 

            if (!isSurging && !isObviouslyFast && !isDeliberateSlowDrag) {
                prevMag = currentMag;
                return;
            }

            var canGoBack = window._historyIndex > 0;
            var canGoForward = window._historyIndex < window._maxHistoryIndex;

            if (currentDir === 'back' && !canGoBack) { prevMag = currentMag; slowDragAccX = 0; return; }
            if (currentDir === 'forward' && !canGoForward) { prevMag = currentMag; slowDragAccX = 0; return; }

            isTpSwiping = true;
            tpDir = currentDir;
            
            trackpadAccX = isDeliberateSlowDrag ? slowDragAccX * 0.5 : 0;
            slowDragAccX = 0; 
        }

        if (isTpSwiping) {
            e.preventDefault(); 

            if (currentDir === tpDir) {
                trackpadAccX += e.deltaX;
            }

            var absX = Math.abs(trackpadAccX);
            var progress = Math.min(absX / commitThreshold, 1.0);
            sendVisualUpdate('UpdateSwipeVisual', progress + '|' + tpDir);

            if (absX >= commitThreshold) {
                sendVisualUpdate('EndSwipeVisual', 'success');

                var finalDir = tpDir; 
                setTimeout(function() { triggerNav(finalDir); }, 50);

                isTpSwiping = false;
                tpDir = null;
                trackpadAccX = 0;
                clearTimeout(trackpadTimer);

                isTrackpadLocked = true;
                clearTimeout(trackpadLockoutTimer); 
                trackpadLockoutTimer = setTimeout(function() { 
                    isTrackpadLocked = false; 
                }, 300); 
                
                return;
            }

            clearTimeout(trackpadTimer);
            trackpadTimer = setTimeout(function() {
                sendVisualUpdate('EndSwipeVisual', 'cancelled');
                isTpSwiping = false;
                tpDir = null;
                trackpadAccX = 0; 
                
                isTrackpadLocked = true;
                clearTimeout(trackpadLockoutTimer);
                trackpadLockoutTimer = setTimeout(function() {
                    isTrackpadLocked = false;
                }, 350);
            }, 120); 
        }

        prevMag = currentMag; 

    }, { passive: false, capture: true });

    // ==========================================
    // MOBILE
    // ==========================================
    var touchStartX = 0, touchStartY = 0, swipeMode = null;
    var maxDragDistance = Math.max(window.innerWidth * 0.30, 1); 
    var isTouchLocked = false;

    window.addEventListener('touchstart', function (e) {
      if (window._isSwipeLocked) return;
      if (isTouchLocked) return; 

      touchStartX = e.changedTouches[0].clientX;
      touchStartY = e.changedTouches[0].clientY;
      
      if (touchStartX < 60) swipeMode = 'back';
      else if (touchStartX > window.innerWidth - 60) swipeMode = 'forward';
      else swipeMode = null;

      if (swipeMode) {
          var canGoBack = window._historyIndex > 0;
          var canGoForward = window._historyIndex < window._maxHistoryIndex;

          if (swipeMode === 'back' && !canGoBack) swipeMode = null;
          if (swipeMode === 'forward' && !canGoForward) swipeMode = null;
      }

      if (swipeMode !== null) {
          isTouchLocked = true;
      }
    }, { passive: false, capture: true });

    window.addEventListener('touchmove', function (e) {
      if (!swipeMode) return;
      
      var dx = e.changedTouches[0].clientX - touchStartX;
      var dy = e.changedTouches[0].clientY - touchStartY;
      
      if (Math.abs(dx) <= 2 * Math.abs(dy)) return;
      e.preventDefault();
      var progress = 0;
      if (swipeMode === 'back' && dx > 0) progress = Math.min(dx / maxDragDistance, 1.0);
      else if (swipeMode === 'forward' && dx < 0) progress = Math.min(Math.abs(dx) / maxDragDistance, 1.0);

      sendVisualUpdate('UpdateSwipeVisual', progress + '|' + swipeMode);
    }, { passive: false, capture: true });

    window.addEventListener('touchend', function (e) { 
        if (!swipeMode) return;

        var dx = e.changedTouches[0].clientX - touchStartX;      
        
        var didTrigger = false;
        if (swipeMode === 'back' && dx >= maxDragDistance) didTrigger = true;
        else if (swipeMode === 'forward' && dx <= -maxDragDistance) didTrigger = true;

        var result = didTrigger ? "success" : "cancelled";
        sendVisualUpdate('EndSwipeVisual', result);
        
        if (didTrigger) {
            triggerNav(swipeMode);
        }

        swipeMode = null; 
        isTouchLocked = false;
    }, { passive: false, capture: true });  

    window.addEventListener('touchcancel', function (e) { 
        if (!swipeMode) return;
        
        sendVisualUpdate('EndSwipeVisual', 'cancelled');
        swipeMode = null; 
        isTouchLocked = false; 
    }, { passive: false, capture: true });
  },
    
  WebGLPushHistoryState: function (viewNamePtr) {
    var viewName = UTF8ToString(viewNamePtr);
    window._portfolioCurrentState = viewName; 
    
    window._historyIndex++;
    window._maxHistoryIndex = window._historyIndex; 
    
    window.history.pushState({ portfolioView: viewName, index: window._historyIndex }, '', window.location.pathname + window.location.search + '#' + viewName);
  },

  WebGLGoBack: function () {
    window.history.back();
  },

  WebGLSetSwipeLock: function (isLocked) {
    window._isSwipeLocked = isLocked;
  },

  WebGLCancelHistoryChange: function () {
    var delta = window._lastKnownIndex - window._historyIndex;
    
    if (delta !== 0) {
        window._isRepairingHistory = true; 
        window._historyIndex = window._lastKnownIndex; 
        window.history.go(delta); 
    }
  }

});