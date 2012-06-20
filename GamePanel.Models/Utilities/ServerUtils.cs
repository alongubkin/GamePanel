using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GamePanel.Models;

namespace GamePanel.Utilities
{
    public static class ServerUtils
    {
        public static int CalculatePrice(int baseCredits, int creditsPerSlot, int maxPlayers, int months, int creditsPerMonth)
        {
            return months * (baseCredits +
                 ((maxPlayers < 12) ? 0 : (maxPlayers - 10) * creditsPerSlot));
        }

        public static string GetServerIdentifier(string game, int serverId)
        {
            return string.Concat(game, '-', serverId);
        }

        public static string GetServerDirectory(string game, int serverId)
        {
            return Path.Combine(Properties.Settings.Default.ServersDirectory, GetServerIdentifier(game, serverId));
        }

        public static string GetOrginalGameDirectory(string game)
        {
            return Path.Combine(Properties.Settings.Default.OrginalDirectory, game);
        }

        public static string GetIPsFile()
        {
            return Properties.Settings.Default.IPsFile;
        }

        public static Tuple<string, string> BuildCommand(Server server)
        {
            var executableName = string.Empty;
            var arguments = new Dictionary<string, string>();

            switch (server.ConfigurationType)
            {
                case "Valve":
                    var valveServer = (ValveServer)server;

                    switch ((Engines)server.Game.Engine)
                    {
                        case Engines.Source:
                            executableName = Path.Combine(GetServerDirectory(server.Game.Abbreviation, server.Id), "orangebox\\srcds.exe");

                            arguments.Add("+ip", server.IP);
                            arguments.Add("-port", server.Port.ToString());
                            arguments.Add("-game", server.Game.DirectoryName);
                            arguments.Add("-maxplayers", server.MaxPlayers.ToString());
                            arguments.Add("-console", null);
                            arguments.Add("+fps_max", valveServer.MaximumFPS.GetValueOrDefault(300).ToString());
                            arguments.Add("+map", valveServer.Map);
                            arguments.Add("+rcon_password", valveServer.RconPassword);
                            if (valveServer.AutoUpdate.GetValueOrDefault(false))
                                arguments.Add("-autoupdate", null);

                            break;

                        case Engines.GoldSrc:
                            executableName = "hlds.exe";

                            arguments.Add("-console", null);
                            arguments.Add("-game", server.Game.DirectoryName);
                            arguments.Add("+map", valveServer.Map);
                            arguments.Add("-port", server.Port.ToString());
                            arguments.Add("+maxplayers", server.MaxPlayers.ToString());
                            arguments.Add("+sv_lan", "0");

                            if (valveServer.AutoUpdate.GetValueOrDefault(false))
                                arguments.Add("-autoupdate", null);

                            break;
                    }

                    break;

                case "Samp":

                    break;

                case "Minecraft":
                    executableName = "java.exe";
                    arguments.Add("-Xincgc", null);
                    arguments.Add("-Xmx1G", null);
                    arguments.Add("-jar", Path.Combine(GetServerDirectory(server.Game.Abbreviation, server.Id), "craftbukkit-0.0.1-SNAPSHOT.jar"));

                    break;
            }

            var sb = new StringBuilder();

            foreach (var argument in arguments)
            {
                sb.Append(argument.Key);
                sb.Append(' ');

                if (argument.Value != null)
                {
                    sb.Append(argument.Value);
                    sb.Append(' ');
                }
            }

            return new Tuple<string, string>(executableName, sb.ToString());
        }

        public static string BuildMinecraftConfiguration(Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# PLEASE USE THE GAME PANEL TO EDIT THIS FILE");

            foreach (var parameter in parameters)
            {
                sb.Append(parameter.Key);
                sb.Append('=');
                sb.Append(parameter.Value);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
