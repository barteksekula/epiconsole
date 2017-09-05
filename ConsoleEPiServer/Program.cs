using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EPiServer.ServiceLocation;
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

            HostingEnvironmentMutator.Mutate(AppDomain.CurrentDomain);
            Console.WriteLine("Initializing EPiServer environment");

            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Path.Combine(commandLineParams.AppPath, "web.config")
            };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var section = config.GetSection("episerver.framework") as EPiServerFrameworkSection;
            section.VirtualPathProviders.Clear();
            var connectionString = config.ConnectionStrings.ConnectionStrings["EPiServerDB"];
            var builder = new SqlConnectionStringBuilder(connectionString.ConnectionString);

            if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename))
            {
                builder.AttachDBFilename = builder.AttachDBFilename.Replace("|DataDirectory|",
                    Path.Combine(commandLineParams.AppPath, "App_Data") + "\\");
            }
            connectionString.ConnectionString = builder.ConnectionString;
            section.AppData.BasePath = "/";
            ConfigurationSource.Instance = new FileConfigurationSource(config);
            InitializationModule.FrameworkInitialization(HostType.Service);

            var assembly = Assembly.LoadFrom(commandLineParams.TaskAssembly);
            var assemblyName = AssemblyName.GetAssemblyName(commandLineParams.TaskAssembly);
            AppDomain.CurrentDomain.Load(assemblyName);

            var tasksLoader = new TasksLoader(ServiceLocator.Current);
            try
            {
                var epiTasks = tasksLoader.Load(assembly, commandLineParams.TaskTypes);
                Console.WriteLine("Executing tasks");
                foreach (var type in epiTasks)
                {
                    var watch = Stopwatch.StartNew();
                    Console.WriteLine(" - " + type.Type.Name);
                    Console.WriteLine();

                    using (var pbar = new ProgressBar(100, ""))
                    {
                        var taskRunner = new TaskRunner(pbar);
                        taskRunner.Run(type);

                        Console.WriteLine();
                        Console.WriteLine("  total time: " + watch.Elapsed);
                    }
                }

                Console.WriteLine("Tasks completed");                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}