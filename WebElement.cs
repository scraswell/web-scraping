using System;
using System.Drawing;

namespace Craswell.WebScraping
{
    /// <summary>
    /// Models a web element.
    /// </summary>
    public class WebElement
    {
        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        /// <value>The name of the tag.</value>
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craswell.WebScraping.WebElement"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craswell.WebScraping.WebElement"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public Point Location { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public Size Size { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craswell.WebScraping.WebElement"/> is displayed.
        /// </summary>
        /// <value><c>true</c> if displayed; otherwise, <c>false</c>.</value>
        public bool Displayed { get; set; }
    }
}

