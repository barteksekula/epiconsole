using System;
using System.Reflection;

namespace ConsoleEPiServer
{
    public class EpiTask
    {
        public object Instance { get; set; }
        public Type Type { get; set; }
        public EventInfo ProgressEvent { get; set; }
        public MethodInfo ExecuteMethod { get; set; }
    }
}