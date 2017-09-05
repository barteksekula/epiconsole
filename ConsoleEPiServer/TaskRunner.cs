using System;
using System.ComponentModel;
using System.Reflection;
using EPiServer.ServiceLocation;
using ShellProgressBar;

namespace ConsoleEPiServer
{
public class TaskRunner
{
    private readonly IServiceLocator _serviceLocator;
    private ProgressBar _pbar;

    public int? tick;

    public TaskRunner(ProgressBar pBar)
    {
        this._pbar = pBar;
    }

    public void Run(EpiTask epiTask)
    {
        if (epiTask.ProgressEvent != null)
        {
            var showProgressMethod = this.GetType()
                .GetMethod("ShowProgress", BindingFlags.Instance | BindingFlags.Public);
            var tDelegate = epiTask.ProgressEvent.EventHandlerType;
            var handler = Delegate.CreateDelegate(tDelegate, this, showProgressMethod);
            epiTask.ProgressEvent.AddEventHandler(epiTask.Instance, handler);
        }
            
        epiTask.ExecuteMethod.Invoke(epiTask.Instance, null);
    }

    public void ShowProgress(object sender, ProgressChangedEventArgs eventArgs)
    {
        if (!tick.HasValue)
        {
            tick = (int)(100 / (eventArgs.ProgressPercentage));
            this._pbar.UpdateMaxTicks(tick.Value);
        }

        this._pbar.Tick();
    }
}
}