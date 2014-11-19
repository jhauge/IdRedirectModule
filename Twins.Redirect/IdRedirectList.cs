using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Xml.Linq;
using log4net;

namespace Twins.Redirect
{
    public class IdRedirectList
    {
        private const string CacheName = "Twins.Redirect.IdRedirects";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Dictionary<int, string> _redirects;

        public IdRedirectList(string idListPath)
        {
            IsInitialized = false;
            var cache = HttpContext.Current.Cache;

            // Read from cache if possible
            if (cache[CacheName] != null)
            {
                Log.Debug("Read redirectlist from cache");
                _redirects = (Dictionary<int, string>) cache[CacheName];
                if (_redirects != null)
                {
                    IsInitialized = true;
                    return;
                };
            }

            // Read from file
            if (!File.Exists(idListPath)) return;
            var xdoc = XDocument.Load(idListPath);
            if (xdoc.Root == null) return;
            var listElement = xdoc.Root.Element("IdRedirects");
            if (listElement == null) return;
            _redirects = new Dictionary<int, string>();
            foreach (XElement el in listElement.Elements("Redirect"))
            {
                int id;
                if (!int.TryParse(el.Attribute("id").Value, out id)) continue;
                string location = el.Attribute("location").Value;
                if (!string.IsNullOrWhiteSpace(location))
                {
                    try
                    {
                        _redirects.Add(id, location);
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            // Add to cache
            var cacheDependency = new CacheDependency(idListPath);
            cache.Insert(CacheName, _redirects, cacheDependency);

            // Finish up
            IsInitialized = true;
        }

        public bool IsInitialized { get; private set; }

        public string GetLocation(int id)
        {
            try
            {
                return _redirects[id];
            }
            catch (KeyNotFoundException)
            {
                return "404";
            }
        }
    }
}