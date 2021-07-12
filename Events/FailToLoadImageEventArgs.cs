using System;

namespace COCOAnnotator.Events {
    public class FailToLoadImageEventArgs : EventArgs {
        public FailToLoadImageEventArgs(Uri ImageUri) {
            this.ImageUri = ImageUri;
        }

        public Uri ImageUri { get; }
    }
}
