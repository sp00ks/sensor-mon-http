using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Owin;
using Owin;
using CommandLine;
using CommandLine.Text;
using NetFwTypeLib;
using Newtonsoft.Json;
using OpenHardwareMonitor.Hardware;

[assembly: OwinStartup(typeof(SensorMonHTTP.Startup1))]

namespace SensorMonHTTP
{
    public class GPUZData 
    { 
        public string SensorName { get; set; }
        public double SensorValue { get; set; }
        public string SensorUnit { get; set; }
        public GPUZData(string SensorName, double SensorValue, string SensorUnit) { 
            this.SensorName = SensorName;
            this.SensorValue = SensorValue;
            this.SensorUnit = SensorUnit;
        }
    }
    public class Startup1
    {

        Computer myComputer;
        public void Configuration(IAppBuilder app)
        {
            myComputer = new Computer()
            {
                MainboardEnabled = false,
                CPUEnabled = true,
                RAMEnabled = false,
                GPUEnabled = false,
                FanControllerEnabled = false,
                HDDEnabled = false
            };
            myComputer.Open();
            app.Run(context =>
            {
                return context.Response.WriteAsync(GetSensorInfo());
            });
        }
        public string GetSensorInfo ()
        {
            GpuzWrapper gpuz = new GpuzWrapper();
            gpuz.Open();
            List<GPUZData> GPUZSensorInfo = new List<GPUZData>();
                        
            foreach (var hardwareItem in myComputer.Hardware)
            {
                hardwareItem.Update();
                if (hardwareItem.SubHardware.Length > 0)
                {
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();
                        foreach (var sensor in subHardware.Sensors)
                        {
                            string SensorClass;
                            SensorClass = String.Format("{0}", sensor.SensorType);
                            GPUZSensorInfo.Add(new GPUZData(String.Format("Open Hardware Monitor: {0} {1}", sensor.Name, SensorClass), sensor.Value.Value, GetSensorUnit(SensorClass)));
                        }
                    }
                }
                else
                {
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        string SensorClass;
                        SensorClass = String.Format("{0}", sensor.SensorType);
                        GPUZSensorInfo.Add(new GPUZData(String.Format("Open Hardware Monitor: {0} {1}", sensor.Name, SensorClass), sensor.Value.Value, GetSensorUnit(SensorClass)));
                    }
                }
            }
            
            String s;
            for (int i = 0; (s = gpuz.SensorName(i)) != String.Empty; i++)
            {
                GPUZSensorInfo.Add(new GPUZData(String.Format("GPU-Z: {0}", gpuz.SensorName(i)), gpuz.SensorValue(i), gpuz.SensorUnit(i)));
            }
            String SensorDataJSON = JsonConvert.SerializeObject(GPUZSensorInfo, Formatting.Indented);
            return SensorDataJSON;
        }
        public string GetSensorUnit(string Temp)
        {
            string SensorUnit = null;
            switch (Temp)
            {
                case "Voltage":
                    SensorUnit = "V";
                    break;
                case "Clock":
                    SensorUnit = "MHz";
                    break;
                case "Temperature":
                    SensorUnit = "C";
                    break;
                case "Load":
                    SensorUnit = "%";
                    break;
                case "Fan":
                    SensorUnit = "rpm";
                    break;
                case "Flow":
                    SensorUnit = "Lph";
                    break;
                case "Control":
                    SensorUnit = "%";
                    break;
                case "Level":
                    SensorUnit = "%";
                    break;
                case "Power":
                    SensorUnit = "W";
                    break;
                default:
                    SensorUnit = null;
                    break;
            }
            return SensorUnit;
        }
    }
    public class clsFirewall
    {
        private INetFwProfile fwProfile = null;
        bool port_opened_by_curr_program = false;
        private int port_num;
        public clsFirewall (int port_num_arg)
        {
            port_num = port_num_arg;
        }
        protected internal void SetProfile()
        {
            INetFwMgr fwMgr = null;
            INetFwPolicy fwPolicy = null;
            try
            {
                fwMgr = GetInstance("INetFwMgr") as INetFwMgr;
                fwPolicy = fwMgr.LocalPolicy;
                fwProfile = fwPolicy.CurrentProfile;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (fwMgr != null) fwMgr = null;
                if (fwPolicy != null) fwPolicy = null;
            }
        }
        protected internal object GetInstance(string typeName)
        {
            Type tpResult = null;
            switch (typeName)
            {
                case "INetFwMgr":
                    tpResult = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
                    return Activator.CreateInstance(tpResult);
                case "INetAuthApp":
                    tpResult = Type.GetTypeFromCLSID(new Guid("{EC9846B3-2762-4A6B-A214-6ACB603462D2}"));
                    return Activator.CreateInstance(tpResult);
                case "INetOpenPort":
                    tpResult = Type.GetTypeFromCLSID(new Guid("{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}"));
                    return Activator.CreateInstance(tpResult);
                default:
                    return null;
            }
        }
        protected internal bool isPortFound(int portNumber)
        {
            bool boolResult = false;
            INetFwOpenPorts ports = null;
            Type progID = null;
            INetFwMgr firewall = null;
            INetFwOpenPort currentPort = null;
            try
            {
                progID = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                firewall = Activator.CreateInstance(progID) as INetFwMgr;
                ports = firewall.LocalPolicy.CurrentProfile.GloballyOpenPorts;
                IEnumerator portEnumerate = ports.GetEnumerator();
                while ((portEnumerate.MoveNext()))
                {
                    currentPort = portEnumerate.Current as INetFwOpenPort;
                    if (currentPort.Port == portNumber)
                    {
                        boolResult = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (ports != null) ports = null;
                if (progID != null) progID = null;
                if (firewall != null) firewall = null;
                if (currentPort != null) currentPort = null;
            }
            return boolResult;
        }
        protected internal void OpenFirewall()
        {
            INetFwOpenPorts openPorts = null;
            INetFwOpenPort openPort = null;
            try
            {
                if (isPortFound(port_num) == false)
                {
                    SetProfile();
                    openPorts = fwProfile.GloballyOpenPorts;
                    openPort = GetInstance("INetOpenPort") as INetFwOpenPort;
                    openPort.Port = port_num;
                    openPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    openPort.Name = "SensorMonHTTPPort";
                    openPorts.Add(openPort);
                    port_opened_by_curr_program = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (openPorts != null) openPorts = null;
                if (openPort != null) openPort = null;
            }
        }
        protected internal void CloseFirewall()
        {
            INetFwOpenPorts ports = null;
            try
            {
                if (isPortFound(port_num) == true)
                {
                    if (port_opened_by_curr_program == true)
                    {
                        SetProfile();
                        ports = fwProfile.GloballyOpenPorts;
                        ports.Remove(port_num, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (ports != null) ports = null;
            }
        }
    }
    class Options
    {
        [Option('p', "port", DefaultValue = "55555", Required = false, HelpText = "Port number to use")]
        public string port_val { get; set; }
        [ParserState]
        public IParserState LastParserState { get; set; }
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        private static clsFirewall objFirewall ; 
        static void Main(string[] args)
        {
            Process[] processlist = Process.GetProcesses();
            string proc_to_search_for = "GPU-Z";
            bool gpuz_proc_found = false;
            foreach(Process theprocess in processlist){
                Match match = Regex.Match(theprocess.ProcessName, proc_to_search_for);
                if (match.Success) 
                {
                    Console.WriteLine(theprocess.ProcessName + " : " + proc_to_search_for);
                    gpuz_proc_found = true;
                }
            }
            if (gpuz_proc_found == false)
            {
                Console.WriteLine("Please start GPU-Z prior to starting this program");
                Console.WriteLine("Exiting program...");
                return;
            }
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Starting SensorMonHTTP on port " + options.port_val);
            }
            try
            {
                objFirewall = new clsFirewall(Convert.ToInt32(options.port_val));
                objFirewall.OpenFirewall();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error accessing firewall - Application may not be accessible from the LAN..." + ex.Message);
            } 

            using (Microsoft.Owin.Hosting.WebApp.Start<Startup1>("http://*:" + options.port_val))
            {                
                Console.WriteLine("Web server running...");
                Console.WriteLine();
                Console.WriteLine("Press [enter] to quit...");
                Console.ReadLine();
                objFirewall.CloseFirewall();
            }
            
        }
       
    }
}
