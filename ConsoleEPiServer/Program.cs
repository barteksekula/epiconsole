using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using EPiServer.Web.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using EPiServer.ServiceLocation;
using Fclp;
using ShellProgressBar;

namespace ConsoleEPiServer
{
    class Program
    {
        public static ProgressBar pbar;

        static void Main(string[] args)
        {
            pbar = new ProgressBar(100, "Starting", ConsoleColor.Cyan);
            pbar.UpdateMessage("Initializing AppDomain...");
            var p = new FluentCommandLineParser();

            string appPath = null;
            var taskNames = new List<string>();
            string taskAssembly = null;
            p.Setup<List<string>>('t', "tasks").Callback(items => taskNames = items).Required();
            p.Setup<string>('p', "appPath").Callback(item => appPath = item).Required();
            p.Setup<string>('a', "assembly").Callback(item => taskAssembly = item).Required();

            var result = p.Parse(args);

            if (result.HasErrors)
            {
                Console.Error.WriteLine(result.ErrorText);
                Console.ReadKey();
                return;
            }                    

            InitializeHostingEnvironment();
            InitalizeEPiServer();

            var types = new List<Type>();
            var binFolder = Path.Combine(appPath, @"bin\Debug");
            foreach (var file in Directory.GetFiles(binFolder, taskAssembly))
            {
                var assembly = Assembly.LoadFrom(file);
                var assemblyName = AssemblyName.GetAssemblyName(file);
                AppDomain.CurrentDomain.Load(assemblyName);

                var foundTasks = assembly.ExportedTypes.Where(t => taskNames.Contains(t.Name));
                types.AddRange(foundTasks);
            }            

            foreach (var type in types)
            {                                
                var task = ServiceLocator.Current.GetInstance(type);
                var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance);

                var eventProgress = type.GetEvent("Progress");
                var showProgressMethod = typeof(Program).GetMethod("ShowProgress", BindingFlags.Static | BindingFlags.Public);
                Type tDelegate = eventProgress.EventHandlerType;
                Delegate handler = Delegate.CreateDelegate(tDelegate, null, showProgressMethod);
                eventProgress.AddEventHandler(task, handler);
                
                method.Invoke(task, null);
            }
            
            var rootPage =
                DataFactory.Instance.Get<IContent>(ContentReference.RootPage);            

            Console.Out.WriteLine(rootPage.Name);
            Console.ReadKey();
            pbar.Dispose();
        }

        public static int? tick;

        public static void ShowProgress(object sender, ProgressChangedEventArgs eventArgs)
        {
            if (!tick.HasValue)
            {
                tick = (int) (100/(eventArgs.ProgressPercentage));
                pbar.UpdateMaxTicks(tick.Value);
            }            
            
            pbar.Tick("Currently processing " + eventArgs.ProgressPercentage);            
        }

        private static void InitalizeEPiServer()
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename =
                    @"D:\Projects\ConsoleEPiServer\alloy\alloy\web.config"
            };
            Configuration config =
                ConfigurationManager.OpenMappedExeConfiguration(
                    fileMap,
                    ConfigurationUserLevel.None);
            EPiServerFrameworkSection section =
                 config.GetSection("episerver.framework") as EPiServerFrameworkSection;
            section.VirtualPathProviders.Clear(); // use our own VPP
            var connection = config.ConnectionStrings.ConnectionStrings["EPiServerDB"];
            connection.ConnectionString = connection.ConnectionString.Replace("|DataDirectory|", @"D:\Projects\ConsoleEPiServer\alloy\alloy\App_Data\");
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