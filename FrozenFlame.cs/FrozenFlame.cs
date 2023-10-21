using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;



namespace WindowsGSM.Plugins
{
    public class FrozenFlame : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.FrozenFlame", // WindowsGSM.XXXX
            author = "Spidees",
            description = "WindowsGSM plugin for supporting FrozenFlame Dedicated Server",
            version = "1.0",
            url = "https://github.com/spidees/WindowsGSM.FrozenFlame", // Github repository link
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "1348640"; // Game server appId

        // - Standard Constructor and properties
        public FrozenFlame(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => @"FrozenFlameServer.exe"; // Game server start path
        public string FullName = "FrozenFlame Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = false;  // Does this server support output redirect?
        public int PortIncrements = 10; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "7876"; // Default port
        public string QueryPort = "7877"; // Default query port
        public string Defaultmap = "Survive.Survive"; // Default map name
        public string Maxplayers = "25"; // Default maxplayers
        public string Additional = "-MetaGameServerName=\"SERVERNAME\" -RconPort=rconPort -RconPassword=rconPassword"; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            //Use setting in serverfiles\FrozenFlame\Saved\Config\WindowsServer
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {

			//Get WAN IP from net
            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);


            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }			
			
		

            // Prepare start parameter

			string param = $"-log -LOCALLOGTIMES";
			param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" -ip={externalIp.ToString()}";
			param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -Port={_serverData.ServerPort}"; 
			param += string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? string.Empty : $" -queryPort={_serverData.ServerQueryPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}"; 	


            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


// - Stop server function
public async Task Stop(Process p) => await Task.Run(() => { p.Kill(); }); // I believe Core Keeper don't have a proper way to stop the server so just kill it

    }
}
