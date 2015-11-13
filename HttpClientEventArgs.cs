using System;

namespace Craswell.WebScraping
{
    /// <summary>
    /// Http client event arguments.
    /// </summary>
    public class HttpClientEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the element.
        /// </summary>
        /// <value>The element.</value>
        public WebElement Element { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the await selector.
        /// </summary>
        /// <value>The await selector.</value>
        public string AwaitSelector { get; set; }
    }
}

