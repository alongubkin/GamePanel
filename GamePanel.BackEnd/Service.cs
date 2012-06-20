using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;
using log4net;

namespace GamePanel.BackEnd
{
    public partial class Service : ServiceBase
    {
        ServiceHost _serviceHost = null;
        Process _rconServer = null;

        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Service()
        {
            InitializeComponent();
        }

        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("console"))
            {
                new Service().OnStart(args);
            }
            else
            {
                ServiceBase.Run(new Service());
            }
        }

        protected override void OnStart(string[] args)
        {
            // ServerManager.Instance.Clear();
            if (_serviceHost != null)
                _serviceHost.Close();

            try
            {
                _serviceHost = new ServiceHost(typeof(QueueService));
                _serviceHost.Open();
            }
            catch (Exception e)
            {
                log.Fatal("Failed to initialize the WCF service.", e);
            }

            _rconServer = Process.Start("python.exe",
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RconServer\\rcon.py"));

            GameServerManager.Instance.StartActivated();

            Console.ReadLine();
            OnStop();
        }

        protected override void OnStop()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }

            GameServerManager.Instance.StopActivated();
            _rconServer.Kill();
        }
    }
}
