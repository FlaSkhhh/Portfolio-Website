mergeInto(LibraryManager.library, {

  WebGLInitHistory: function () {
    // 1. CUSTOM HISTORY TRACKER
    window._historyIndex = 0;
    window._maxHistoryIndex = 0;
    window._portfolioCurrentState = 'intro';

    window.history.replaceState({ portfolioView: 'intro', index: 0 }, '', window.location.pathname);

    window.addEventListener('popstate', function (event) {
      var currentView = (event.state && event.state.portfolioView) ? event.state.portfolioView : 'intro';
      window._portfolioCurrentState = currentView;
      
      // Update our internal index tracker based on the secret state data
      window._historyIndex = (event.state && event.state.index !== undefined) ? event.state.index : 0;

      if (typeof window.unityInstance !== 'undefined') {
        window.unityInstance.SendMessage('UIRoot', 'OnBrowserStateChanged', currentView);
      } else {
        SendMessage('UIRoot', 'OnBrowserStateChanged', currentView);
      }
    });

    var lastNavTime = 0;
    function triggerNav(direction) {
      var now = Date.now();
      if (now - lastNavTime < 750) return;
      lastNavTime = now; 

      if (direction === 'back') window.history.back();
      else if (direction === 'forward') window.history.forward();
    }

    function sendVisualUpdate(method, data) {
        if (typeof window.unityInstance !== 'undefined') {
            window.unityInstance.SendMessage('UIRoot', method, data);
        } else {
            SendMessage('UIRoot', method, data);
        }
    }
    
    // ==========================================
    // TRACKPAD
    // ==========================================
    var trackpadAccX = 0;
    var trackpadTimer = null;
    var trackpadThreshold = 150; 
    var isTpSwiping = false;
    var tpDir = null;

   window.addEventListener('wheel', function (e) {      
        if (Math.abs(e.deltaX) <= Math.abs(e.deltaY)) return;      
        
        if (!isTpSwiping) {
            tpDir = (e.deltaX < 0) ? 'back' : 'forward';

            // 2. HISTORY-AWARE GUARDS!
            var canGoBack = window._historyIndex > 0;
            var canGoForward = window._historyIndex < window._maxHistoryIndex;

            // Instantly kill the swipe if no history exists in that direction
            if (tpDir === 'back' && !canGoBack) return;
            if (tpDir === 'forward' && !canGoForward) return;

            isTpSwiping = true;
        }

        // If swipe was rejected above, do nothing
        if (!isTpSwiping) return;

        // DIRECTIONAL CLAMPING
        if (tpDir === 'back') {
            trackpadAccX += e.deltaX;
            if (trackpadAccX > 0) trackpadAccX = 0; 
        } 
        else if (tpDir === 'forward') {
            trackpadAccX += e.deltaX;
            if (trackpadAccX < 0) trackpadAccX = 0; 
        }

        var progress = Math.min(Math.abs(trackpadAccX) / trackpadThreshold, 1.0);
        sendVisualUpdate('UpdateSwipeVisual', progress + '|' + tpDir);

        clearTimeout(trackpadTimer);
        
        trackpadTimer = setTimeout(function() {
            var didTrigger = false;
            if (tpDir === 'back' && trackpadAccX <= -trackpadThreshold) didTrigger = true; 
            else if (tpDir === 'forward' && trackpadAccX >= trackpadThreshold) didTrigger = true;
            
            sendVisualUpdate('EndSwipeVisual', didTrigger ? 'success' : 'cancelled');

            if (didTrigger) {
                // BUGFIX: Cache tpDir locally so the timeout doesn't lose it!
                var finalDir = tpDir; 
                setTimeout(function() { triggerNav(finalDir); }, 150);
            }
            
            isTpSwiping = false;
            tpDir = null;
            trackpadAccX = 0; 
        }, 150);

    }, { passive: true, capture: true });

    // ==========================================
    // MOBILE
    // ==========================================
    var touchStartX = 0, touchStartY = 0, swipeMode = null;
    var maxDragDistance = Math.max(window.innerWidth * 0.30, 1); 
    var isTouchLocked = false;

    window.addEventListener('touchstart', function (e) {
      if (isTouchLocked) return; 

      touchStartX = e.changedTouches[0].screenX;
      touchStartY = e.changedTouches[0].screenY;
      
      if (touchStartX < 40) swipeMode = 'back';
      else if (touchStartX > window.innerWidth - 40) swipeMode = 'forward';
      else swipeMode = null;

      if (swipeMode) {
          // HISTORY-AWARE GUARDS
          var canGoBack = window._historyIndex > 0;
          var canGoForward = window._historyIndex < window._maxHistoryIndex;

          if (swipeMode === 'back' && !canGoBack) swipeMode = null;
          if (swipeMode === 'forward' && !canGoForward) swipeMode = null;
      }

      if (swipeMode !== null) {
          isTouchLocked = true;
      }
    }, { passive: true, capture: true });

    window.addEventListener('touchmove', function (e) {
      if (!swipeMode) return;
      var dx = e.changedTouches[0].screenX - touchStartX;
      var dy = e.changedTouches[0].screenY - touchStartY;
      if (Math.abs(dx) <= Math.abs(dy)) return;

      var progress = 0;
      if (swipeMode === 'back' && dx > 0) progress = Math.min(dx / maxDragDistance, 1.0);
      else if (swipeMode === 'forward' && dx < 0) progress = Math.min(Math.abs(dx) / maxDragDistance, 1.0);

      sendVisualUpdate('UpdateSwipeVisual', progress + '|' + swipeMode);
    }, { passive: true, capture: true });

    window.addEventListener('touchend', function (e) { 
        if (!swipeMode) return;

        var dx = e.changedTouches[0].screenX - touchStartX;      
        
        var didTrigger = false;
        if (swipeMode === 'back' && dx >= maxDragDistance) didTrigger = true;
        else if (swipeMode === 'forward' && dx <= -maxDragDistance) didTrigger = true;

        var result = didTrigger ? "success" : "cancelled";
        sendVisualUpdate('EndSwipeVisual', result);
        
        if (didTrigger) {
            // BUGFIX: Cache swipeMode safely!
            var finalDir = swipeMode; 
            setTimeout(function() { triggerNav(finalDir); }, 150);
        }

        swipeMode = null; 
        isTouchLocked = false;
    }, { passive: true, capture: true });  
  },

  WebGLPushHistoryState: function (viewNamePtr) {
    var viewName = UTF8ToString(viewNamePtr);
    window._portfolioCurrentState = viewName; 
    
    // Increment index when navigating to a new page
    window._historyIndex++;
    
    // Pushing a new state destroys all forward history, so max becomes current
    window._maxHistoryIndex = window._historyIndex; 
    
    // Secretly pass the index to the history state
    window.history.pushState({ portfolioView: viewName, index: window._historyIndex }, '', window.location.pathname + window.location.search + '#' + viewName);
  },

  WebGLGoBack: function () {
    window.history.back();
  }
});