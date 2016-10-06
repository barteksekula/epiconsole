using System;
using System.Collections.Specialized;
using System.IO;
using EPiServer.Web.Hosting;

namespace ConsoleEPiServer
{
    internal class HostingEnvironmentMutator
    {
        public static void Mutate(AppDomain currentDomain)
        {
            Console.WriteLine("Initializing host environment");

            var applicationPhysicalPath = new FileInfo(currentDomain.SetupInformation.ConfigurationFile).Directory.FullName;
            var environment =
                new NoneWebContextHostingEnvironment
                {
                    ApplicationVirtualPath = "/",
                    ApplicationPhysicalPath = applicationPhysicalPath
                };
            GenericHostingEnvironment.Instance = environment;
            var configParameters = new NameValueCollection
            {
                {"physicalPath", currentDomain.BaseDirectory},
                {"virtualPath", "~/"}
            };
            var virtualPathProvider = new VirtualPathNonUnifiedProvider("fallbackMapPathVPP", configParameters);
            environment.RegisterVirtualPathProvider(virtualPathProvider);
        }
    }
}