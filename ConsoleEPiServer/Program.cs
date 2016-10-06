using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using EPiServer.Web.Hosting;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using ShellProgressBar;
using InitializationModule = EPiServer.Framework.Initialization.InitializationModule;

namespace ConsoleEPiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string errorText;
            var commandLineParams = CommandLineParams.FromArgs(args, out errorText);
            if (commandLineParams == null)
            {
                Console.Error.WriteLine(errorText);
                Console.ReadKey();
                return;
            }

            InitializeHostingEnvironment();
            Console.WriteLine("Initializing EPiServer environment");

            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Path.Combine(commandLineParams.AppPath, "web.config")
            };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var section = config.GetSection("episerver.framework") as EPiServerFrameworkSection;
            section.VirtualPathProviders.Clear(); // use our own VPP
            var connection = config.ConnectionStrings.ConnectionStrings["EPiServerDB"];
            connection.ConnectionString = connection.ConnectionString.Replace("|DataDirectory|",
                Path.Combine(commandLineParams.AppPath, "App_Data") + "\\");
            section.AppData.BasePath = "/";
            ConfigurationSource.Instance = new FileConfigurationSource(config);
            InitializationModule.FrameworkInitialization(HostType.Service);

            var assembly = Assembly.LoadFrom(commandLineParams.TaskAssembly);
            var assemblyName = AssemblyName.GetAssemblyName(commandLineParams.TaskAssembly);
            AppDomain.CurrentDomain.Load(assemblyName);

            var tasksLoader = new TasksLoader(ServiceLocator.Current);
            var epiTasks = tasksLoader.Load(assembly, commandLineParams.TaskTypes);

            Console.WriteLine("Executing tasks");
            foreach (var type in epiTasks)
            {
                var watch = Stopwatch.StartNew();
                Console.WriteLine(" - " + type.Type.Name);
                Console.WriteLine();

                using (var pbar = new ProgressBar(100, "", ConsoleColor.Cyan))
                {
                    var taskRunner = new TaskRunner(pbar);
                    taskRunner.Run(type);

                    Console.WriteLine();
                    Console.WriteLine("  total time: " + watch.Elapsed);
                }
            }

            Console.WriteLine("Tasks completed");
            Console.ReadKey();
        }

        private static void InitializeHostingEnvironment()
        {
            Console.WriteLine("Initializing host environment");

            var applicationPhysicalPath = new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile).Directory.FullName;
            var environment =
                new NoneWebContextHostingEnvironment
                {
                    ApplicationVirtualPath = "/",
                    ApplicationPhysicalPath = applicationPhysicalPath
                };
            GenericHostingEnvironment.Instance = environment;
            var configParameters = new NameValueCollection
            {
                {"physicalPath", AppDomain.CurrentDomain.BaseDirectory},
                {"virtualPath", "~/"}
            };
            var virtualPathProvider = new VirtualPathNonUnifiedProvider("fallbackMapPathVPP", configParameters);
            environment.RegisterVirtualPathProvider(virtualPathProvider);
        }
    }
}