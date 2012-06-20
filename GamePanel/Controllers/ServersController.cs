using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GamePanel.Models;
using GamePanel.QueueService;
using GamePanel.Utilities;
using System.Xml.Linq;

namespace GamePanel.Controllers
{
    public class ServersController : Controller
    {
        ModelDataContext _context;
        QueueServiceClient _queueService;

        public ServersController()
        {
            _context = new ModelDataContext(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
            _queueService = new QueueServiceClient();
        }

        //
        // GET: /Servers/

        [Authorize]
        public ActionResult Index()
        {
            return View(new IndexModel() {
                User = _context.Users.SingleOrDefault(p => p.Id == (Guid)Membership.GetUser().ProviderUserKey),
                Games = _context.Games.ToList()
            });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Settings(int id)
        {
            var server = _context.Servers.Single(p => p.Id == id && p.UserId == (Guid)Membership.GetUser().ProviderUserKey);

            if (server.ConfigurationType == "Valve")
                return View("ValveSettings", (ValveServer)server);
            else if (server.ConfigurationType == "Minecraft")
                return View("MinecraftSettings", (MinecraftServer)server);

            return null;
        }

        [Authorize]
        [HttpPost]
        public ActionResult Settings(int id, FormCollection collection)
        {
            try
            {
                var server = _context.Servers.Single(p => p.Id == id && p.UserId == (Guid)Membership.GetUser().ProviderUserKey);
                server.Name = collection["Name"];

                switch (server.ConfigurationType)
                {
                    case "Valve":
                        var valveServer = (ValveServer)server;

                        if (collection["RconPassword"] != null)
                            valveServer.RconPassword = collection["RconPassword"];

                        if (!string.IsNullOrEmpty(collection["Pvp"]))
                            valveServer.AutoUpdate = bool.Parse(collection["Pvp"]);

                        if (!string.IsNullOrWhiteSpace(collection["Map"]))
                            valveServer.Map = collection["Map"];

                        break;

                    case "Minecraft":
                        var minecraftServer = (MinecraftServer)server;

                        if (!string.IsNullOrWhiteSpace(collection["World"]))
                            minecraftServer.World = collection["World"];

                        if (!string.IsNullOrEmpty(collection["Pvp"]))
                            minecraftServer.Pvp = bool.Parse(collection["Pvp"]);

                        if (!string.IsNullOrEmpty(collection["SpawnProtection"]))
                            minecraftServer.SpawnProtection = byte.Parse(collection["SpawnProtection"]);

                        if (!string.IsNullOrEmpty(collection["AllowFlight"]))
                            minecraftServer.AllowFlight = bool.Parse(collection["AllowFlight"]);

                        if (!string.IsNullOrEmpty(collection["SpawnAnimals"]))
                            minecraftServer.SpawnAnimals = bool.Parse(collection["SpawnAnimals"]);

                        if (!string.IsNullOrEmpty(collection["SpawnMonsters"]))
                            minecraftServer.SpawnMonsters = bool.Parse(collection["SpawnMonsters"]);

                        if (!string.IsNullOrEmpty(collection["ViewDistance"]))
                            minecraftServer.ViewDistance = byte.Parse(collection["ViewDistance"]);

                        if (!string.IsNullOrEmpty(collection["AllowNether"]))
                            minecraftServer.AllowNether = bool.Parse(collection["AllowNether"]);

                        if (!string.IsNullOrEmpty(collection["WhiteList"]))
                            minecraftServer.WhiteList = bool.Parse(collection["WhiteList"]);

                        if (!string.IsNullOrEmpty(collection["OnlineMode"]))
                            minecraftServer.OnlineMode = bool.Parse(collection["OnlineMode"]);

                        break;
                }

                _context.SubmitChanges();

                return RedirectToAction("Manage", new { id = id });
            }
            catch
            {
                return View();
            }
        }

        [Authorize]
        public int Price(int gameId, int maxPlayers, int months)
        {
            var game = _context.Games.SingleOrDefault(p => p.Id == gameId);
            var x = game.CreditsPerMonth;

            if (game == null)
                return -1;

            return ServerUtils.CalculatePrice(game.BaseCredits.GetValueOrDefault(0), game.Price, maxPlayers, months, game.CreditsPerMonth.GetValueOrDefault(1));
        }

        [Authorize]
        public ActionResult Upgrade(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            ViewData["Game"] = _context.Games.SingleOrDefault(p => p.Id == id);
            ViewData["Credits"] = _context.Users.SingleOrDefault(p => p.Id == (Guid)Membership.GetUser().ProviderUserKey).Credits;

            return View();
        }

        public ActionResult ComingSoon()
        {
            return View();
        }

        //
        // GET: /Servers/Manage/5

        [Authorize]
        public ActionResult Manage(int id)
        {
            var server = _context.Servers.SingleOrDefault(p => p.Id == id && p.UserId == (Guid)Membership.GetUser().ProviderUserKey);

            if (server.IsInstalled)
                return View(server);

            return View("NotInstalled", server);
        }

        [Authorize]
        public ActionResult Console(int id)
        {
            var server = _context.Servers.SingleOrDefault(p => p.Id == id && p.UserId == (Guid)Membership.GetUser().ProviderUserKey && p.ConfigurationType == "Valve");

            if (server.IsInstalled)
                return View(server);

            return View("NotInstalled", server);
        }

        [Authorize]
        public ActionResult ReInstall(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.ReInstall(id);
            return RedirectToAction("Wait", new { Id = id });
        }

        [Authorize]
        public ActionResult Start(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.Start(id);
            return RedirectToAction("Wait", new { Id = id });
        }

        [Authorize]
        public ActionResult Update(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.Update(id);
            return RedirectToAction("Wait", new { Id = id });
        }

        [Authorize]
        public ActionResult Stop(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.Stop(id);
            return RedirectToAction("Wait", new { Id = id });
        }

        [Authorize]
        public ActionResult Restart(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.Restart(id);
            return RedirectToAction("Wait", new { Id = id });
        }

        [Authorize]
        public bool Wait(int id)
        {
            return true;
        }

        //
        // GET: /Servers/Create

        [Authorize]
        public ActionResult Create(int id)
        {
            ViewData["Game"] = _context.Games.SingleOrDefault(p => p.Id == id);
            ViewData["Credits"] = _context.Users.SingleOrDefault(p => p.Id == (Guid)Membership.GetUser().ProviderUserKey).Credits;

            return View();
        } 

        //
        // POST: /Servers/Create

        [HttpPost]
        [Authorize]
        public ActionResult Create(int id, string name, byte maxPlayers, int months)
        {
            Server server = null;
            
            var game = _context.Games.Single(p => p.Id == id);
            var user = _context.Users.Single(p => p.Id == (Guid)Membership.GetUser().ProviderUserKey);

            var credits = ServerUtils.CalculatePrice(game.BaseCredits.GetValueOrDefault(0), game.Price, maxPlayers, months, 0);

            if (credits > user.Credits)
                throw new Exception();

            user.Credits -= credits;

            switch (game.ConfigurationType)
            {
                case "Valve":
                    // TODO: Dynamic Configuration Here
                    // Hardcoded config

                    server = new ValveServer()
                    {
                        Map = game.DefaultMap,
                        MaximumFPS = 300,
                        AutoUpdate = false,
                        RconPassword = "1"
                    };

                    break;

                case "Samp":
                    server = new SampServer()
                    {
                        HostName = name
                    };

                    break;

                case "Minecraft":
                    server = new MinecraftServer();
                    break;
            }

            var ips = XDocument.Load(ServerUtils.GetIPsFile()).Element("IPs").Elements("IP").ToList();
            var ip = ips[new Random().Next(0, ips.Count())].Value;

            server.IP = ip;
            server.Port = 20000 + server.Id;
            server.PublicIPAddress = ip + ":" + server.Port;
            server.Name = name;
            server.StartDateTime = DateTime.Now;
            server.EndDateTime = DateTime.Now.AddMonths(months);
            server.MaxPlayers = maxPlayers;
            server.Game = game;
            server.User = user;
            server.IsInstalled = false;
        
            _context.Servers.InsertOnSubmit(server);
            _context.SubmitChanges();

            server.Port = 20000 + server.Id;
            _context.SubmitChanges();

            _queueService.Install(server.Id);

            return RedirectToAction("Index");
        }


        //
        // GET: /Servers/Delete/5

        [Authorize]
        public ActionResult Delete(int id)
        {
            if (_context.Servers.Single(p => p.Id == id).UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            ViewData.Model = _context.Servers.Single(p => p.Id == id);
            return View();
        }

        //
        // POST: /Servers/Delete/5

        [HttpPost]
        [Authorize]
        public ActionResult Delete(int id, FormCollection collection)
        {
            // TODO: Add delete logic here
            var entity = _context.Servers.Single(p => p.Id == id);

            if (entity.UserId != (Guid)Membership.GetUser().ProviderUserKey)
                return null;

            _queueService.Delete(entity.Game.Abbreviation, id);

            _context.Servers.DeleteOnSubmit(entity);
            _context.SubmitChanges();

            return RedirectToAction("Index");
        }
    }

    public class IndexModel
    {
        public User User
        {
            get;
            set;
        }

        public List<Game> Games
        {
            get;
            set;
        }
    }
}
