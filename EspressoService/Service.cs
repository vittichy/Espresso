using EspressoCommon;
using System.ServiceProcess;

namespace EspressoService
{
    /// <summary>
    /// services:
    /// https://dzone.com/articles/create-windows-services-in-c
    /// 
    /// win session lock, unlock:
    /// https://stackoverflow.com/questions/16282231/get-notified-from-logon-and-logoff
    /// </summary>
    public partial class Service : ServiceBase
    {
        private readonly Common Common = new Common();

        public Service()
        {
            InitializeComponent();
            // for execution OnStart & OnStop events!
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args) 
            => Common.WriteMsg("EspressoService - START");

        protected override void OnStop() 
            => Common.WriteMsg("EspressoService - STOP");

        protected override void OnSessionChange(SessionChangeDescription changeDescription) 
            => Common.WriteMsg(changeDescription);
    }
}
