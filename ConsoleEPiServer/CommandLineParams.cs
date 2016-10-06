using System.Collections.Generic;
using Fclp;

namespace ConsoleEPiServer
{
    public class CommandLineParams
    {        
        public string AppPath { get; set; }
        public IEnumerable<string> TaskTypes { get; set; }
        public string TaskAssembly { get; set; }

        public static CommandLineParams FromArgs(string[] args, out string errorText)
        {
            var p = new FluentCommandLineParser();

            var cmdParams = new CommandLineParams();

            p.Setup<List<string>>('t', "tasks").Callback(items => cmdParams.TaskTypes = items).Required();
            p.Setup<string>('p', "appPath").Callback(item => cmdParams.AppPath = item).Required();            
            p.Setup<string>('a', "taskAssembly").Callback(item => cmdParams.TaskAssembly = item).Required();

            var result = p.Parse(args);

            if (result.HasErrors)
            {
                errorText = result.ErrorText;
                return null;
            }
            errorText = string.Empty;
            return cmdParams;
        }
    }
}