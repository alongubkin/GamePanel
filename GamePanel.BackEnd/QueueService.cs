using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.Linq;
using GamePanel.Models;
using GamePanel.Utilities;
using System.Diagnostics;
using System.Threading;
using Microsoft.Web.Administration;
using log4net;
namespace GamePanel.BackEnd
{
    public class QueueService : IQueueService
    {
        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ModelDataContext _context;

        public QueueService()
        {
            _context = new ModelDataContext(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
        }

        public void Install(int serverId)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    log.Info(String.Format("Installing server #{0}.", serverId));

                    Server server = null;

                    lock (_context)
                    {
                        server = _context.Servers.Single(p => p.Id == serverId);
                    }

                    var serverDirectory = ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id);

                    new DirectoryInfo(ServerUtils.GetOrginalGameDirectory(server.Game.Abbreviation))
                        .CopyTo(serverDirectory, true);

                    server.IsInstalled = true;

                    if (server.ConfigurationType == "Minecraft")
                    {
                        var minecraftServer = (MinecraftServer)server;

                        File.WriteAllText(
                            Path.Combine(serverDirectory, "server.properties"),
                            ServerUtils.BuildMinecraftConfiguration(new Dictionary<string, object>()
                        {
                            {"level-name", string.IsNullOrEmpty(minecraftServer.World) ? "world" : minecraftServer.World},
                            {"allow-nether", minecraftServer.AllowNether.GetValueOrDefault(true)},
                            {"view-distance", minecraftServer.ViewDistance.GetValueOrDefault(10)},
                            {"spawn-monsters", minecraftServer.SpawnMonsters.GetValueOrDefault(true)},
                            {"online-mode", minecraftServer.OnlineMode.GetValueOrDefault(true)},
                            {"max-players", minecraftServer.MaxPlayers},
                            {"server-ip", minecraftServer.IP},
                            {"pvp", minecraftServer.Pvp.GetValueOrDefault(false)},
                            {"level-seed", string.Empty},
                            {"server-port", minecraftServer.Port},
                            {"allow-flight", minecraftServer.AllowFlight.GetValueOrDefault(false)},
                            {"white-list", minecraftServer.WhiteList.GetValueOrDefault(true)}
                         })
                        );
                    }

                    var password = Guid.NewGuid().ToString().Substring(0, 8);
                    
                    FtpManager.CreateFtpUser(
                        "GamePanelFTP",
                        ServerUtils.GetServerIdentifier(server.Game.Abbreviation, server.Id),
                        password);

                    server.FtpAddress = "www.mpsite-serv.com";
                    server.FtpPassword = password;

                    lock (_context)
                    {
                        _context.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    log.Warn(String.Format("Failed to install server #{0}.", serverId), e);
                }
            })).Start();
        }

        public void ReInstall(int serverId)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    log.Info(String.Format("Reinstalling server #{0}.", serverId));

                    Server server = null;

                    lock (_context)
                    {
                        server = _context.Servers.Single(p => p.Id == serverId);
                        server.IsInstalled = false;

                        _context.SubmitChanges();
                    }

                    Directory.Delete(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), true);

                    new DirectoryInfo(ServerUtils.GetOrginalGameDirectory(server.Game.Abbreviation))
                        .CopyTo(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), true);

                    server.IsInstalled = true;

                    if (server.ConfigurationType == "Minecraft")
                    {
                        var minecraftServer = (MinecraftServer)server;

                        File.WriteAllText(
                            Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "server.properties"),
                            ServerUtils.BuildMinecraftConfiguration(new Dictionary<string, object>()
                        {
                            {"level-name", string.IsNullOrEmpty(minecraftServer.World) ? "world" : minecraftServer.World},
                            {"allow-nether", minecraftServer.AllowNether.GetValueOrDefault(true)},
                            {"view-distance", minecraftServer.ViewDistance.GetValueOrDefault(10)},
                            {"spawn-monsters", minecraftServer.SpawnMonsters.GetValueOrDefault(true)},
                            {"online-mode", minecraftServer.OnlineMode.GetValueOrDefault(true)},
                            {"max-players", minecraftServer.MaxPlayers},
                            {"server-ip", minecraftServer.IP},
                            {"pvp", minecraftServer.Pvp.GetValueOrDefault(false)},
                            {"level-seed", string.Empty},
                            {"server-port", minecraftServer.Port},
                            {"allow-flight", minecraftServer.AllowFlight.GetValueOrDefault(false)},
                            {"white-list", minecraftServer.WhiteList.GetValueOrDefault(true)}
                         })
                        );
                    }

                    lock (_context)
                    {
                        _context.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    log.Warn(String.Format("Failed to reinstall server #{0}.", serverId), e);
                }
            })).Start();
        }

        public void Delete(string abbr, int serverId)
        {
            try
            {
                log.Info(String.Format("Deleting server #{0}.", serverId));

                var directory = ServerUtils.GetServerDirectory(abbr, serverId);

                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception e)
            {
                log.Warn(String.Format("Failed to delete server #{0}.", serverId), e);
            }
        }

        public void Start(int serverId)
        {
            try
            {
                log.Info(String.Format("Starting server #{0}.", serverId));

                var server = _context.Servers.Single(p => p.Id == serverId);

                if (server.IsActivated)
                    return;

                if (server.ConfigurationType == "Minecraft")
                {
                    var minecraftServer = (MinecraftServer)server;

                    File.WriteAllText(
                        Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "server.properties"),
                        ServerUtils.BuildMinecraftConfiguration(new Dictionary<string, object>()
                        {
                            {"level-name", string.IsNullOrEmpty(minecraftServer.World) ? "world" : minecraftServer.World},
                            {"allow-nether", minecraftServer.AllowNether.GetValueOrDefault(true)},
                            {"view-distance", minecraftServer.ViewDistance.GetValueOrDefault(10)},
                            {"spawn-monsters", minecraftServer.SpawnMonsters.GetValueOrDefault(true)},
                            {"online-mode", minecraftServer.OnlineMode.GetValueOrDefault(true)},
                            {"max-players", minecraftServer.MaxPlayers},
                            {"server-ip", minecraftServer.IP},
                            {"pvp", minecraftServer.Pvp.GetValueOrDefault(false)},
                            {"level-seed", string.Empty},
                            {"server-port", minecraftServer.Port},
                            {"allow-flight", minecraftServer.AllowFlight.GetValueOrDefault(false)},
                            {"white-list", minecraftServer.WhiteList.GetValueOrDefault(true)}
                         })
                    );
                }

                GameServerManager.Instance.Start(server);

                server.IsActivated = true;

                lock (_context)
                {
                    _context.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                log.Warn(String.Format("Failed to start server #{0}.", serverId), e);
            }
        }

        public void Stop(int serverId)
        {
            try
            {
                log.Info(String.Format("Stopping server #{0}.", serverId));

                var server = _context.Servers.Single(p => p.Id == serverId);
                GameServerManager.Instance.Stop(server);

                server.IsActivated = false;

                lock (_context)
                {
                    _context.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                log.Warn(String.Format("Failed to stop server #{0}.", serverId), e);
            }
        }

        public void Update(int serverId)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    log.Info(String.Format("Updating server #{0}.", serverId));

                    var server = _context.Servers.Single(p => p.Id == serverId);

                    server.IsInstalled = false;
                    server.IsActivated = false;

                    lock (_context)
                    {
                        _context.SubmitChanges();
                    }

                    GameServerManager.Instance.Stop(server);
                    GameServerManager.Instance.Update(server);

                    server.IsInstalled = true;

                    if (server.ConfigurationType == "Minecraft")
                    {
                        var minecraftServer = (MinecraftServer)server;

                        File.WriteAllText(
                            Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "server.properties"),
                            ServerUtils.BuildMinecraftConfiguration(new Dictionary<string, object>()
                        {
                            {"level-name", string.IsNullOrEmpty(minecraftServer.World) ? "world" : minecraftServer.World},
                            {"allow-nether", minecraftServer.AllowNether.GetValueOrDefault(true)},
                            {"view-distance", minecraftServer.ViewDistance.GetValueOrDefault(10)},
                            {"spawn-monsters", minecraftServer.SpawnMonsters.GetValueOrDefault(true)},
                            {"online-mode", minecraftServer.OnlineMode.GetValueOrDefault(true)},
                            {"max-players", minecraftServer.MaxPlayers},
                            {"server-ip", minecraftServer.IP},
                            {"pvp", minecraftServer.Pvp.GetValueOrDefault(false)},
                            {"level-seed", string.Empty},
                            {"server-port", minecraftServer.Port},
                            {"allow-flight", minecraftServer.AllowFlight.GetValueOrDefault(false)},
                            {"white-list", minecraftServer.WhiteList.GetValueOrDefault(true)}
                         })
                        );
                    }

                    lock (_context)
                    {
                        _context.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    log.Warn(String.Format("Failed to update server #{0}.", serverId), e);
                }
            })).Start();
        }

        public void Restart(int serverId)
        {
            try
            {
                log.Info(String.Format("Restarting server #{0}.", serverId));

                var server = _context.Servers.Single(p => p.Id == serverId);

                server.IsActivated = false;

                lock (_context)
                {
                    _context.SubmitChanges();
                }

                if (server.ConfigurationType == "Minecraft")
                {
                    var minecraftServer = (MinecraftServer)server;

                    File.WriteAllText(
                        Path.Combine(ServerUtils.GetServerDirectory(server.Game.Abbreviation, server.Id), "server.properties"),
                        ServerUtils.BuildMinecraftConfiguration(new Dictionary<string, object>()
                        {
                            {"level-name", string.IsNullOrEmpty(minecraftServer.World) ? "world" : minecraftServer.World},
                            {"allow-nether", minecraftServer.AllowNether.GetValueOrDefault(true)},
                            {"view-distance", minecraftServer.ViewDistance.GetValueOrDefault(10)},
                            {"spawn-monsters", minecraftServer.SpawnMonsters.GetValueOrDefault(true)},
                            {"online-mode", minecraftServer.OnlineMode.GetValueOrDefault(true)},
                            {"max-players", minecraftServer.MaxPlayers},
                            {"server-ip", minecraftServer.IP},
                            {"pvp", minecraftServer.Pvp.GetValueOrDefault(false)},
                            {"level-seed", string.Empty},
                            {"server-port", minecraftServer.Port},
                            {"allow-flight", minecraftServer.AllowFlight.GetValueOrDefault(false)},
                            {"white-list", minecraftServer.WhiteList.GetValueOrDefault(true)}
                         })
                    );
                }

                GameServerManager.Instance.Restart(server);

                server.IsActivated = true;

                lock (_context)
                {
                    _context.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                log.Warn(String.Format("Failed to restart server #{0}.", serverId), e);
            }
        }
    }
}
