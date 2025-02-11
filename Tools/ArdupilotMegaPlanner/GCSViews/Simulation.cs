﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.IO;
using System.Xml; // config file
using System.Runtime.InteropServices; // dll imports
using ZedGraph; // Graphs
using ArdupilotMega;
using System.Reflection;

// Written by Michael Oborne
namespace ArdupilotMega.GCSViews
{
    public partial class Simulation : MyUserControl
    {
        MAVLink comPort = MainV2.comPort;
        UdpClient XplanesSEND;
        UdpClient MavLink;
        Socket SimulatorRECV;
        //TcpClient FlightGearSEND;
        EndPoint Remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
        byte[] udpdata = new byte[113 * 9 + 5]; // 113 types - 9 items per type (index+8) + 5 byte header
        float[][] DATA = new float[113][];
        TDataFromAeroSimRC aeroin = new TDataFromAeroSimRC();
        DateTime now = DateTime.Now;
        DateTime lastgpsupdate = DateTime.Now;
        List<string> position = new List<string>();
        int REV_pitch = 1;
        int REV_roll = 1;
        int REV_rudder = 1;
        int GPS_rate = 200;
        bool displayfull = false;
        int packetssent = 0;
        //string logdata = "";
        int tickStart = 0;
        public static int threadrun = 0;
        string simIP = "127.0.0.1";
        int simPort = 49000;
        int recvPort = 49005;

        // set defaults
        int rollgain = 10000;
        int pitchgain = 10000;
        int ruddergain = 10000;
        int throttlegain = 10000;

        // for servo graph
        RollingPointPairList list = new RollingPointPairList(1200);
        RollingPointPairList list2 = new RollingPointPairList(1200);
        RollingPointPairList list3 = new RollingPointPairList(1200);
        RollingPointPairList list4 = new RollingPointPairList(1200);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct fgIMUData
        {
            // GPS
            public double latitude;
            public double longitude;
            public double altitude;
            public double heading;
            public double velocityN;
            public double velocityE;

            // IMU
            public double accelX;
            public double accelY;
            public double accelZ;
            public double rateRoll;
            public double ratePitch;
            public double rateYaw;

            // trailer
            public uint magic;
        }

        const int AEROSIMRC_MAX_CHANNELS = 39;

        //-----------------------------------------------------------------------------
        // Two main data structures are used. This is the first one:
        //
        // This data struct is filled by AeroSIM RC with the simulation data, and sent to the plugin
        //-----------------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TDataFromAeroSimRC
        {
            public ushort nStructSize;  // size in bytes of TDataFromAeroSimRC

            //---------------------
            // Integration Time
            //---------------------
            public float Simulation_fIntegrationTimeStep;  // integration time step in seconds. This is the simulated time since last call to AeroSIMRC_Plugin_Run()

            //---------------------
            // Channels
            //---------------------

            [MarshalAs(
                UnmanagedType.ByValArray,
                SizeConst = AEROSIMRC_MAX_CHANNELS)]
            public float[] Channel_afValue_TX;  // [-1, 1] channel positions at TX sticks (i.e. raw stick positions)
            [MarshalAs(
          UnmanagedType.ByValArray,
          SizeConst = AEROSIMRC_MAX_CHANNELS)]
            public float[] Channel_afValue_RX;   // [-1, 1] channel positions at RX (i.e. after TX mixes)

            // Use the following constants as indexes for the channel arrays
            // The simulator uses internally the channel numbers for Transmitter Mode 2 (regardless of mode selected by user)
            const int CH_AILERON = 0;
            const int CH_ELEVATOR = 1;
            const int CH_THROTTLE = 2;
            const int CH_RUDDER = 3;
            const int CH_5 = 4;
            const int CH_6 = 5;
            const int CH_7 = 6;
            const int CH_PLUGIN_1 = 22; // This channel is mapped by user to any real channel number
            const int CH_PLUGIN_2 = 23; // This channel is mapped by user to any real channel number

            //---------------------
            // OSD
            //---------------------
            // Video buffer for OSD is a bitmap, 4 bytes per pixel: R G B A; The first 4 bytes are the Top-Left corner pixel
            // The size of the OSD Video Buffer is defined in plugin.txt
            // .OSD_BUFFER_SIZE, in plugin.txt, can be set to one of the following sizes: 512x512, 1024x512 or 1024x1024
            // Set OSD_nWindow_DX and OSD_nWindow_DY in struct TDataToAeroSimRC to the actual size to be displayed
            public IntPtr OSD_pVideoBuffer;

            //---------------------
            // Menu
            //---------------------
            // This variable represent the custom menu status. E.g. 0x000001 means that first menu item is ticked
            // Command  menu item bits are set to 1 when selected, but cleared in the next cycle.
            // Checkbox menu item bits remain 1 until unchecked by user, or cleared in TDataToAeroSimRC::Menu_nFlags_MenuItem_New_CheckBox_Status
            public uint Menu_nFlags_MenuItem_Status;

            //---------------------
            // Model Initial Position in current scenario
            //---------------------
            public float Scenario_fInitialModelPosX; public float Scenario_fInitialModelPosY; public float Scenario_fInitialModelPosZ; // (m) Model Initial Position on runway
            public float Scenario_fInitialModelHeading; public float Scenario_fInitialModelPitch; public float Scenario_fInitialModelRoll; // (m) Model Initial Attitude on runway

            //---------------------
            // WayPoints
            // The Description string can be freely used to add more information to the waypoint such as Altitude, WP Type (Overfly, Landing, CAP), Bearing, etc.
            //---------------------
            public float Scenario_fWPHome_X; public float Scenario_fWPHome_Y; public float Scenario_fWPHome_Lat; public float Scenario_fWPHome_Long; IntPtr Scenario_strWPHome_Description; // (m, deg, string)
            public float Scenario_fWPA_X; public float Scenario_fWPA_Y; public float Scenario_fWPA_Lat; public float Scenario_fWPA_Long; IntPtr Scenario_strWPA_Description;    // (m, deg, string)
            public float Scenario_fWPB_X; public float Scenario_fWPB_Y; public float Scenario_fWPB_Lat; public float Scenario_fWPB_Long; IntPtr Scenario_strWPB_Description;    // (m, deg, string)
            public float Scenario_fWPC_X; public float Scenario_fWPC_Y; public float Scenario_fWPC_Lat; public float Scenario_fWPC_Long; IntPtr Scenario_strWPC_Description;    // (m, deg, string)
            public float Scenario_fWPD_X; public float Scenario_fWPD_Y; public float Scenario_fWPD_Lat; public float Scenario_fWPD_Long; IntPtr Scenario_strWPD_Description;    // (m, deg, string)

            //---------------------
            // Model data
            //---------------------
            public float Model_fPosX; public float Model_fPosY; public float Model_fPosZ;    // m      Model absolute position in scenario (X=Right, Y=Front, Z=Up)
            public float Model_fVelX; public float Model_fVelY; public float Model_fVelZ;    // m/s    Model velocity
            public float Model_fAngVelX; public float Model_fAngVelY; public float Model_fAngVelZ; // rad/s  Model angular velocity (useful to implement gyroscopes)
            public float Model_fAccelX; public float Model_fAccelY; public float Model_fAccelZ;  // m/s/s  Model acceleration (useful to implement accelerometers)

            public double Model_fLatitude; public double Model_fLongitude;   // deg    Model Position in Lat/Long coordinates

            public float Model_fHeightAboveTerrain;            // m

            public float Model_fHeading;                       // rad [-PI,   PI  ] 0 = North, PI/2 = East, PI = South, - PI/2 = West
            public float Model_fPitch;                         // rad [-PI/2, PI/2] Positive pitch when nose up
            public float Model_fRoll;                          // rad [-PI,   PI  ] Positive roll when right wing Up

            // Wind
            public float Model_fWindVelX; public float Model_fWindVelY; public float Model_fWindVelZ;    // m/s   Velocity of the wind (with gusts) at model position (useful to compute air vel)

            // Engine/Motor Revs per minute
            public float Model_fEngine1_RPM;
            public float Model_fEngine2_RPM;
            public float Model_fEngine3_RPM;
            public float Model_fEngine4_RPM;

            // Battery (electric models)
            public float Model_fBatteryVoltage;          // V
            public float Model_fBatteryCurrent;          // A
            public float Model_fBatteryConsumedCharge;   // Ah
            public float Model_fBatteryCapacity;         // Ah

            // Fuel (gas & jet models)
            public float Model_fFuelConsumed;            // l
            public float Model_fFuelTankCapacity;        // l
        };

        ~Simulation()
        {
            if (threadrun == 1)
                ConnectComPort_Click(new object(), new EventArgs());

            MavLink = null;
            XplanesSEND = null;
            SimulatorRECV = null;
        }

        public Simulation()
        {
            InitializeComponent();
        }

        private void Simulation_Load(object sender, EventArgs e)
        {
            GPSrate.SelectedIndex = 2;

            xmlconfig(false);

            CreateChart(zg1);

            zg1.Visible = displayfull;

            CHKgraphpitch.Visible = displayfull;
            CHKgraphroll.Visible = displayfull;
            CHKgraphrudder.Visible = displayfull;
            CHKgraphthrottle.Visible = displayfull;
        }

        private void ConnectComPort_Click(object sender, EventArgs e)
        {
            if (threadrun == 0)
            {
                OutputLog.Clear();

                if (MainV2.comPort.BaseStream.IsOpen == false)
                {
                    MessageBox.Show("Please connect first");
                    return;
                }


                try
                {
                    quad = new HIL.QuadCopter();

                    SetupUDPRecv();

                    if (RAD_softXplanes.Checked)
                    {
                        SetupUDPXplanes();
                        SetupUDPMavLink();
                    }
                    else
                    {
                        //SetupTcpFlightGear(); // old style
                        SetupUDPXplanes(); // fg udp style
                        SetupUDPMavLink(); // pass traffic - raw
                    }

                    OutputLog.AppendText("Sim Link Started\n");
                }
                catch (Exception ex) { OutputLog.AppendText("Socket setup problem. Do you have this open already? " + ex.ToString()); }

                System.Threading.Thread t11 = new System.Threading.Thread(new System.Threading.ThreadStart(mainloop))
                {
                    Name = "Main Serial/UDP listener",
                    IsBackground = true
                };
                t11.Start();
                MainV2.threads.Add(t11);
                timer1.Start();
            }
            else
            {

                timer1.Stop();
                threadrun = 0;
                if (SimulatorRECV != null)
                    SimulatorRECV.Close();
                if (SimulatorRECV != null && SimulatorRECV.Connected)
                    SimulatorRECV.Disconnect(true);
                if (MavLink != null)
                    MavLink.Close();
                position.Clear();

                if (XplanesSEND != null)
                    XplanesSEND.Close();

                //                if (comPort.BaseStream.IsOpen)
                //                    comPort.stopall(true);

                OutputLog.AppendText("Sim Link Stopped\n");

                System.Threading.Thread.Sleep(1000);
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Sets config hash for write on application exit
        /// </summary>
        /// <param name="write">true/false</param>
        private void xmlconfig(bool write)
        {
            if (write)
            {
                ArdupilotMega.MainV2.config["REV_roll"] = CHKREV_roll.Checked.ToString();
                ArdupilotMega.MainV2.config["REV_pitch"] = CHKREV_pitch.Checked.ToString();
                ArdupilotMega.MainV2.config["REV_rudder"] = CHKREV_rudder.Checked.ToString();
                ArdupilotMega.MainV2.config["GPSrate"] = GPSrate.Text;
                ArdupilotMega.MainV2.config["Xplanes"] = RAD_softXplanes.Checked.ToString();

                ArdupilotMega.MainV2.config["MAVrollgain"] = TXT_rollgain.Text;
                ArdupilotMega.MainV2.config["MAVpitchgain"] = TXT_pitchgain.Text;
                ArdupilotMega.MainV2.config["MAVruddergain"] = TXT_ruddergain.Text;
                ArdupilotMega.MainV2.config["MAVthrottlegain"] = TXT_throttlegain.Text;

                ArdupilotMega.MainV2.config["CHKdisplayall"] = CHKdisplayall.Checked.ToString();

                ArdupilotMega.MainV2.config["simIP"] = simIP;
                ArdupilotMega.MainV2.config["recvPort"] = recvPort;

                ArdupilotMega.MainV2.config["simPort"] = simPort.ToString();
            }
            else
            {
                foreach (string key in ArdupilotMega.MainV2.config.Keys)
                {
                    switch (key)
                    {
                        case "simIP":
                            simIP = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "simPort":
                            simPort = int.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "recvPort":
                            recvPort = int.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "REV_roll":
                            CHKREV_roll.Checked = bool.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "REV_pitch":
                            CHKREV_pitch.Checked = bool.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "REV_rudder":
                            CHKREV_rudder.Checked = bool.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "GPSrate":
                            GPSrate.Text = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "Xplanes":
                            RAD_softXplanes.Checked = bool.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            break;
                        case "MAVrollgain":
                            TXT_rollgain.Text = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "MAVpitchgain":
                            TXT_pitchgain.Text = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "MAVruddergain":
                            TXT_ruddergain.Text = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "MAVthrottlegain":
                            TXT_throttlegain.Text = ArdupilotMega.MainV2.config[key].ToString();
                            break;
                        case "CHKdisplayall":
                            CHKdisplayall.Checked = bool.Parse(ArdupilotMega.MainV2.config[key].ToString());
                            displayfull = CHKdisplayall.Checked;
                            break;
                        default:
                            break;
                    }
                }
            }



        }

        FGNetFDM lastfdmdata = new FGNetFDM();

        const int FG_MAX_ENGINES = 4;
        const int FG_MAX_WHEELS = 3;
        const int FG_MAX_TANKS = 4;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FGNetFDM
        {
            public uint version;		// increment when data values change
            public uint padding;		// padding

            // Positions
            public double longitude;		// geodetic (radians)
            public double latitude;		// geodetic (radians)
            public double altitude;		// above sea level (meters)
            public float agl;			// above ground level (meters)
            public float phi;			// roll (radians)
            public float theta;		// pitch (radians)
            public float psi;			// yaw or true heading (radians)
            public float alpha;                // angle of attack (radians)
            public float beta;                 // side slip angle (radians)

            // Velocities
            public float phidot;		// roll rate (radians/sec)
            public float thetadot;		// pitch rate (radians/sec)
            public float psidot;		// yaw rate (radians/sec)
            public float vcas;		        // calibrated airspeed
            public float climb_rate;		// feet per second
            public float v_north;              // north velocity in local/body frame, fps
            public float v_east;               // east velocity in local/body frame, fps
            public float v_down;               // down/vertical velocity in local/body frame, fps
            public float v_wind_body_north;    // north velocity in local/body frame
            // relative to local airmass, fps
            public float v_wind_body_east;     // east velocity in local/body frame
            // relative to local airmass, fps
            public float v_wind_body_down;     // down/vertical velocity in local/body
            // frame relative to local airmass, fps

            // Accelerations
            public float A_X_pilot;		// X accel in body frame ft/sec^2
            public float A_Y_pilot;		// Y accel in body frame ft/sec^2
            public float A_Z_pilot;		// Z accel in body frame ft/sec^2

            // Stall
            public float stall_warning;        // 0.0 - 1.0 indicating the amount of stall
            public float slip_deg;		// slip ball deflection


            // Pressure

            // Engine status
            uint num_engines;          // Number of valid engines
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            uint[] eng_state;// Engine state (off, cranking, running)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] rpm;           // Engine RPM rev/min
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] fuel_flow; // Fuel flow gallons/hr
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] fuel_px;   // Fuel pressure psi
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] egt;           // Exhuast gas temp deg F
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] cht;           // Cylinder head temp deg F
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] mp_osi;    // Manifold pressure
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] tit;           // Turbine Inlet Temperature
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] oil_temp;  // Oil temp deg F
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_ENGINES)]
            float[] oil_px;    // Oil pressure psi

            // Consumables
            uint num_tanks;       // Max number of fuel tanks
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_TANKS)]
            float[] fuel_quantity;

            // Gear status
            uint num_wheels;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_WHEELS)]
            uint[] wow;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_WHEELS)]
            float[] gear_pos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_WHEELS)]
            float[] gear_steer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FG_MAX_WHEELS)]
            float[] gear_compression;

            // Environment
            uint cur_time;           // current unix time
            // FIXME: make this uint64_t before 2038
            int warp;                // offset in seconds to unix time
            float visibility;            // visibility in meters (for env. effects)

            // Control surface positions (normalized values)
            float elevator;
            float elevator_trim_tab;
            float left_flap;
            float right_flap;
            float left_aileron;
            float right_aileron;
            float rudder;
            float nose_wheel;
            float speedbrake;
            float spoilers;
        }

        const float ft2m = (float)(1.0 / 3.2808399);
        const float rad2deg = (float)(180 / Math.PI);
        const float deg2rad = (float)(1.0 / rad2deg);
        const float kts2fps = (float)1.68780986;

        private void mainloop()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US"); 
            threadrun = 1;
            Remote = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));

            DateTime lastdata = DateTime.MinValue;

            while (threadrun == 1)
            {
                if (comPort.BaseStream.IsOpen == false) { break; }
                // re-request servo data
                if (!(lastdata.AddSeconds(8) > DateTime.Now))
                {
                    Console.WriteLine("REQ streams - sim");
                    try
                    {
                        if (CHK_quad.Checked)
                        {
                            comPort.requestDatastream((byte)ArdupilotMega.MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_RAW_CONTROLLER, 0); // request servoout
                        }
                        else
                        {
                            comPort.requestDatastream((byte)ArdupilotMega.MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_RAW_CONTROLLER, 50); // request servoout
                        }
                    }
                    catch { }
                    lastdata = DateTime.Now; // prevent flooding
                }
                if (SimulatorRECV.Available > 0)
                {
                    udpdata = new byte[udpdata.Length];
                    try
                    {
                        int recv = SimulatorRECV.ReceiveFrom(udpdata, ref Remote);

                        RECVprocess(udpdata, recv, comPort);
                    }
                    catch (Exception ex) { OutputLog.AppendText("Xplanes Data Problem - You need DATA IN/OUT 3, 4, 17, 18, 19, 20\n" + ex.Message + "\n"); }
                }
                if (MavLink != null && MavLink.Client != null && MavLink.Client.Connected && MavLink.Available > 0)
                {
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    try
                    {
                        Byte[] receiveBytes = MavLink.Receive(ref RemoteIpEndPoint);

                        Console.WriteLine("sending " + receiveBytes[5]);

                        if (receiveBytes[5] == 39)
                        {
                            Console.WriteLine("wp no " + receiveBytes[9]); // ??
                        }

                        comPort.BaseStream.Write(receiveBytes, 0, receiveBytes.Length);
                    }
                    catch { }
                }
                if (comPort.BaseStream.IsOpen == false) { break; }
                try
                {
                    MainV2.cs.UpdateCurrentSettings(null); // when true this uses alot more cpu time

                    if ((DateTime.Now - simsendtime).TotalMilliseconds > 19)
                    {
                        hzcount++;
                        simsendtime = DateTime.Now;
                        processArduPilot();
                    }
                }
                catch (Exception ex) { Console.WriteLine("SIM Main loop exception " + ex.ToString()); }

                if (hzcounttime.Second != DateTime.Now.Second)
                {
                    //                    Console.WriteLine("SIM hz {0}", hzcount);
                    hzcount = 0;
                    hzcounttime = DateTime.Now;
                }



                System.Threading.Thread.Sleep(5); // this controls send speed  to sim                
            }

        }

        int hzcount = 0;
        DateTime hzcounttime = DateTime.Now;

        DateTime simsendtime = DateTime.Now;

        private void SetupUDPRecv()
        {
            // setup receiver
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, recvPort);

            SimulatorRECV = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);

            SimulatorRECV.Bind(ipep);

            OutputLog.AppendText("Listerning on port " + recvPort + " (sim->planner)\n");
        }

        private void SetupUDPXplanes()
        {
            // setup sender
            XplanesSEND = new UdpClient(simIP, simPort);

            OutputLog.AppendText("Sending to port " + simPort + " (planner->sim)\n");
        }

        private void SetupUDPMavLink()
        {
            // setup sender
            MavLink = new UdpClient("127.0.0.1", 14550);
        }

        /// <summary>
        /// From http://code.google.com/p/gentlenav/source/browse/trunk/Tools/XP_UDB_HILSIM/utility.cpp
        /// Converts from xplanes to fixed body ref
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        public static void FLIGHTtoBCBF(ref float x, ref float y, ref float z, float alpha, float beta)
        {
            ﻿float Ca = (float)Math.Cos(alpha);
             float Cb = (float)Math.Cos(beta);
             float Sa = (float)Math.Sin(alpha);
             float Sb = (float)Math.Sin(beta);

             float X_plane = (x * Ca * Cb) - (z * Sa * Cb) - (y * Sb);
             float Y_plane = (z * Sa * Sb) - (x * Ca * Sb) - (y * Cb);
             float Z_plane = (x * Sa) + (z * Ca);

             x = X_plane;
            ﻿y = Y_plane;
            ﻿z = Z_plane;
        }

        void OGLtoBCBF(ref float x, ref float y, ref float z, float phi, float theta, float psi)
        {
            float x_NED, y_NED, z_NED;
            float Cr, Cp, Cy;
            float Sr, Sp, Sy;

            //Accelerations in X-Plane are expressed in the local OpenGL reference frame, for whatever reason. 
            //This coordinate system is defined as follows (taken from the X-Plane SDK Wiki):

            //	The origin 0,0,0 is on the surface of the earth at sea level at some "reference point".
            //	The +X axis points east from the reference point.
            //	The +Z axis points south from the reference point.
            //	The +Y axis points straight up away from the center of the earth at the reference point.

            // First we shall convert from this East Up South frame, to a more conventional NED (North East Down) frame.
            x_NED = -1.0f * z;
            y_NED = x;
            z_NED = -1.0f * y;

            // Next calculate cos & sin of angles for use in the transformation matrix.
            // r, p & y subscripts stand for roll pitch and yaw.

            Cr = (float)Math.Cos(phi);
            Cp = (float)Math.Cos(theta);
            Cy = (float)Math.Cos(psi);
            Sr = (float)Math.Sin(phi);
            Sp = (float)Math.Sin(theta);
            Sy = (float)Math.Sin(psi);

            // Next we need to rotate our accelerations from the NED reference frame, into the body fixed reference frame

            // THANKS TO GEORGE M SIOURIS WHOSE "MISSILE GUIDANCE AND CONTROL SYSTEMS" BOOK SEEMS TO BE THE ONLY EASY TO FIND REFERENCE THAT
            // ACTUALLY GETS THE NED TO BODY FRAME ROTATION MATRIX CORRECT!!

            // CpCy, CpSy, -Sp					| local_ax
            // SrSpCy-CrSy, SrSpSy+CrCy, SrCp	| local_ay
            // CrSpCy+SrSy, CrSpSy-SrCy, CrCp	| local_az

            x = (x_NED * Cp * Cy) + (y_NED * Cp * Sy) - (z_NED * Sp);
            y = (x_NED * ((Sr * Sp * Cy) - (Cr * Sy))) + (y_NED * ((Sr * Sp * Sy) + (Cr * Cy))) + (z_NED * Sr * Cp);
            z = (x_NED * ((Cr * Sp * Cy) + (Sr * Sy))) + (y_NED * ((Cr * Sp * Sy) - (Sr * Cy))) + (z_NED * Cr * Cp);
        }

        double sin(double rad)
        {
            return Math.Sin(rad);
        }

        double cos(double rad)
        {
            return Math.Cos(rad);
        }

        float oldax =0, olday =0, oldaz = 0;
        DateTime oldtime = DateTime.Now;
#if MAVLINK10
        ArdupilotMega.MAVLink.__mavlink_gps_raw_int_t oldgps = new MAVLink.__mavlink_gps_raw_int_t();
		#endif

        ArdupilotMega.MAVLink.__mavlink_attitude_t oldatt = new ArdupilotMega.MAVLink.__mavlink_attitude_t();

        /// <summary>
        /// Recevied UDP packet, process and send required data to serial port.
        /// </summary>
        /// <param name="data">Packet</param>
        /// <param name="receviedbytes">Length</param>
        /// <param name="comPort">Com Port</param>
        private void RECVprocess(byte[] data, int receviedbytes, ArdupilotMega.MAVLink comPort)
        {
#if MAVLINK10
            ArdupilotMega.MAVLink.__mavlink_hil_state_t hilstate = new ArdupilotMega.MAVLink.__mavlink_hil_state_t();

            ArdupilotMega.MAVLink.__mavlink_gps_raw_int_t gps = new ArdupilotMega.MAVLink.__mavlink_gps_raw_int_t();
#else
            ArdupilotMega.MAVLink.__mavlink_gps_raw_t gps = new ArdupilotMega.MAVLink.__mavlink_gps_raw_t();
#endif
            ArdupilotMega.MAVLink.__mavlink_raw_imu_t imu = new ArdupilotMega.MAVLink.__mavlink_raw_imu_t();


            ArdupilotMega.MAVLink.__mavlink_attitude_t att = new ArdupilotMega.MAVLink.__mavlink_attitude_t();

            ArdupilotMega.MAVLink.__mavlink_vfr_hud_t asp = new ArdupilotMega.MAVLink.__mavlink_vfr_hud_t();

            if (data[0] == 'D' && data[1] == 'A')
            {
                // Xplanes sends
                // 5 byte header
                // 1 int for the index - numbers on left of output
                // 8 floats - might be useful. or 0 if not
                int count = 5;
                while (count < receviedbytes)
                {
                    int index = BitConverter.ToInt32(data, count);

                    DATA[index] = new float[8];

                    DATA[index][0] = BitConverter.ToSingle(data, count + 1 * 4); ;
                    DATA[index][1] = BitConverter.ToSingle(data, count + 2 * 4); ;
                    DATA[index][2] = BitConverter.ToSingle(data, count + 3 * 4); ;
                    DATA[index][3] = BitConverter.ToSingle(data, count + 4 * 4); ;
                    DATA[index][4] = BitConverter.ToSingle(data, count + 5 * 4); ;
                    DATA[index][5] = BitConverter.ToSingle(data, count + 6 * 4); ;
                    DATA[index][6] = BitConverter.ToSingle(data, count + 7 * 4); ;
                    DATA[index][7] = BitConverter.ToSingle(data, count + 8 * 4); ;

                    count += 36; // 8 * float
                }

                att.pitch = (DATA[18][0] * deg2rad);
                att.roll = (DATA[18][1] * deg2rad);
                att.yaw = (DATA[18][2] * deg2rad);
                att.pitchspeed = (DATA[17][0]);
                att.rollspeed = (DATA[17][1]);
                att.yawspeed = (DATA[17][2]);

                TimeSpan timediff = DateTime.Now - oldtime;

                float pdiff = (float)((att.pitch - oldatt.pitch) / timediff.TotalSeconds);
                float rdiff = (float)((att.roll - oldatt.roll) / timediff.TotalSeconds);
                float ydiff = (float)((att.yaw - oldatt.yaw) / timediff.TotalSeconds);

                //Console.WriteLine("{0:0.00000} {1:0.00000} {2:0.00000} \t {3:0.00000} {4:0.00000} {5:0.00000}", pdiff, rdiff, ydiff, DATA[17][0], DATA[17][1], DATA[17][2]);

                oldatt = att;

                rdiff = DATA[17][1];
                pdiff = DATA[17][0];
                ydiff = DATA[17][2];

                Int16 xgyro = Constrain(rdiff * 1000.0, Int16.MinValue, Int16.MaxValue);
                Int16 ygyro = Constrain(pdiff * 1000.0, Int16.MinValue, Int16.MaxValue);
                Int16 zgyro = Constrain(ydiff * 1000.0, Int16.MinValue, Int16.MaxValue);

                oldtime = DateTime.Now;

                YLScsDrawing.Drawing3d.Vector3d accel3D = HIL.QuadCopter.RPY_to_XYZ(DATA[18][1], DATA[18][0], 0, -9.8); //DATA[18][2]

                float turnrad = (float)(((DATA[3][7] * 0.44704) * (DATA[3][7] * 0.44704) * 1.943844) / (float)(11.26 * Math.Tan(att.roll))) * ft2m;

                float centripaccel = (float)((DATA[3][7] * 0.44704) * (DATA[3][7] * 0.44704)) / turnrad;

                //Console.WriteLine("old {0} {1} {2}",accel3D.X,accel3D.Y,accel3D.Z);

                YLScsDrawing.Drawing3d.Vector3d cent3D = HIL.QuadCopter.RPY_to_XYZ(DATA[18][1] - 90, 0, 0, centripaccel);

                accel3D -= cent3D;

                //Console.WriteLine("new {0} {1} {2}", accel3D.X, accel3D.Y, accel3D.Z);

                oldax = DATA[4][5];
                olday = DATA[4][6];
                oldaz = DATA[4][4];

                double head = DATA[18][2] - 90;
#if MAVLINK10
                imu.time_usec = ((ulong)DateTime.Now.ToBinary());
				#else
				imu.usec = ((ulong)DateTime.Now.ToBinary());
				#endif
                imu.xgyro = xgyro; // roll - yes
                imu.xmag = (short)(Math.Sin(head * deg2rad) * 1000);
                imu.ygyro = ygyro; // pitch - yes
                imu.ymag = (short)(Math.Cos(head * deg2rad) * 1000);
                imu.zgyro = zgyro;
                imu.zmag = 0;

                imu.xacc = (Int16)(accel3D.X * 1000); // pitch
                imu.yacc = (Int16)(accel3D.Y * 1000); // roll
                imu.zacc = (Int16)(accel3D.Z * 1000);

                //Console.WriteLine("ax " + imu.xacc + " ay " + imu.yacc + " az " + imu.zacc);
#if MAVLINK10
                gps.alt = (int)(DATA[20][2] * ft2m * 1000);
                gps.fix_type = 3;
                gps.cog = (ushort)(DATA[19][2] * 100);
                gps.lat = (int)(DATA[20][0] * 1.0e7);
                gps.lon = (int)(DATA[20][1] * 1.0e7);
                gps.time_usec = ((ulong)0);
                gps.vel = (ushort)(DATA[3][7] * 0.44704 * 100);
#else
                gps.alt = ((float)(DATA[20][2] * ft2m));
                gps.fix_type = 3;
                gps.hdg = ((float)DATA[19][2]);
                gps.lat = ((float)DATA[20][0]);
                gps.lon = ((float)DATA[20][1]);
                gps.usec = ((ulong)0);
                gps.v = ((float)(DATA[3][7] * 0.44704));
#endif

                asp.airspeed = ((float)(DATA[3][6] * 0.44704));


            }
            else if (receviedbytes == 0x64) // FG binary udp
            {
                //FlightGear

                object imudata = new fgIMUData();

                MAVLink.ByteArrayToStructureEndian(data, ref imudata, 0);

                imudata = (fgIMUData)(imudata);



                fgIMUData imudata2 = (fgIMUData)imudata;

                if (imudata2.magic != 0x4c56414d)
                    return;

                if (imudata2.latitude == 0)
                    return;

                chkSensor.Checked = true;

#if MAVLINK10
                imu.time_usec = ((ulong)DateTime.Now.ToBinary());
				#else
				imu.usec = ((ulong)DateTime.Now.ToBinary());
				#endif

                imu.xacc = ((Int16)(imudata2.accelX * 9808 / 32.2));
                imu.xgyro = ((Int16)(imudata2.rateRoll * 17.453293));
                imu.xmag = 0;
                imu.yacc = ((Int16)(imudata2.accelY * 9808 / 32.2));
                imu.ygyro = ((Int16)(imudata2.ratePitch * 17.453293));
                imu.ymag = 0;
                imu.zacc = ((Int16)(imudata2.accelZ * 9808 / 32.2)); // + 1000
                imu.zgyro = ((Int16)(imudata2.rateYaw * 17.453293));
                imu.zmag = 0;
				
#if MAVLINK10
                gps.alt = ((int)(imudata2.altitude * ft2m * 1000));
                gps.fix_type = 3;
                gps.cog = (ushort)(Math.Atan2(imudata2.velocityE, imudata2.velocityN) * rad2deg * 100);
                gps.lat = (int)(imudata2.latitude * 1.0e7);
                gps.lon = (int)(imudata2.longitude * 1.0e7);
                gps.time_usec = ((ulong)DateTime.Now.Ticks);
                gps.vel = (ushort)(Math.Sqrt((imudata2.velocityN * imudata2.velocityN) + (imudata2.velocityE * imudata2.velocityE)) * ft2m * 100);
#else
                gps.alt = ((float)(imudata2.altitude * ft2m));
                gps.fix_type = 3;
                gps.hdg = ((float)Math.Atan2(imudata2.velocityE, imudata2.velocityN) * rad2deg);
                gps.lat = ((float)imudata2.latitude);
                gps.lon = ((float)imudata2.longitude);
                gps.usec = ((ulong)DateTime.Now.Ticks);
                gps.v = ((float)Math.Sqrt((imudata2.velocityN * imudata2.velocityN) + (imudata2.velocityE * imudata2.velocityE)) * ft2m);

#endif
                //FileStream stream = File.OpenWrite("fgdata.txt");
                //stream.Write(data, 0, receviedbytes);
                //stream.Close();
            }
            else if (receviedbytes == 582)
            {
                aeroin = new TDataFromAeroSimRC();

                object temp = aeroin;

                MAVLink.ByteArrayToStructure(data, ref temp, 0);

                aeroin = (TDataFromAeroSimRC)(temp);

                att.pitch = (aeroin.Model_fPitch);
                att.roll = (aeroin.Model_fRoll * -1);
                att.yaw = (float)((aeroin.Model_fHeading));
                att.pitchspeed = (aeroin.Model_fAngVelX);
                att.rollspeed = (aeroin.Model_fAngVelY);
                att.yawspeed = (aeroin.Model_fAngVelZ);


#if MAVLINK10
                imu.time_usec = ((ulong)DateTime.Now.ToBinary());
				#else
				imu.usec = ((ulong)DateTime.Now.ToBinary());
				#endif
                imu.xgyro = (short)(aeroin.Model_fAngVelX * 1000); // roll - yes
                //imu.xmag = (short)(Math.Sin(head * deg2rad) * 1000);
                imu.ygyro = (short)(aeroin.Model_fAngVelY * 1000); // pitch - yes
                //imu.ymag = (short)(Math.Cos(head * deg2rad) * 1000);
                imu.zgyro = (short)(aeroin.Model_fAngVelZ * 1000);
                //imu.zmag = 0;

                YLScsDrawing.Drawing3d.Vector3d accel3D = HIL.QuadCopter.RPY_to_XYZ(att.roll, att.pitch, 0, -9.8); //DATA[18][2]

                imu.xacc = (Int16)((accel3D.X + aeroin.Model_fAccelX) * 1000); // pitch
                imu.yacc = (Int16)((accel3D.Y + aeroin.Model_fAccelY) * 1000); // roll
                imu.zacc = (Int16)((accel3D.Z + aeroin.Model_fAccelZ) * 1000);

                Console.WriteLine("x {0} y {1} z {2}", imu.xacc, imu.yacc, imu.zacc);

#if MAVLINK10
                gps.alt = ((int)(aeroin.Model_fPosZ) * 1000);
                gps.fix_type = 3;
                gps.cog = (ushort)(Math.Atan2(aeroin.Model_fVelX, aeroin.Model_fVelY) * rad2deg * 100);
                gps.lat = (int)(aeroin.Model_fLatitude * 1.0e7);
                gps.lon = (int)(aeroin.Model_fLongitude * 1.0e7);
                gps.time_usec = ((ulong)DateTime.Now.Ticks);
                gps.vel = (ushort)(Math.Sqrt((aeroin.Model_fVelY * aeroin.Model_fVelY) + (aeroin.Model_fVelX * aeroin.Model_fVelX)) * 100);
#else 
                gps.alt = ((float)(aeroin.Model_fPosZ));
                gps.fix_type = 3;
                gps.hdg = ((float)Math.Atan2(aeroin.Model_fVelX, aeroin.Model_fVelY) * rad2deg);
                gps.lat = ((float)aeroin.Model_fLatitude);
                gps.lon = ((float)aeroin.Model_fLongitude);
                gps.usec = ((ulong)DateTime.Now.Ticks);
                gps.v = ((float)Math.Sqrt((aeroin.Model_fVelY * aeroin.Model_fVelY) + (aeroin.Model_fVelX * aeroin.Model_fVelX)));

#endif
                float xvec = aeroin.Model_fVelY - aeroin.Model_fWindVelY;
                float yvec = aeroin.Model_fVelX - aeroin.Model_fWindVelX;

                asp.airspeed = ((float)Math.Sqrt((yvec * yvec) + (xvec * xvec)));

            }
            else if (receviedbytes > 0x100)
            {

                FGNetFDM fdm = new FGNetFDM();

                object temp = fdm;

                MAVLink.ByteArrayToStructureEndian(data, ref temp, 0);

                fdm = (FGNetFDM)(temp);

                lastfdmdata = fdm;

                att.roll = fdm.phi;
                att.pitch = fdm.theta;
                att.yaw = fdm.psi;

#if MAVLINK10
                imu.time_usec = ((ulong)DateTime.Now.ToBinary());
				#else
				imu.usec = ((ulong)DateTime.Now.ToBinary());
				#endif
                imu.xgyro = (short)(fdm.phidot * 1150); // roll - yes
                //imu.xmag = (short)(Math.Sin(head * deg2rad) * 1000);
                imu.ygyro = (short)(fdm.thetadot * 1150); // pitch - yes
                //imu.ymag = (short)(Math.Cos(head * deg2rad) * 1000);
                imu.zgyro = (short)(fdm.psidot * 1150);
                imu.zmag = 0;

                imu.xacc = (Int16)Math.Min(Int16.MaxValue, Math.Max(Int16.MinValue, (fdm.A_X_pilot * 9808 / 32.2))); // pitch
                imu.yacc = (Int16)Math.Min(Int16.MaxValue, Math.Max(Int16.MinValue, (fdm.A_Y_pilot * 9808 / 32.2))); // roll
                imu.zacc = (Int16)Math.Min(Int16.MaxValue, Math.Max(Int16.MinValue, (fdm.A_Z_pilot / 32.2 * 9808)));

                //Console.WriteLine("ax " + imu.xacc + " ay " + imu.yacc + " az " + imu.zacc);
#if MAVLINK10
                gps.alt = ((int)(fdm.altitude * ft2m * 1000));
                gps.fix_type = 3;
                gps.cog = (ushort)((((Math.Atan2(fdm.v_east, fdm.v_north) * rad2deg) + 360) % 360) * 100);
                gps.lat = (int)(fdm.latitude * rad2deg * 1.0e7);
                gps.lon = (int)(fdm.longitude * rad2deg * 1.0e7);
                gps.time_usec = ((ulong)DateTime.Now.Ticks);
                gps.vel = (ushort)(Math.Sqrt((fdm.v_north * fdm.v_north) + (fdm.v_east * fdm.v_east)) * ft2m * 100);
#else
                gps.alt = ((float)(fdm.altitude * ft2m));
                gps.fix_type = 3;
                gps.hdg = (float)(((Math.Atan2(fdm.v_east, fdm.v_north) * rad2deg) + 360) % 360);
                //Console.WriteLine(gps.hdg);
                gps.lat = ((float)fdm.latitude * rad2deg);
                gps.lon = ((float)fdm.longitude * rad2deg);
                gps.usec = ((ulong)DateTime.Now.Ticks);
                gps.v = ((float)Math.Sqrt((fdm.v_north * fdm.v_north) + (fdm.v_east * fdm.v_east)) * ft2m);

#endif
                asp.airspeed = fdm.vcas * kts2fps * ft2m;
            }
            else
            {
                //FlightGear - old style udp

                DATA[20] = new float[8];

                DATA[18] = new float[8];

                DATA[19] = new float[8];

                DATA[3] = new float[8];

                // this text line is defined from ardupilot.xml
                string telem = Encoding.ASCII.GetString(data, 0, data.Length);

                try
                {
                    // should convert this to regex.... or just leave it.
                    int oldpos = 0;
                    int pos = telem.IndexOf(",");
                    DATA[20][0] = float.Parse(telem.Substring(oldpos, pos - 1), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf(",", pos + 1);
                    DATA[20][1] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf(",", pos + 1);
                    DATA[20][2] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf(",", pos + 1);
                    DATA[18][1] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf(",", pos + 1);
                    DATA[18][0] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf(",", pos + 1);
                    DATA[19][2] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));

                    oldpos = pos;
                    pos = telem.IndexOf("\n", pos + 1);
                    DATA[3][6] = float.Parse(telem.Substring(oldpos + 1, pos - 1 - oldpos), new System.Globalization.CultureInfo("en-US"));
                    DATA[3][7] = DATA[3][6];
                }
                catch (Exception) { }

                chkSensor.Checked = false;

                att.pitch = (DATA[18][0]);
                att.roll = (DATA[18][1]);
                att.yaw = (DATA[19][2]);
#if MAVLINK10
                gps.alt = ((int)(DATA[20][2] * ft2m * 1000));
                gps.fix_type = 3;
                gps.cog = (ushort)(DATA[18][2] * 100);
                gps.lat = (int)(DATA[20][0] * 1.0e7);
                gps.lon = (int)(DATA[20][1] * 1.0e7);
                gps.time_usec = ((ulong)0);
                gps.vel = (ushort)((DATA[3][7] * 0.44704 * 100));
#else
                gps.alt = ((float)(DATA[20][2] * ft2m));
                gps.fix_type = 3;
                gps.hdg = ((float)DATA[18][2]);
                gps.lat = ((float)DATA[20][0]);
                gps.lon = ((float)DATA[20][1]);
                gps.usec = ((ulong)0);
                gps.v = ((float)(DATA[3][7] * 0.44704));
#endif

                asp.airspeed = ((float)(DATA[3][6] * 0.44704));
            }

            // write arduimu to ardupilot
            if (CHK_quad.Checked) // quad does its own
            {
                return;
            }

#if MAVLINK10

            TimeSpan gpsspan = DateTime.Now - lastgpsupdate;

            if (gpsspan.TotalMilliseconds >= GPS_rate)
            {
                lastgpsupdate = DateTime.Now;
                oldgps = gps;
                //comPort.sendPacket(gps);
            }


            hilstate.alt = oldgps.alt;
            hilstate.lat = oldgps.lat;
            hilstate.lon = oldgps.lon;
            hilstate.pitch = att.pitch;
            hilstate.pitchspeed = att.pitchspeed;
            hilstate.roll = att.roll;
            hilstate.rollspeed = att.rollspeed;
            hilstate.time_usec = gps.time_usec;
            hilstate.vx = (short)(gps.vel * Math.Sin(oldgps.cog / 100.0 * deg2rad));
            hilstate.vy = (short)(gps.vel * Math.Cos(oldgps.cog / 100.0 * deg2rad));
            hilstate.vz = 0;
            hilstate.xacc = imu.xacc;
            hilstate.yacc = imu.yacc;
            hilstate.yaw = att.yaw;
            hilstate.yawspeed = att.yawspeed;
            hilstate.zacc = imu.zacc;

            comPort.sendPacket(hilstate);

            comPort.sendPacket(asp);
			
			#else
			
            if (chkSensor.Checked == false) // attitude
            {
                comPort.sendPacket( att);

                comPort.sendPacket( asp);
            }
            else // raw imu
            {
                // imudata

                comPort.sendPacket( imu);

			#endif

            MAVLink.__mavlink_raw_pressure_t pres = new MAVLink.__mavlink_raw_pressure_t();
            double calc = (101325 * Math.Pow(1 - 2.25577 * Math.Pow(10, -5) * gps.alt, 5.25588)); // updated from valid gps
            pres.press_diff1 = (short)(int)(calc - 101325); // 0 alt is 0 pa

            comPort.sendPacket(pres);
#if !MAVLINK10
                comPort.sendPacket(asp);
            }

            TimeSpan gpsspan = DateTime.Now - lastgpsupdate;

            if (gpsspan.TotalMilliseconds >= GPS_rate)
            {
                lastgpsupdate = DateTime.Now;

                comPort.sendPacket(gps);
            }
#endif
        }        

        HIL.QuadCopter quad = new HIL.QuadCopter();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lat">rads </param>
        /// <param name="lng">rads </param>
        /// <param name="alt">m</param>
        /// <param name="roll">rads</param>
        /// <param name="pitch">rads</param>
        /// <param name="heading">rads</param>
        /// <param name="yaw">rads</param>
        /// <param name="roll_out">-1 to 1</param>
        /// <param name="pitch_out">-1 to 1</param>
        /// <param name="rudder_out">-1 to 1</param>
        /// <param name="throttle_out">0 to 1</param>
        private void updateScreenDisplay(double lat,double lng,double alt,double roll,double pitch,double heading, double yaw,double roll_out,double pitch_out, double rudder_out, double throttle_out)
        {
            try
            {
                // Update Sim stuff
                this.Invoke((MethodInvoker)delegate
                {
                    TXT_servoroll.Text = roll_out.ToString("0.000");
                    TXT_servopitch.Text = pitch_out.ToString("0.000");
                    TXT_servorudder.Text = rudder_out.ToString("0.000");
                    TXT_servothrottle.Text = throttle_out.ToString("0.000");

                    TXT_lat.Text = (lat * rad2deg).ToString("0.00000");
                    TXT_long.Text = (lng * rad2deg).ToString("0.00000");
                    TXT_alt.Text = (alt).ToString("0.00");

                    TXT_roll.Text = (roll * rad2deg).ToString("0.000");
                    TXT_pitch.Text = (pitch * rad2deg).ToString("0.000");
                    TXT_heading.Text = (heading * rad2deg).ToString("0.000");
                    TXT_yaw.Text = (yaw * rad2deg).ToString("0.000");

                    TXT_wpdist.Text = MainV2.cs.wp_dist.ToString();
                    TXT_bererror.Text = MainV2.cs.ber_error.ToString();
                    TXT_alterror.Text = MainV2.cs.alt_error.ToString();
                    TXT_WP.Text = MainV2.cs.wpno.ToString();
                    TXT_control_mode.Text = MainV2.cs.mode;
                });
            }
            catch { this.Invoke((MethodInvoker)delegate { OutputLog.AppendText("NO SIM data - exep\n"); }); }
        }

        private void processArduPilot()
        {

            bool heli = CHK_heli.Checked;

            if (CHK_quad.Checked)
            {

                double[] m = new double[4];

                m[0] = (ushort)MainV2.cs.ch1out;
                m[1] = (ushort)MainV2.cs.ch2out;
                m[2] = (ushort)MainV2.cs.ch3out;
                m[3] = (ushort)MainV2.cs.ch4out;

                if (!RAD_softFlightGear.Checked)
                {
                    lastfdmdata.latitude = DATA[20][0] * deg2rad;
                    lastfdmdata.longitude = DATA[20][1] * deg2rad;
                    lastfdmdata.altitude = (DATA[20][2]);
                }

                try
                {

                    if (lastfdmdata.version == 0)
                        return;

                    quad.update(ref m, lastfdmdata);
                }
                catch (Exception e) { Console.WriteLine("Quad hill error " + e.ToString()); }

                byte[] FlightGear = new byte[8 * 11];// StructureToByteArray(fg);

                Array.Copy(BitConverter.GetBytes((double)(m[0])), 0, FlightGear, 0, 8);
                Array.Copy(BitConverter.GetBytes((double)(m[1])), 0, FlightGear, 8, 8);
                Array.Copy(BitConverter.GetBytes((double)(m[2])), 0, FlightGear, 16, 8);
                Array.Copy(BitConverter.GetBytes((double)(m[3])), 0, FlightGear, 24, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.latitude)), 0, FlightGear, 32, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.longitude)), 0, FlightGear, 40, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.altitude * 1 / ft2m)), 0, FlightGear, 48, 8);
                Array.Copy(BitConverter.GetBytes((double)((quad.altitude - quad.ground_level) * 1 / ft2m)), 0, FlightGear, 56, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.roll)), 0, FlightGear, 64, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.pitch)), 0, FlightGear, 72, 8);
                Array.Copy(BitConverter.GetBytes((double)(quad.yaw)), 0, FlightGear, 80, 8);

                if (RAD_softFlightGear.Checked)
                {

                    Array.Reverse(FlightGear, 0, 8);
                    Array.Reverse(FlightGear, 8, 8);
                    Array.Reverse(FlightGear, 16, 8);
                    Array.Reverse(FlightGear, 24, 8);
                    Array.Reverse(FlightGear, 32, 8);
                    Array.Reverse(FlightGear, 40, 8);
                    Array.Reverse(FlightGear, 48, 8);
                    Array.Reverse(FlightGear, 56, 8);
                    Array.Reverse(FlightGear, 64, 8);
                    Array.Reverse(FlightGear, 72, 8);
                    Array.Reverse(FlightGear, 80, 8);

                }

                try
                {
                    XplanesSEND.Send(FlightGear, FlightGear.Length);
                }
                catch (Exception) { Console.WriteLine("Socket Write failed, FG closed?"); }

                updateScreenDisplay(lastfdmdata.latitude,lastfdmdata.longitude,lastfdmdata.altitude * .3048,lastfdmdata.phi,lastfdmdata.theta,lastfdmdata.psi,lastfdmdata.psi,m[0],m[1],m[2],m[3]);

                return;

            }

            float roll_out, pitch_out, throttle_out, rudder_out, collective_out;

            collective_out = 0;

            if (heli)
            {
                roll_out = (float)MainV2.cs.hilch1 / rollgain;
                pitch_out = (float)MainV2.cs.hilch2 / pitchgain;
                throttle_out = 1;
                rudder_out = (float)MainV2.cs.hilch4 / -ruddergain;

                collective_out = (float)(MainV2.cs.hilch3 - 1500) / throttlegain;
            }
            else
            {

                roll_out = (float)MainV2.cs.hilch1 / rollgain;
                pitch_out = (float)MainV2.cs.hilch2 / pitchgain;
                throttle_out = ((float)MainV2.cs.hilch3 + 5000) / throttlegain;
                rudder_out = (float)MainV2.cs.hilch4 / ruddergain;
            }
            /*
            if ((roll_out == -1 || roll_out == 1) && (pitch_out == -1 || pitch_out == 1))
            {
                this.Invoke((MethodInvoker)delegate
                {
                    try
                    {
                        OutputLog.AppendText("Please check your radio setup - CLI -> setup -> radio!!!\n");
                    }
                    catch { }
                });
            }
            */
            // Limit min and max
            roll_out = Constrain(roll_out, -1, 1);
            pitch_out = Constrain(pitch_out, -1, 1);
            rudder_out = Constrain(rudder_out, -1, 1);
            throttle_out = Constrain(throttle_out, 0, 1);

            try
            {
                if (displayfull)
                {
                    // This updates the servo graphs
                    double time = (Environment.TickCount - tickStart) / 1000.0;

                    if (CHKgraphroll.Checked)
                    {
                        list.Add(time, roll_out);
                    }
                    else { list.Clear(); }
                    if (CHKgraphpitch.Checked)
                    {
                        list2.Add(time, pitch_out);
                    }
                    else { list2.Clear(); }
                    if (CHKgraphrudder.Checked)
                    {
                        list3.Add(time, rudder_out);
                    }
                    else { list3.Clear(); }
                    if (CHKgraphthrottle.Checked)
                    {
                        if (heli)
                        {
                            list4.Add(time, collective_out);
                        }
                        else
                        {
                            list4.Add(time, throttle_out);
                        }
                    }
                    else { list4.Clear(); }
                }

                if (packetssent % 10 == 0) // reduce cpu usage
                {
                    if (RAD_softXplanes.Checked)
                    {
                        updateScreenDisplay(DATA[20][0] * deg2rad, DATA[20][1] * deg2rad, DATA[20][2] * .3048, DATA[18][1] * deg2rad, DATA[18][0] * deg2rad, DATA[19][2] * deg2rad, DATA[18][2] * deg2rad, roll_out, pitch_out, rudder_out, throttle_out);
                    }

                    if (RAD_softFlightGear.Checked)
                    {
                        updateScreenDisplay(lastfdmdata.latitude, lastfdmdata.longitude, lastfdmdata.altitude * .3048, lastfdmdata.phi, lastfdmdata.theta, lastfdmdata.psi, lastfdmdata.psi, roll_out, pitch_out, rudder_out, throttle_out);
                    }

                    if (RAD_aerosimrc.Checked)
                    {
                        updateScreenDisplay(aeroin.Model_fLatitude * deg2rad, aeroin.Model_fLongitude * deg2rad, aeroin.Model_fPosZ, aeroin.Model_fRoll, aeroin.Model_fPitch, aeroin.Model_fHeading, aeroin.Model_fHeading, roll_out, pitch_out, rudder_out, throttle_out);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("Error updateing screen stuff " + e.ToString()); }


            packetssent++;

            if (RAD_aerosimrc.Checked)
            {
                //AeroSimRC
                byte[] AeroSimRC = new byte[4 * 8];// StructureToByteArray(fg);

                Array.Copy(BitConverter.GetBytes((double)(roll_out * REV_roll)), 0, AeroSimRC, 0, 8);
                Array.Copy(BitConverter.GetBytes((double)(pitch_out * REV_pitch * -1)), 0, AeroSimRC, 8, 8);
                Array.Copy(BitConverter.GetBytes((double)(rudder_out * REV_rudder)), 0, AeroSimRC, 16, 8);
                Array.Copy(BitConverter.GetBytes((double)((throttle_out *2) -1)), 0, AeroSimRC, 24, 8);

                if (heli)
                {
                    Array.Copy(BitConverter.GetBytes((double)(collective_out)), 0, AeroSimRC, 24, 8);
                }

                try
                {
                    SimulatorRECV.SendTo(AeroSimRC, Remote);
                }
                catch { }
            }

            // Flightgear

            if (RAD_softFlightGear.Checked)
            {
                //if (packetssent % 2 == 0) { return; } // short supply buffer.. seems to reduce lag

                byte[] FlightGear = new byte[4 * 8];// StructureToByteArray(fg);

                Array.Copy(BitConverter.GetBytes((double)(roll_out * REV_roll)), 0, FlightGear, 0, 8);
                Array.Copy(BitConverter.GetBytes((double)(pitch_out * REV_pitch * -1)), 0, FlightGear, 8, 8);
                Array.Copy(BitConverter.GetBytes((double)(rudder_out * REV_rudder)), 0, FlightGear, 16, 8);
                Array.Copy(BitConverter.GetBytes((double)(throttle_out)), 0, FlightGear, 24, 8);

                Array.Reverse(FlightGear, 0, 8);
                Array.Reverse(FlightGear, 8, 8);
                Array.Reverse(FlightGear, 16, 8);
                Array.Reverse(FlightGear, 24, 8);

                try
                {
                    XplanesSEND.Send(FlightGear, FlightGear.Length);
                }
                catch (Exception) { Console.WriteLine("Socket Write failed, FG closed?"); }

            }

            // Xplanes

            if (RAD_softXplanes.Checked)
            {
                // sending only 1 packet instead of many.

                byte[] Xplane = new byte[5 + 36 + 36];

                if (heli)
                {
                    Xplane = new byte[5 + 36 + 36 + 36];
                }

                Xplane[0] = (byte)'D';
                Xplane[1] = (byte)'A';
                Xplane[2] = (byte)'T';
                Xplane[3] = (byte)'A';
                Xplane[4] = 0;

                Array.Copy(BitConverter.GetBytes((int)25), 0, Xplane, 5, 4); // packet index

                Array.Copy(BitConverter.GetBytes((float)throttle_out), 0, Xplane, 9, 4); // start data
                Array.Copy(BitConverter.GetBytes((float)throttle_out), 0, Xplane, 13, 4);
                Array.Copy(BitConverter.GetBytes((float)throttle_out), 0, Xplane, 17, 4);
                Array.Copy(BitConverter.GetBytes((float)throttle_out), 0, Xplane, 21, 4);

                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 25, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 29, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 33, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 37, 4);

                // NEXT ONE - control surfaces

                Array.Copy(BitConverter.GetBytes((int)11), 0, Xplane, 41, 4); // packet index

                Array.Copy(BitConverter.GetBytes((float)(pitch_out * REV_pitch)), 0, Xplane, 45, 4); // start data
                Array.Copy(BitConverter.GetBytes((float)(roll_out * REV_roll)), 0, Xplane, 49, 4);
                Array.Copy(BitConverter.GetBytes((float)(rudder_out * REV_rudder)), 0, Xplane, 53, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 57, 4);

                Array.Copy(BitConverter.GetBytes((float)(roll_out * REV_roll * 5)), 0, Xplane, 61, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 65, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 69, 4);
                Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, 73, 4);

                if (heli)
                {
                    Array.Copy(BitConverter.GetBytes((float)(0)), 0, Xplane, 53, 4);


                    int a = 73 + 4;
                    Array.Copy(BitConverter.GetBytes((int)39), 0, Xplane, a, 4); // packet index
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((float)(12 * collective_out)), 0, Xplane, a, 4); // main rotor 0 - 12
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((float)(12 * rudder_out)), 0, Xplane, a, 4); // tail rotor -12 - 12
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                    a += 4;
                    Array.Copy(BitConverter.GetBytes((int)-999), 0, Xplane, a, 4);
                }

                try
                {
                    XplanesSEND.Send(Xplane, Xplane.Length);

                }
                catch (Exception e) { Console.WriteLine("Xplanes udp send error " + e.Message); }
            }
        }

        private void RAD_softXplanes_CheckedChanged(object sender, EventArgs e)
        {
            if (RAD_softXplanes.Checked && RAD_softFlightGear.Checked)
            {
                RAD_softFlightGear.Checked = false;
                RAD_aerosimrc.Checked = false;
            }
        }

        private void RAD_softFlightGear_CheckedChanged(object sender, EventArgs e)
        {
            if (RAD_softFlightGear.Checked && RAD_softXplanes.Checked)
            {
                RAD_softXplanes.Checked = false;
                RAD_aerosimrc.Checked = false;
            }
        }

        private void CHKREV_roll_CheckedChanged(object sender, EventArgs e)
        {
            if (CHKREV_roll.Checked)
            {
                REV_roll = -1;
            }
            else
            {
                REV_roll = 1;
            }
        }

        private void CHKREV_pitch_CheckedChanged(object sender, EventArgs e)
        {
            if (CHKREV_pitch.Checked)
            {
                REV_pitch = -1;
            }
            else
            {
                REV_pitch = 1;
            }
        }

        private void CHKREV_rudder_CheckedChanged(object sender, EventArgs e)
        {
            if (CHKREV_rudder.Checked)
            {
                REV_rudder = -1;
            }
            else
            {
                REV_rudder = 1;
            }
        }

        private void GPSrate_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                GPS_rate = int.Parse(GPSrate.Text); //GPSrate.SelectedItem.ToString());
            }
            catch { }
        }

        private void OutputLog_TextChanged(object sender, EventArgs e)
        {
            if (OutputLog.TextLength >= 10000)
            {
                OutputLog.Text = OutputLog.Text.Substring(OutputLog.TextLength / 2);
            }

            // auto scroll
            OutputLog.SelectionStart = OutputLog.Text.Length;

            OutputLog.ScrollToCaret();

            OutputLog.Refresh();

        }

        private float Constrain(float value, float min, float max)
        {
            if (value > max) { value = max; }
            if (value < min) { value = min; }
            return value;
        }

        private short Constrain(double value, double min, double max)
        {
            if (value > max) { value = max; }
            if (value < min) { value = min; }
            return (short)value;
        }


        public void CreateChart(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;

            // Set the titles and axis labels
            myPane.Title.Text = "Servo Output";
            myPane.XAxis.Title.Text = "Time";
            myPane.YAxis.Title.Text = "Output";

            LineItem myCurve;

            myCurve = myPane.AddCurve("Roll", list, Color.Red, SymbolType.None);

            myCurve = myPane.AddCurve("Pitch", list2, Color.Blue, SymbolType.None);

            myCurve = myPane.AddCurve("Rudder", list3, Color.Green, SymbolType.None);

            myCurve = myPane.AddCurve("Throttle", list4, Color.Orange, SymbolType.None);


            // Show the x axis grid
            myPane.XAxis.MajorGrid.IsVisible = true;

            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 5;

            // Make the Y axis scale red
            //myPane.YAxis.Scale.FontSpec.FontColor = Color.Red;
            //myPane.YAxis.Title.FontSpec.FontColor = Color.Red;
            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;
            // Don't display the Y zero line
            myPane.YAxis.MajorGrid.IsZeroLine = true;
            // Align the Y axis labels so they are flush to the axis
            myPane.YAxis.Scale.Align = AlignP.Inside;
            // Manually set the axis range
            //myPane.YAxis.Scale.Min = -1;
            //myPane.YAxis.Scale.Max = 1;

            // Fill the axis background with a gradient
            //myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            // Sample at 50ms intervals
            timer1.Interval = 50;
            timer1.Enabled = true;
            timer1.Start();


            // Calculate the Axis Scale Ranges
            zgc.AxisChange();

            tickStart = Environment.TickCount;


        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Make sure that the curvelist has at least one curve
            if (zg1.GraphPane.CurveList.Count <= 0)
                return;

            // Get the first CurveItem in the graph
            LineItem curve = zg1.GraphPane.CurveList[0] as LineItem;
            if (curve == null)
                return;

            // Get the PointPairList
            IPointListEdit list = curve.Points as IPointListEdit;
            // If this is null, it means the reference at curve.Points does not
            // support IPointListEdit, so we won't be able to modify it
            if (list == null)
                return;

            // Time is measured in seconds
            double time = (Environment.TickCount - tickStart) / 1000.0;

            // Keep the X scale at a rolling 30 second interval, with one
            // major step between the max X value and the end of the axis
            Scale xScale = zg1.GraphPane.XAxis.Scale;
            if (time > xScale.Max - xScale.MajorStep)
            {
                xScale.Max = time + xScale.MajorStep;
                xScale.Min = xScale.Max - 30.0;
            }
                // Make sure the Y axis is rescaled to accommodate actual data
                try
                {
                    zg1.AxisChange();
                }
                catch { }
                // Force a redraw
                zg1.Invalidate();
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            xmlconfig(true);
        }

        private void GPSrate_Leave(object sender, EventArgs e)
        {
            // user entered values
            GPSrate_SelectedIndexChanged(sender, e);
        }

        private void GPSrate_KeyDown(object sender, KeyEventArgs e)
        {
            // user entered values
            GPSrate_SelectedIndexChanged(sender, e);
        }

        private void but_advsettings_Click(object sender, EventArgs e)
        {
            InputBox("IP", "Enter Sim pc IP (def 127.0.0.1)", ref simIP);

            string temp = simPort.ToString();
            InputBox("Port", "Enter Sim pc Port (def 49000)", ref temp);
            simPort = int.Parse(temp);

            temp = recvPort.ToString();
            InputBox("Port", "Enter Planner pc Port (def 49005)", ref temp);
            recvPort = int.Parse(temp);

            xmlconfig(true);

            //Microsoft.VisualBasic.Interaction.InputBox("Enter Xplane pc IP", "IP", "127.0.0.1", -1, -1);
            //Microsoft.VisualBasic.Interaction.InputBox("Enter Xplane pc IP", "IP", "127.0.0.1", -1, -1);
        }

        //from http://www.csharp-examples.net/inputbox/
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            System.Windows.Forms.Label label = new System.Windows.Forms.Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                value = textBox.Text;
            }
            return dialogResult;
        }

        private void CHK_quad_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void BUT_startfgquad_Click(object sender, EventArgs e)
        {
            string extra = "";
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "fgfs|*fgfs*"
            };
            if (File.Exists(@"C:\Program Files (x86)\FlightGear\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files (x86)\FlightGear\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files (x86)\\FlightGear\\data\"";
            }
            else if (File.Exists(@"C:\Program Files\FlightGear\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files\FlightGear\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files\\FlightGear\\data\"";
            }
            else if (File.Exists(@"C:\Program Files\FlightGear 2.4.0\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files\FlightGear 2.4.0\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files\\FlightGear 2.4.0\\data\"";
            }
            else if (File.Exists(@"C:\Program Files (x86)\FlightGear 2.4.0\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files (x86)\FlightGear 2.4.0\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files (x86)\\FlightGear 2.4.0\\data\"";
            }
            else if (File.Exists(@"/usr/games/fgfs"))
            {
                ofd.InitialDirectory = @"/usr/games";
            }

            if (File.Exists(MainV2.getConfig("fgexe")) || ofd.ShowDialog() == DialogResult.OK)
            {
                if (ofd.FileName != "")
                {
                    MainV2.config["fgexe"] = ofd.FileName;
                }
                else
                {
                    ofd.FileName = MainV2.config["fgexe"].ToString();
                }

                System.Diagnostics.Process P = new System.Diagnostics.Process();
                P.StartInfo.FileName = ofd.FileName;
                P.StartInfo.Arguments = extra + @" --geometry=400x300      --aircraft=arducopter      --native-fdm=socket,out,50,127.0.0.1,49005,udp 	 --generic=socket,in,50,127.0.0.1,49000,udp,quadhil 	 --fdm=external 	   --roll=0       --pitch=0       --wind=0@0       --turbulence=0.0       --prop:/sim/frame-rate-throttle-hz111111=30       --timeofday=noon       --shading-flat       --fog-disable       --disable-specular-highlight       --disable-skyblend       --disable-random-objects       --disable-panel       --disable-horizon-effect       --disable-clouds       --disable-anti-alias-hud ";
                P.Start();
            }
        }

        private void BUT_startfgplane_Click(object sender, EventArgs e)
        {
            string extra = "";
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "fgfs|*fgfs*"
            };
            if (File.Exists(@"C:\Program Files (x86)\FlightGear\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files (x86)\FlightGear\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files (x86)\\FlightGear\\data\"";
            }
            else if (File.Exists(@"C:\Program Files\FlightGear\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files\FlightGear\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files\\FlightGear\\data\"";
            }
            else if (File.Exists(@"C:\Program Files\FlightGear 2.4.0\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files\FlightGear 2.4.0\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files\\FlightGear 2.4.0\\data\"";
            }
            else if (File.Exists(@"C:\Program Files (x86)\FlightGear 2.4.0\bin\Win32\fgfs.exe"))
            {
                ofd.InitialDirectory = @"C:\Program Files (x86)\FlightGear 2.4.0\bin\Win32\";
                extra = " --fg-root=\"C:\\Program Files (x86)\\FlightGear 2.4.0\\data\"";
            }
            else if (File.Exists(@"/usr/games/fgfs"))
            {
                ofd.InitialDirectory = @"/usr/games";
            }

            if (File.Exists(MainV2.getConfig("fgexe")) || ofd.ShowDialog() == DialogResult.OK)
            {
                if (ofd.FileName != "")
                {
                    MainV2.config["fgexe"] = ofd.FileName;
                }
                else
                {
                    ofd.FileName = MainV2.config["fgexe"].ToString();
                }

                System.Diagnostics.Process P = new System.Diagnostics.Process();
                P.StartInfo.FileName = ofd.FileName;
                P.StartInfo.Arguments = extra + @" --geometry=400x300         --native-fdm=socket,out,50,127.0.0.1,49005,udp 	--generic=socket,in,50,127.0.0.1,49000,udp,MAVLink		   --roll=0       --pitch=0       --wind=0@0       --turbulence=0.0       --prop:/sim/frame-rate-throttle-hz=30       --timeofday=noon       --shading-flat       --fog-disable       --disable-specular-highlight       --disable-skyblend       --disable-random-objects       --disable-panel       --disable-horizon-effect       --disable-clouds       --disable-anti-alias-hud ";
                P.Start();
            }
        }

        private void BUT_startxplane_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "X-Plane|*X-Plane*"
            };
            try
            {
                ofd.InitialDirectory = Path.GetDirectoryName(MainV2.config["xplaneexe"].ToString());
            }
            catch { }

            if (File.Exists(MainV2.getConfig("xplaneexe")) || ofd.ShowDialog() == DialogResult.OK)
            {
                if (ofd.FileName != "")
                {
                    MainV2.config["xplaneexe"] = ofd.FileName;
                }
                else
                {
                    ofd.FileName = MainV2.config["xplaneexe"].ToString();
                }

                System.Diagnostics.Process P = new System.Diagnostics.Process();
                P.StartInfo.FileName = ofd.FileName;
                P.StartInfo.Arguments = "";
                P.Start();
            }
        }

        private void TXT_rollgain_TextChanged(object sender, EventArgs e)
        {
            updateGains();
        }

        private void TXT_pitchgain_TextChanged(object sender, EventArgs e)
        {
            updateGains();
        }

        private void TXT_ruddergain_TextChanged(object sender, EventArgs e)
        {
            updateGains();
        }

        private void TXT_throttlegain_TextChanged(object sender, EventArgs e)
        {
            updateGains();
        }

        void updateGains()
        {
            try
            {
                rollgain = int.Parse(TXT_rollgain.Text);
                pitchgain = int.Parse(TXT_pitchgain.Text);
                ruddergain = int.Parse(TXT_ruddergain.Text);
                throttlegain = int.Parse(TXT_throttlegain.Text);
            }
            catch (Exception) { this.Invoke((MethodInvoker)delegate { OutputLog.AppendText("Bad Gains!!!\n"); }); }
        }

        private void CHKdisplayall_CheckedChanged(object sender, EventArgs e)
        {
            displayfull = CHKdisplayall.Checked;

            if (displayfull)
            {
                //this.Width = 651;
                timer1.Start();
                zg1.Visible = true;


                CHKgraphpitch.Visible = true;
                CHKgraphroll.Visible = true;
                CHKgraphrudder.Visible = true;
                CHKgraphthrottle.Visible = true;
            }
            else
            {
                //651, 457
                //this.Width = 651;
                //this.Height = 457;

                timer1.Stop();
                zg1.Visible = false;

                CHKgraphpitch.Visible = false;
                CHKgraphroll.Visible = false;
                CHKgraphrudder.Visible = false;
                CHKgraphthrottle.Visible = false;
            }
        }

        private void RAD_aerosimrc_CheckedChanged(object sender, EventArgs e)
        {
            if (RAD_aerosimrc.Checked && RAD_softXplanes.Checked)
            {
                RAD_softXplanes.Checked = false;
                RAD_softFlightGear.Checked = false;
            }
        }
    }
}