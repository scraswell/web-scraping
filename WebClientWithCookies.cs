using System;
using System.Net;

using OQA = OpenQA.Selenium;

namespace Craswell.WebScraping
{
    /// <summary>
    /// Web client with cookies.
    /// </summary>
    public class WebClientWithCookies : WebClient
    {
        /// <summary>
        /// The referrer.
        /// </summary>
        private string referrer;

        /// <summary>
        /// The cookie jar.
        /// </summary>
        private CookieContainer cookieJar = new CookieContainer();

        /// <summary>
        /// Initializes a new instance of the <see cref="Craswell.WebScraping.WebClientWithCookies"/> class.
        /// </summary>
        public WebClientWithCookies()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Craswell.WebScraping.WebClientWithCookies"/> class.
        /// </summary>
        /// <param name="proxyUrl">Proxy URL.</param>
        public WebClientWithCookies(string proxyUrl)
        {
            this.Proxy = new WebProxy(proxyUrl);
        }

        /// <summary>
        /// Gets the web request.
        /// </summary>
        /// <returns>The web request.</returns>
        /// <param name="address">Address.</param>
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            if (request is HttpWebRequest)
            {
                HttpWebRequest webRequest = (HttpWebRequest)request;
                webRequest.CookieContainer = cookieJar;
                if (referrer != null)
                {
                    webRequest.Referer = referrer;
                }
            }
            referrer = address.ToString();

            return request;
        }

        /// <summary>
        /// Imports cookies from a webdriver cookiejar.
        /// </summary>
        /// <param name="cookieJar">Cookie jar.</param>
        public void ImportCookies(OQA.ICookieJar cookieJar)
        {
            foreach (OQA.Cookie cookie in cookieJar.AllCookies)
            {
                this.cookieJar.Add(new Cookie(
                    cookie.Name,
                    cookie.Value,
                    cookie.Path,
                    cookie.Domain));
            }
        }

        /// <summary>
        /// Sets the referrer.
        /// </summary>
        /// <param name="referrer">Referrer.</param>
        public void SetReferrer(string referrer)
        {
            this.referrer = referrer;
        }
    }
}

