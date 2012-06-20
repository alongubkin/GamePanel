using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using GamePanel.Utilities;
using GamePanel.Models;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using log4net;

namespace GamePanel.BackEnd
{
    public class GameServerManager 
    {
        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<int, Process> _proccesses;
        ModelDataContext _context;

        public GameServerManager()
        {
            _proccesses = new Dictionary<int, Process>();
            _context = new ModelDataContext(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
        }

        public static GameServerManager Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        public void StartActivated()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    lock (_context)
                    {
                        _context.Servers.Where(p => p.IsActivated).ToList().ForEach(new Action<Server>((server) =>
                        {
                            Start(server);
                        }));
                    }
                }
                catch (Exception e)
                {
                    log.Warn(String.Format("Failed to start activated servers."), e);
                }
            })).Start();
        }

        public void StopActivated()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    _context.Servers.Where(p => p.IsActivated).ToList().ForEach(new Action<Server>((server) =>
                    {
                        Stop(server);
                    }));
                }
                catch (Exception e)
                {
                    log.Warn(String.Format("Failed to start activated servers."), e);
                }
            })).Start();
        }

        public void Start(Server server)
        {
            lock (_proccesses)
            {
                if (_proccesses.ContainsKey(server.Id))
                {
                    try
                    {
                        _proccesses[server.Id].Kill();
                    }
                    catch
                    {
                    }

                    _proccesses.Remove(server.Id);
                }

                var command = ServerUtils.BuildCommand(server);

                var process = new Process();
                process.StartInfo.FileName = command.Item1;
                process.StartInfo.Arguments = command.Item2;
                process.EnableRaisingEvents = true;
                process.Exited += new EventHandler((sender, e) =>
                {
                    log.InfoFormat("Unexpectedly closing server #{0}.", server.Id);
                    _context.Servers.Single(p => p.Id == server.Id).IsActivated = false;
                    _context.SubmitChanges();
                });
               
                _proccesses.Add(server.Id, process);
                process.Start();
            }
        }


        public void Update(Server server)
        {
            switch (server.ConfigurationType)
            {
                case "Valve":
                    for (int i = 0; i <= 1; i++)
                    {
                        var process = Process.Start(
                            Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "hldsupdatetool.exe"),
                            string.Format("-command update -game {0} -dir ."));

                        process.WaitForExit();
                    }
                    
                    break;

                case "Minecraft":
                    new System.Net.WebClient().DownloadFile("http://ci.bukkit.org/job/dev-CraftBukkit/promotion/latest/Recommended/artifact/target/craftbukkit-0.0.1-SNAPSHOT.jar",
                       Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "craftbukkit-0.0.1-SNAPSHOT.jar"));

                    break;
            }
        }

        public void Stop(Server server)
        {
            lock (_proccesses)
            {
                if (!_proccesses.ContainsKey(server.Id))
                    return;

                try
                {
                    _proccesses[server.Id].Kill();
                }
                catch
                {

                }

                _proccesses.Remove(server.Id);
            }
        }

        public void Restart(Server server)
        {
            Console.WriteLine("Restarts {0}", server.Id);
        
            lock (_proccesses)
            {
                if (_proccesses.ContainsKey(server.Id))
                {
                    _proccesses[server.Id].Kill();
                    _proccesses[server.Id].Start();
                }
                else
                {
                    Start(server);
                }
            }
        } 

        class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested() { }
            internal static readonly GameServerManager instance = new GameServerManager();
        }
    }
}
