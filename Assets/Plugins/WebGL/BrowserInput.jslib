mergeInto(LibraryManager.library, {

  WebGLInitHistory: function () {
    window.history.replaceState({ portfolioView: 'intro' }, '', window.location.pathname);

    window.addEventListener('popstate', function (event) {
      var currentView = (event.state && event.state.portfolioView) ? event.state.portfolioView : 'intro';
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
      else window.history.forward();
    }

    /* Trackpad two-finger horizontal */
    window.addEventListener('wheel', function (e) {
      if (Math.abs(e.deltaX) <= Math.abs(e.deltaY)) return;
      if (e.deltaX < -20) triggerNav('back');
      else if (e.deltaX > 20) triggerNav('forward');
    }, { passive: true, capture: true });

    /* Touch, armed only from screen edges */
    var touchStartX = 0, touchStartY = 0, swipeMode = null;

    window.addEventListener('touchstart', function (e) {
      touchStartX = e.changedTouches[0].screenX;
      touchStartY = e.changedTouches[0].screenY;
      if (touchStartX < 40) swipeMode = 'back';
      else if (touchStartX > window.innerWidth - 40) swipeMode = 'forward';
      else swipeMode = null;
    }, { passive: true, capture: true });

    window.addEventListener('touchmove', function (e) {
      if (!swipeMode) return;
      var dx = e.changedTouches[0].screenX - touchStartX;
      var dy = e.changedTouches[0].screenY - touchStartY;
      if (Math.abs(dx) <= Math.abs(dy)) return;
      if (swipeMode === 'back' && dx > 75) { swipeMode = null; triggerNav('back'); }
      else if (swipeMode === 'forward' && dx < -75) { swipeMode = null; triggerNav('forward'); }
    }, { passive: true, capture: true });

    window.addEventListener('touchend', function () { swipeMode = null; }, { passive: true, capture: true });
  },

  WebGLPushHistoryState: function (viewNamePtr) {
    var viewName = UTF8ToString(viewNamePtr);
    window.history.pushState({ portfolioView: viewName }, '', window.location.pathname + window.location.search + '#' + viewName);
  },

  WebGLGoBack: function () {
    window.history.back();
  }

});