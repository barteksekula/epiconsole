using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.ServiceLocation;

namespace ConsoleEPiServer
{
    public class TasksLoader
    {
        private const string ExecuteMethodName = "Execute";
        private const string ProgressEventName = "Progress";
        private readonly IServiceLocator _serviceLocator;

        public TasksLoader(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public IEnumerable<EpiTask> Load(Assembly assembly, IEnumerable<string> tasks)
        {
            var types =
                assembly.ExportedTypes.Where(t => tasks.Contains(t.Name, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            if (types.Count != tasks.Count())
            {
                var missingTaskClasses = tasks.Except(types.Select(t => t.Name));
                throw new ArgumentException($"Invalid task class {string.Join(",", missingTaskClasses)} provided.");
            }
            var epiTasks = new List<EpiTask>();
            foreach (var type in types)
            {
                var executeMethod = type.GetMethod(ExecuteMethodName, BindingFlags.Public | BindingFlags.Instance);
                if (executeMethod == null)
                    throw new ArgumentException($"The task {type} is missing the {ExecuteMethodName} method.");

                var epiTask = new EpiTask
                {
                    Type = type,
                    Instance = _serviceLocator.GetInstance(type),
                    ExecuteMethod = executeMethod,
                    ProgressEvent = type.GetEvent(ProgressEventName)
                };

                epiTasks.Add(epiTask);
            }
            return epiTasks;
        }
    }
}