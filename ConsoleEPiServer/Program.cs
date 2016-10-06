using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using EPiServer.Web.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using EPiServer.ServiceLocation;

namespace ConsoleEPiServer
{
    internal interface IOperation
    {
        void Execute(IContentRepository contentRepository);
    }

    public class ImportPagesOperation : IOperation
    {
        public ImportPagesOperation(IContentRepository contentRepository)
        {
            
        }
    }

    class Program
    {
//        [ImportMany]
//        IEnumerable<IOperation> operations;

        static void Main(string[] args)
        {
            InitializeHostingEnvironment();
            InitalizeEPiServer();

            var typeName = "ImportPagesOperation";

            // custom task
            var type = Type.GetType(typeName);
            ServiceLocator.Current.GetInstance(type);

            var rootPage =
                DataFactory.Instance.Get<IContent>(ContentReference.RootPage);

            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();

            Console.Out.WriteLine(rootPage.Name);
        }

        private static void InitalizeEPiServer()
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename =
                    @"D:\Projects\alloy\alloy\web.config"
            };
            Configuration config =
                ConfigurationManager.OpenMappedExeConfiguration(
                    fileMap,
                    ConfigurationUserLevel.None);
            EPiServerFrameworkSection section =
                 config.GetSection("episerver.framework") as EPiServerFrameworkSection;
            section.VirtualPathProviders.Clear(); // use our own VPP
            var connection = config.ConnectionStrings.ConnectionStrings["EPiServerDB"];
            connection.ConnectionString = connection.ConnectionString.Replace("|DataDirectory|", @"D:\Projects\alloy\alloy\App_Data\");
            section.AppData.BasePath = "/";
            ConfigurationSource.Instance = new FileConfigurationSource(config);
            InitializationModule.FrameworkInitialization(HostType.Service);
        }

        private static void InitializeHostingEnvironment()
        {
            NoneWebContextHostingEnvironment environment =
                new NoneWebContextHostingEnvironment
                {
                    ApplicationVirtualPath = "/",
                    ApplicationPhysicalPath = new FileInfo(
                      AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                ).Directory.FullName
                };
            GenericHostingEnvironment.Instance = environment;
            NameValueCollection configParameters = new NameValueCollection();
            configParameters.Add("physicalPath", AppDomain.CurrentDomain.BaseDirectory);
            configParameters.Add("virtualPath", "~/");
            VirtualPathNonUnifiedProvider virtualPathProvider = new
                 VirtualPathNonUnifiedProvider("fallbackMapPathVPP", configParameters);
            environment.RegisterVirtualPathProvider(virtualPathProvider);
        }
    }



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
            typeof(VirtualPathProvider).GetField(
                 "_previous", BindingFlags.NonPublic | BindingFlags.Instance)
                 .SetValue(virtualPathProvider, provider);
            provider = virtualPathProvider;
        }

        public string ApplicationID { get; set; }

        public string ApplicationPhysicalPath { get; set; }

        public string ApplicationVirtualPath { get; set; }

        public System.Web.Hosting.VirtualPathProvider VirtualPathProvider
        {
            get
            {
                return provider;
            }
        }
    }
}