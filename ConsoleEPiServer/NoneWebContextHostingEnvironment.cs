using System;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using EPiServer.Web.Hosting;

namespace ConsoleEPiServer
{
    internal class NoneWebContextHostingEnvironment : IHostingEnvironment
    {
        private VirtualPathProvider provider = null;

        public string MapPath(string virtualPath)
        {
            return Path.Combine(
                Environment.CurrentDirectory,
                virtualPath.Trim(new char[] { ' ', '~', '/' }).Replace('/', '\\'));
        }

        public void RegisterVirtualPathProvider(VirtualPathProvider virtualPathProvider)
        {
            var fieldInfo = typeof(VirtualPathProvider).GetField("_previous", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(virtualPathProvider, provider);
            provider = virtualPathProvider;
        }

        public string ApplicationID { get; set; }

        public string ApplicationPhysicalPath { get; set; }

        public string ApplicationVirtualPath { get; set; }

        public System.Web.Hosting.VirtualPathProvider VirtualPathProvider => provider;
    }
}