using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using T = System.Timers;

using log4net;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.PhantomJS;

namespace Craswell.WebScraping
{
    /// <summary>
    /// Models an HTTP client used for web scraping.
    /// </summary>
    public class HttpClient : IDisposable
    {
        /// <summary>
        /// The default user agent.
        /// </summary>
        private const string DefaultUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/601.2.7 (KHTML, like Gecko) Version/9.0.1 Safari/601.2.7";

        /// <summary>
        /// The default timeout in seconds.
        /// </summary>
        private const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// The http client logger.
        /// </summary>
        private ILog logger;

        /// <summary>
        /// The client is configured.
        /// </summary>
        private bool clientIsConfigured;

        /// <summary>
        /// The phantom driver.
        /// </summary>
        private PhantomJSDriver driver;

        /// <summary>
        /// The phantom driver service.
        /// </summary>
        private PhantomJSDriverService driverService;

        /// <summary>
        /// The phantom client options.
        /// </summary>
        private PhantomJSOptions options;

        /// <summary>
        /// The phantom driver timeout.
        /// </summary>
        private TimeSpan? driverTimeout;

        /// <summary>
        /// A wait handle to facilitate delay.
        /// </summary>
        private ManualResetEvent delayComplete;

        /// <summary>
        /// The delay timer, prevents actions from happening too quickly.
        /// Helps simulate an actual user.
        /// </summary>
        private T.Timer delayTimer;

        /// <summary>
        /// The proxy URL.
        /// </summary>
        private string proxyUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="Craswell.WebScraping.HttpClient"/> class.
        /// </summary>
        /// <param name="logger">The client logger.</param>
        public HttpClient(ILog logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Craswell.WebScraping.HttpClient"/> class.
        /// </summary>
        /// <param name="logger">The client logger.</param>
        /// <param name="proxyUrl">Proxy URL.</param>
        public HttpClient(ILog logger, string proxyUrl)
            : this(logger)
        {
            this.proxyUrl = proxyUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Craswell.WebScraping.HttpClient"/> class.
        /// </summary>
        /// <param name="logger">The client logger.</param>
        /// <param name="proxyUrl">Proxy URL.</param>
        /// <param name="timeoutSeconds">Timeout seconds.</param>
        public HttpClient(ILog logger, string proxyUrl, int timeoutSeconds)
            : this(logger)
        {
            this.proxyUrl = proxyUrl;
            this.driverTimeout = new TimeSpan(0, 0, timeoutSeconds);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Craswell.WebScraping.HttpClient"/> is reclaimed by garbage collection.
        /// </summary>
        ~HttpClient()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the current URI.
        /// </summary>
        /// <value>The current URI.</value>
        public string CurrentUri
        {
            get
            {
                return this.driver.Url;
            }
        }

        /// <summary>
        /// Gets the current page source.
        /// </summary>
        /// <value>The current page source.</value>
        public string CurrentPageSource
        {
            get
            {
                return this.driver.PageSource;
            }
        }

        /// <summary>
        /// Configures the PhantomJSDriver
        /// </summary>
        public void Configure()
        {
            this.ConfigureDriverTimeout();
            this.ConfigureDriverService();
            this.ConfigureDriverOptions();
            this.ConfigurePhantomJSDriver();
            this.ConfigureDelay();

            this.clientIsConfigured = true;
        }

        /// <summary>
        /// Opens the URL and waits for the selector.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <param name="awaitSelector">Wait for the selector.</param>
        public void OpenUrl(string url, string awaitSelector)
        {
            if (!this.clientIsConfigured)
            {
                this.Configure();
            }

            this.logger.InfoFormat("Navigating to {0}...", url);

            this.driver.Navigate()
                .GoToUrl(url);

            this.WaitForSelector(awaitSelector);
        }

        /// <summary>
        /// Clicks the element.
        /// </summary>
        /// <param name="elementSelector">Element selector.</param>
        public void ClickElement(string elementSelector)
        {
            var element = this.driver
                .FindElements(By.CssSelector(elementSelector))
                .Where(e => e.Displayed)
                .FirstOrDefault();

            if (element == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Could not find element with selector: {0};",
                    elementSelector));
            }

            this.logger.InfoFormat(
                "Clicking element with text {0}",
                element.Text);

            element.Click();
            this.WaitSome();
        }

        /// <summary>
        /// Closes the active window and makes the first window opened active
        /// </summary>
        public void CloseActiveWindow()
        {
            if (this.driver.WindowHandles.Count > 1)
            {
                this.logger.Info("Closing active window...");
                this.driver.Close();

                string windowHandle = this.driver
                    .WindowHandles
                    .First();

                this.logger.InfoFormat(
                    "Setting window {0} active...",
                    windowHandle);

                this.driver
                    .SwitchTo()
                    .Window(windowHandle);
            }

            this.WaitSome();
        }

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>The name of the downloaded file.</returns>
        public string DownloadFile(string url)
        {
            string absolutePath = string.Empty;

            this.logger.InfoFormat(
                "Downloading file from {0}...",
                url);

            try
            {

                using (WebClientWithCookies wc = this.CreateWebClient())
                {
                    var tmpName = Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid().ToString());

                    this.logger.InfoFormat(
                        "Using temporary file name {0}",
                        tmpName);

                    wc.DownloadFile(
                        url,
                        tmpName);

                    string fileName = this.CreateDownloadedFileName(wc);

                    this.logger.InfoFormat(
                        "Saving downloaded file as {0}",
                        fileName);

                    absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    File.Move(
                        tmpName,
                        absolutePath);
                }
            }
            catch(Exception e)
            {
                this.logger.Error(
                    "File download failed.",
                    e);
            }

            return absolutePath;
        }

        /// <summary>
        /// Toggles the active window.
        /// </summary>
        public void FocusLastOpenedWindow()
        {
            string mostRecentWindow = this.driver.WindowHandles.Last();

            this.logger.InfoFormat(
                "Making most recent window {0} active...",
                mostRecentWindow);

            this.driver
                .SwitchTo()
                .Window(mostRecentWindow);
        }

        /// <summary>
        /// Clicks the element and wait for selector.
        /// </summary>
        public void ClickElementAndWaitForSelector(string elementSelector, string awaitSelector)
        {
            this.ClickElement(elementSelector);
            this.WaitForSelector(awaitSelector);

            this.WaitSome();
        }

        /// <summary>
        /// Enters the text to the element identified.
        /// </summary>
        /// <param name="elementSelector">Element selector.</param>
        /// <param name="inputText">Input text.</param>
        public void EnterInput(string elementSelector, string inputText)
        {
            this.logger.InfoFormat(
                "Sending input to element {0}",
                elementSelector);
            
            this.GetElementByCssSelector(elementSelector)
                .SendKeys(inputText);
        }

        /// <summary>
        /// Gets the element text.
        /// </summary>
        /// <returns>The element text.</returns>
        /// <param name="elementSelector">Element selector.</param>
        public string GetElementText(string elementSelector)
        {
            this.logger.DebugFormat(
                "Getting the text for element {0}",
                elementSelector);

            string elementText = this.GetElementByCssSelector(elementSelector)
                .GetAttribute("textContent");

            this.logger.DebugFormat(
                "The text found was '{0}'",
                elementText);

            return elementText;
        }

        /// <summary>
        /// Gets the elements text.
        /// </summary>
        /// <returns>The elements text.</returns>
        /// <param name="elementSelector">Element selector.</param>
        /// <param name="attributeName">The attribute name.</param>
        public string GetElementAttributeValue(string elementSelector, string attributeName)
        {
            this.logger.InfoFormat(
                "Getting the attribute value for element {0} attribute {1}",
                elementSelector,
                attributeName);

            string value = this.GetElementByCssSelector(elementSelector)
                .GetAttribute(attributeName);

            this.logger.InfoFormat(
                "Attribute value was '{0}'",
                value);

            return value;
        }

        /// <summary>
        /// Gets the elements text.
        /// </summary>
        /// <returns>The elements text.</returns>
        /// <param name="elementSelector">Element selector.</param>
        public IList<string> GetElementsText(string elementSelector)
        {
            this.logger.InfoFormat(
                "Getting the element text for all elements matching {0}...",
                elementSelector);

            List<string> elementTexts = new List<string>();

            ReadOnlyCollection<IWebElement> elements = this.driver
                .FindElementsByCssSelector(elementSelector);

            foreach (IWebElement element in elements)
            {
                elementTexts.Add(element.GetAttribute("textContent"));
            }

            return elementTexts.ToArray();
        }

        /// <summary>
        /// Gets the elements text.
        /// </summary>
        /// <returns>The elements text.</returns>
        /// <param name="elementSelector">Element selector.</param>
        /// <param name="attributeName">The attribute name.</param>
        public IList<string> GetElementsAttributeValue(string elementSelector, string attributeName)
        {
            this.logger.InfoFormat(
                "Getting the attribute value for all elements matching {0}; attribute {1}",
                elementSelector,
                attributeName);

            List<string> attributeValues = new List<string>();

            ReadOnlyCollection<IWebElement> elements = this.driver
                .FindElementsByCssSelector(elementSelector);

            foreach (IWebElement element in elements)
            {
                attributeValues.Add(element.GetAttribute(attributeName));
            }

            return attributeValues.ToArray();
        }

        /// <summary>
        /// Enumerates the child elements of an element and maps their text to an attribute value.
        /// </summary>
        /// <param name="elementSelector">The selector to find the element.</param>
        /// <param name="attributeToMap">The attribute to map.</param>
        /// <returns>A dictionary mapping element text to an attribute value.</returns>
        public IDictionary<string, string> EnumerateSelect(string elementSelector, string attributeToMap)
        {
            Dictionary<string, string> enumeratedData = new Dictionary<string, string>();

            ReadOnlyCollection<IWebElement> elements = this.driver
                .FindElementsByCssSelector(elementSelector);

            foreach (IWebElement element in elements)
            {
                string attributeValue = element.GetAttribute(attributeToMap);

                if (string.IsNullOrEmpty(attributeValue))
                {
                    continue;
                }

                enumeratedData.Add(
                    element.GetAttribute("textContent"),
                    element.GetAttribute(attributeToMap));
            }

            return enumeratedData;
        }

        /// <summary>
        /// Releases all resource used by the <see cref="Craswell.WebScraping.HttpClient"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Craswell.WebScraping.HttpClient"/>.
        /// The <see cref="Dispose"/> method leaves the <see cref="Craswell.WebScraping.HttpClient"/> in an unusable
        /// state. After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Craswell.WebScraping.HttpClient"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Craswell.WebScraping.HttpClient"/> was occupying.</remarks>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Waits for the selector to return an element.
        /// </summary>
        /// <param name="selector">The selector.</param>
        private void WaitForSelector(string selector)
        {
            this.logger.InfoFormat(
                "Waiting for selector {0}",
                selector);

            WebDriverWait wait = new WebDriverWait(
                this.driver,
                this.driverTimeout.Value);

            wait.Until(r => By.CssSelector(selector));
        }

        /// <summary>
        /// Configures the driver service.
        /// </summary>
        private void ConfigureDriverService()
        {
            this.driverService = PhantomJSDriverService.CreateDefaultService();
            this.driverService.IgnoreSslErrors = true;
            this.driverService.WebSecurity = false;

            if (!string.IsNullOrEmpty(this.proxyUrl))
            {
                this.driverService.Proxy = "localhost:8080";
                this.driverService.ProxyType = "http";
            }
        }

        /// <summary>
        /// Configures the driver options.
        /// </summary>
        private void ConfigureDriverOptions()
        {
            this.options = new PhantomJSOptions();

            this.options.AddAdditionalCapability(
                "phantomjs.page.settings.userAgent",
                DefaultUserAgent);
        }

        /// <summary>
        /// Configures the driver timeout.
        /// </summary>
        private void ConfigureDriverTimeout()
        {
            if (!this.driverTimeout.HasValue)
            {
                this.driverTimeout = new TimeSpan(0, 0, DefaultTimeoutSeconds);
            }
        }

        /// <summary>
        /// Configures the phantom JS driver.
        /// </summary>
        private void ConfigurePhantomJSDriver()
        {
            this.driver = new PhantomJSDriver(
                this.driverService,
                this.options,
                this.driverTimeout.Value);

            this.driver.Manage()
                .Window.Size = new Size(1280, 1024);
        }

        /// <summary>
        /// Gets the element by css selector.
        /// </summary>
        /// <returns>The element by css selector.</returns>
        private IWebElement GetElementByCssSelector(string cssSelector)
        {
            return this.driver.FindElementByCssSelector(cssSelector);
        }

        /// <summary>
        /// Dispose the specified disposing.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> disposing.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.driver != null)
                {
                    this.driver.Close();
                    this.driver.Dispose();
                    this.driver = null;
                }

                if (this.driverService != null)
                {
                    this.driverService.Dispose();
                    this.driverService = null;
                }
            }
        }

        /// <summary>
        /// Configures the delay.
        /// </summary>
        private void ConfigureDelay()
        {
            this.delayComplete = new ManualResetEvent(true);
            this.delayTimer = new T.Timer(1500);

            this.delayTimer.Elapsed += this.DelayIntervalElapsed;
        }

        /// <summary>
        /// Handles the delay interval elapsed.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="e">The event arguments.</param>
        private void DelayIntervalElapsed(object sender, EventArgs e)
        {
            this.delayComplete.Set();
        }

        /// <summary>
        /// Leverages a wait handle to introduce a wait.
        /// </summary>
        private void WaitSome()
        {
            this.logger.Debug("Waiting some...");

            this.delayComplete.Reset();
            this.delayTimer.Start();
            this.delayComplete.WaitOne();
            this.delayTimer.Stop();
        }

        /// <summary>
        /// Creates the web client.
        /// </summary>
        /// <returns>The web client.</returns>
        private WebClientWithCookies CreateWebClient()
        {
            WebClientWithCookies wc;

            if (this.proxyUrl == null)
            {
                wc = new WebClientWithCookies();
            }
            else
            {
                wc = new WebClientWithCookies(this.proxyUrl);
            }

            var cookieJar = this.driver.Manage().Cookies;
            var referrer = this.CurrentUri;

            wc.ImportCookies(cookieJar);
            wc.SetReferrer(referrer);

            return wc;
        }

        /// <summary>
        /// Creates the name of the downloaded file.
        /// </summary>
        /// <returns>The downloaded file name.</returns>
        /// <param name="wc">Wc.</param>
        private string CreateDownloadedFileName(WebClientWithCookies wc)
        {
            string fileName = null;

            string contentDisposition = wc.ResponseHeaders["Content-Disposition"]
                .Split(
                    new string[] { ";" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.ToLowerInvariant().Contains("filename"))
                .SingleOrDefault();

            if (!string.IsNullOrEmpty(contentDisposition))
            {
                fileName = contentDisposition
                    .Split(
                        new string[] { "=" },
                        StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();

                if (!string.IsNullOrEmpty(fileName))
                {
                    fileName = fileName.Replace("\"", string.Empty);
                    fileName = string.Concat(Guid.NewGuid(), "_", fileName);
                }
            }

            return fileName;
        }
    }
}

