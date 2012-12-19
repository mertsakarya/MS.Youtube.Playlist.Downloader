using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MS.Video.Downloader.Service.Youtube
{
    public class WebViewWrapper
    {
        // Maintain a reference to the WebView control so that 
        // we can invoke javascript 
        public WebView WebView { get; private set; }

        public WebViewWrapper(WebView webView)
        {
            WebView = webView;
        }

        // The Navigating event is a custom event, allowing us to hook/unhook 
        // from the ScriptNotify and LoadCompleted events. To invoke this 
        // event, we actually invoke the internalNavigating event. 
        private event EventHandler<NavigatingEventArgs> internalNavigating;
        public event EventHandler<NavigatingEventArgs> Navigating
        {
            add
            {
                WebView.ScriptNotify += NavigatingScriptNotify;
                WebView.LoadCompleted += WireUpNavigating;
                internalNavigating += value;
            }
            remove
            {
                WebView.ScriptNotify -= NavigatingScriptNotify;
                WebView.LoadCompleted -= WireUpNavigating;
                internalNavigating -= value;
            }
        }

        // When each page loads, run a javascript function which wires up 
        // an event handler to the onbeforeunload event on the window.  This 
        // event is raised when the window is about to unload the current page 
        // In the event handler we call window.external.notify in order to raise 
        // the ScriptNotify event on the WebView. The javascript function also 
        // returns the current document location. This is used to update the 
        // AllowedScriptNotifyUris property on the WebView in order to permit 
        // the current document to call window.external.notify (remembering 
        // that even though we injected the javascript, it’s being invoked in the 
        // context of the current document. 
        private void WireUpNavigating(object sender, NavigationEventArgs e)
        {
            var unloadFunc = "(function(){" +
                                " function navigating(){" +
                                "  window.external.notify('%%' + location.href);" +
                                "} " +
                                "window.onbeforeunload=navigating;" +
                                "return location.href;" +
                                "})();";
            var host = WebView.InvokeScript("eval", new[] { unloadFunc });
            WebView.AllowedScriptNotifyUris = new[] { new Uri(host) };
        }

        // Check to see if the ScriptNotify was raised by the javascript we 
        // injected (ie does it start with %%), and then raise the Navigating 
        // event. 
        private void NavigatingScriptNotify(object sender, NotifyEventArgs e)
        {
            if (internalNavigating == null) return;
            if (!string.IsNullOrWhiteSpace(e.Value)) {
                if (e.Value.StartsWith("%%")) {
                    internalNavigating(this, new NavigatingEventArgs() { LeavingUri = new Uri(e.Value.Trim('%')) });
                }
            }
        }

        public string SaveToString()
        {
            try {
                var retrieveHtml = "document.documentElement.outerHTML;";
                var html = WebView.InvokeScript("eval", new[] { retrieveHtml });
                return html;
            } catch {
                return null;
            }
        }
    }

    public class NavigatingEventArgs : EventArgs
    {
        public Uri LeavingUri { get; set; }
    }
}
