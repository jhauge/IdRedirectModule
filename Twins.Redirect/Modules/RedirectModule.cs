using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using log4net;
using Umbraco.Core;

namespace Twins.Redirect.Modules
{
    public class RedirectModule : IHttpModule
    {
        private const string RedirectFilepath = "/config/TwinsRedirects.config";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IHttpModule Members

        public void Dispose()
        {
            Log.DisposeIfDisposable();
        }

        public void Init(HttpApplication context)
        {
            Log.Info("Initializing redirects");
            context.BeginRequest += OnBeginRequest;
        }

        #endregion

        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string matchStr = request.Url.PathAndQuery;

            // Redirect id=1234 urls
            var idRgex = new Regex(@"^\/(default\.asp\?|\?)id=\d*$", RegexOptions.IgnoreCase);
            var redirectList = new IdRedirectList(context.Server.MapPath(RedirectFilepath));
            if (idRgex.IsMatch(matchStr) && redirectList.IsInitialized)
            {
                var idStr = matchStr.Substring(matchStr.LastIndexOf('=') + 1);
                int id;
                if (int.TryParse(idStr, out id))
                {
                    var location = redirectList.GetLocation(id);
                    Log.Debug(string.Format("Redirecting url id: {0} to {1}", id, location));
                    if (location != "404")
                    {
                        Create301Response(location, ref response);
                    }
                    response.End();
                }
            }
        }

        private static void Create301Response(string location, ref HttpResponse response)
        {
            response.StatusCode = (int) HttpStatusCode.MovedPermanently;
            response.RedirectLocation = location;
        }
    }
}