using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.ServiceLocation;

namespace ConsoleEPiServer
{
    public class TasksLoader
    {
        private readonly IServiceLocator _serviceLocator;
        const string ExecuteMethodName = "Execute";
        const string ProgressEventName = "Progress";

        public TasksLoader(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public IEnumerable<EpiTask> Load(Assembly assembly, IEnumerable<string> tasks)
        {
            var types = assembly.ExportedTypes.Where(t => tasks.Contains(t.Name)).ToList();
            var epiTasks = new List<EpiTask>();
            foreach (var type in types)
            {
                var epiTask = new EpiTask
                {
                    Type = type,
                    Instance = this._serviceLocator.GetInstance(type),
                    ExecuteMethod = type.GetMethod(ExecuteMethodName, BindingFlags.Public | BindingFlags.Instance),
                    ProgressEvent = type.GetEvent(ProgressEventName)
                };

                epiTasks.Add(epiTask);
            }
            return epiTasks;
        }
    }

    public class EpiTask
    {
        public object Instance { get; set; }
        public Type Type { get; set; }
        public EventInfo ProgressEvent { get; set; }
        public MethodInfo ExecuteMethod { get; set; }
    }
}