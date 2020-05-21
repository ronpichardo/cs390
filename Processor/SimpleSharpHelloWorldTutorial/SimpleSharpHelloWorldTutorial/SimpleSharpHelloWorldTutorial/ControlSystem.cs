using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.Lighting;
using DMPS3_200Test;

namespace SimpleSharpHelloWorldTutorial
{
    public class ControlSystem : CrestronControlSystem
    {
        private Tsw760 userInterface;
        private ComPort comport;
        private Glpp1DimFlv3CnPm lighting;
        private HDMISwitcher switcher;

        private const string HDMI_SWITCHER_IP = "192.168.1.15";
        private const int DESKTOP_INPUT = 1;
        private const int LAPTOP_INPUT = 2;
        private const int BLURAY_INPUT = 3;
        private const int DOC_CAMERA_INPUT = 4;

        const int SYSTEM_POWER_BUTTON = 1;
        const int SYSTEM_WAKE_BUTTON = 2;
        const int LIGHTING_STATE_BUTTON = 3;
        const int PROJECTOR_ON_BUTTON = 6;
        const int PROJECTOR_OFF_BUTTON = 7;
        const int DESKTOP_BUTTON = 8;
        const int LAPTOP_BUTTON = 9;
        const int BLURAY_BUTTON = 10;
        const int DOC_CAMERA_BUTTON = 11;

        public const string PROJECTOR_DELIMITER_STR = "\x0D";
        public const string PROJECTOR_VGA1_STR = "Source 10";
        public const string PROJECTOR_VGA2_STR = "Source 20";
        public const string PROJECTOR_HDMI_STR = "Source 30";
        public const string PROJECTOR_SVIDEO_STR = "Source 40";
        public const string PROJECTOR_POWER_ON_STR = "PWR ON";
        public const string PROJECTOR_POWER_OFF_STR = "PWR OFF";

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                userInterface = new Tsw760(03, this);
                userInterface.SigChange += new SigEventHandler(userInterface_SigChange);
                userInterface.Register();

                switcher = new HDMISwitcher(165, HDMI_SWITCHER_IP, this);

                //164 is A4 is decimal notation
                lighting = new Glpp1DimFlv3CnPm(164, this);
                lighting.Register();

                comport = ComPorts[1];
                comport.Register();
                comport.SetComPortSpec(Crestron.SimplSharpPro.ComPort.eComBaudRates.ComspecBaudRate9600,
                                        Crestron.SimplSharpPro.ComPort.eComDataBits.ComspecDataBits8,
                                        Crestron.SimplSharpPro.ComPort.eComParityType.ComspecParityNone,
                                        Crestron.SimplSharpPro.ComPort.eComStopBits.ComspecStopBits1,
                                        Crestron.SimplSharpPro.ComPort.eComProtocolType.ComspecProtocolRS232,
                                        Crestron.SimplSharpPro.ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                                        Crestron.SimplSharpPro.ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                                        false);



                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        void setSourcesOffAndSetToHDMI()
        {
            comport.Send(PROJECTOR_HDMI_STR + PROJECTOR_DELIMITER_STR);
            userInterface.BooleanInput[DESKTOP_BUTTON].BoolValue = false;
            userInterface.BooleanInput[LAPTOP_BUTTON].BoolValue = false;
            userInterface.BooleanInput[BLURAY_BUTTON].BoolValue = false;
            userInterface.BooleanInput[DOC_CAMERA_BUTTON].BoolValue = false;
        }

        void userInterface_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    {
                        if (args.Sig.BoolValue)
                        {
                            //Projector On
                            if (args.Sig.Number == PROJECTOR_ON_BUTTON)
                            {
                                userInterface.BooleanInput[PROJECTOR_ON_BUTTON].BoolValue = true;
                                userInterface.BooleanInput[PROJECTOR_OFF_BUTTON].BoolValue = false;
                                comport.Send(PROJECTOR_POWER_ON_STR + PROJECTOR_DELIMITER_STR);
                            }
                            //Projector Off
                            else if(args.Sig.Number == PROJECTOR_OFF_BUTTON)
                            {
                                userInterface.BooleanInput[PROJECTOR_ON_BUTTON].BoolValue = false;
                                userInterface.BooleanInput[PROJECTOR_OFF_BUTTON].BoolValue = true;
                                comport.Send(PROJECTOR_POWER_OFF_STR + PROJECTOR_DELIMITER_STR);
                            }
                            //DESKTOP
                            else if (args.Sig.Number == DESKTOP_BUTTON && !userInterface.BooleanInput[DESKTOP_BUTTON].BoolValue)
                            {
                                setSourcesOffAndSetToHDMI();
                                userInterface.BooleanInput[DESKTOP_BUTTON].BoolValue = true;
                                switcher.RouteVideo(DESKTOP_INPUT);
                            }
                            //LAPTOP
                            else if (args.Sig.Number == LAPTOP_BUTTON && !userInterface.BooleanInput[LAPTOP_BUTTON].BoolValue)
                            {
                                setSourcesOffAndSetToHDMI();
                                userInterface.BooleanInput[LAPTOP_BUTTON].BoolValue = true;
                                switcher.RouteVideo(LAPTOP_INPUT);
                            }
                            //BLURAY
                            else if (args.Sig.Number == BLURAY_BUTTON && !userInterface.BooleanInput[BLURAY_BUTTON].BoolValue)
                            {
                                setSourcesOffAndSetToHDMI();
                                userInterface.BooleanInput[BLURAY_BUTTON].BoolValue = true;
                                switcher.RouteVideo(BLURAY_INPUT);
                            }
                            //DOC Camera
                            else if (args.Sig.Number == DOC_CAMERA_BUTTON && !userInterface.BooleanInput[DOC_CAMERA_BUTTON].BoolValue)
                            {
                                setSourcesOffAndSetToHDMI();
                                userInterface.BooleanInput[DOC_CAMERA_BUTTON].BoolValue = true;
                                switcher.RouteVideo(DOC_CAMERA_INPUT);
                            }
                            //Lighting Toggle
                            else if (args.Sig.Number == LIGHTING_STATE_BUTTON)
                            {
                                if (userInterface.BooleanInput[LIGHTING_STATE_BUTTON].BoolValue == true)
                                {
                                    lighting.SetLoadsOff();
                                    userInterface.BooleanInput[LIGHTING_STATE_BUTTON].BoolValue = false;
                                }
                                else 
                                {
                                    lighting.SetLoadsFullOn();
                                    userInterface.BooleanInput[LIGHTING_STATE_BUTTON].BoolValue = true;
                                }
                            }
                            //Sleep Screen
                            else if (args.Sig.Number == SYSTEM_POWER_BUTTON)
                            {
                                userInterface.BooleanInput[SYSTEM_POWER_BUTTON].BoolValue = true;
                                userInterface.BooleanInput[SYSTEM_WAKE_BUTTON].BoolValue = false;
                            }
                            //Wake Screen
                            else if (args.Sig.Number == SYSTEM_WAKE_BUTTON)
                            {
                                userInterface.BooleanInput[SYSTEM_WAKE_BUTTON].BoolValue = true;
                                userInterface.BooleanInput[SYSTEM_POWER_BUTTON].BoolValue = false;
                            }
                        }
                        break;
                    }
                case eSigType.UShort:
                    break;
                case eSigType.String:
                    break;
            }
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// 
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
}