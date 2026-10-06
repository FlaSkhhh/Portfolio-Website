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
      if (now - lastNavTime < 50) return;
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
    // TRACKPAD (Decay Gravity Well)
    // ==========================================
    var trackpadAccX = 0;
    var trackpadTimer = null;
    var commitThreshold = 400;   
    var isTpSwiping = false;
    var tpDir = null;

    var isTrackpadLocked = false;
    var trackpadLockoutTimer = null;
    var prevMag = 0; 
    var slowDragAccX = 0; 

    window.addEventListener('wheel', function (e) {      
        var currentMag = Math.abs(e.deltaX);
        var currentDir = (e.deltaX < 0) ? 'back' : 'forward';

        // 1. THE STRICT DIAGONAL GATE (0-15 Degrees)
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
        
        // 2. THE GHOST EATER
        if (isTrackpadLocked) {
            e.preventDefault(); 
            prevMag = currentMag; 
            slowDragAccX = 0;
            return; 
        }

        // 3. THE KICKSTART
        if (!isTpSwiping) {
            if ((currentDir === 'back' && slowDragAccX <= 0) || (currentDir === 'forward' && slowDragAccX >= 0)) {
                // Direction matches
            } else {
                slowDragAccX = 0; 
            }

            // The Active Decay + Your Gravity Well
            if (currentMag >= prevMag) {
                slowDragAccX += e.deltaX; // Bank credit
            } else {
                var decay = currentMag + 5; // Decelerating shrinks credit
                if (slowDragAccX > 0) {
                    slowDragAccX -= decay;
                    // YOUR FIX: The Gravity Well. Snap tiny lingering noise to 0.
                    if (slowDragAccX < 15) slowDragAccX = 0; 
                } else if (slowDragAccX < 0) {
                    slowDragAccX += decay;
                    if (slowDragAccX > -15) slowDragAccX = 0;
                }
            }

            var isSurging = currentMag >= prevMag + 25;
            var isObviouslyFast = currentMag >= 80;
            var isDeliberateSlowDrag = Math.abs(slowDragAccX) > 180; 

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
            
            // Hand off 50% of the drag distance so the UI indicator spawns already connected to their finger
            trackpadAccX = isDeliberateSlowDrag ? slowDragAccX * 0.5 : 0;
            slowDragAccX = 0; 
        }

        // 4. SMOOTH DISTANCE ACCUMULATION
        if (isTpSwiping) {
            e.preventDefault(); 

            if (currentDir === tpDir) {
                trackpadAccX += e.deltaX;
            }

            var absX = Math.abs(trackpadAccX);
            var progress = Math.min(absX / commitThreshold, 1.0);
            sendVisualUpdate('UpdateSwipeVisual', progress + '|' + tpDir);

            // --- THE COMMIT ---
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

            // --- THE RELEASE (Cancel) ---
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
            triggerNav(swipeMode);
        }

        swipeMode = null; 
        isTouchLocked = false;
    }, { passive: true, capture: true });  

	window.addEventListener('touchcancel', function (e) { 
        if (!swipeMode) return;
        
        sendVisualUpdate('EndSwipeVisual', 'cancelled');
        swipeMode = null; 
        isTouchLocked = false; // CRITICAL: Frees the swipe engine if the browser aborts the touch
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