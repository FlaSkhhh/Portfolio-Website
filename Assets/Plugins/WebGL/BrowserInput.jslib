mergeInto(LibraryManager.library, {
    WebGLTrapBackButton: function() {
        // Push a dummy state so the browser has something to "go back" to
        window.history.pushState({ internal: true }, '', window.location.href);

        window.addEventListener('popstate', function(event) {
            // When the user swipes back, it pops the dummy state.
            // We immediately push it back to trap them so they can't accidentally exit the tab
            window.history.pushState({ internal: true }, '', window.location.href);

            // Send a message into Unity! 
            // Arg 1: The exact name of the GameObject in the Unity Hierarchy
            // Arg 2: The name of the public C# method to trigger
            SendMessage('UIRoot', 'OnBrowserBack');
        });
    }
});