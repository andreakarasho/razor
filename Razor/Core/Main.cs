using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;

namespace Assistant
{

	public class Engine
	{
		private static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			if ( e.IsTerminating )
			{
				ClientCommunication.Close();
				m_Running = false;

				new MessageDialog( "Unhandled Exception", !e.IsTerminating, e.ExceptionObject.ToString() ).ShowDialog( Engine.ActiveWindow );
			}

			LogCrash( e.ExceptionObject as Exception );
		}

		public static void LogCrash( object exception )
		{
			if ( exception == null || ( exception is ThreadAbortException ) )
				return;

			using ( StreamWriter txt = new StreamWriter( "Crash.log", true ) )
			{
				txt.AutoFlush = true;
				txt.WriteLine( "Exception @ {0}", DateTime.Now.ToString( "MM-dd-yy HH:mm:ss.ffff" ) );
				txt.WriteLine( exception.ToString() );
				txt.WriteLine( "" );
				txt.WriteLine( "" );
			}
		}

		private static Version m_ClientVersion = null;

		public static Version ClientVersion
		{
			get
			{
				if ( m_ClientVersion == null || m_ClientVersion.Major < 2 )
				{
					string[] split = ClientCommunication.GetUOVersion().Split( '.' );

					if ( split.Length < 3 )
						return new Version( 4, 0, 0, 0 );

					int rev = 0;

					if ( split.Length > 3 )
						rev = Utility.ToInt32( split[3], 0 ) ;

					m_ClientVersion = new Version( 
						Utility.ToInt32( split[0], 0 ), 
						Utility.ToInt32( split[1], 0 ), 
						Utility.ToInt32( split[2], 0 ),
						rev );

					if ( m_ClientVersion.Major == 0 ) // sanity check if the client returns 0.0.0.0
						m_ClientVersion = new Version( 4, 0, 0, 0 );
				}

				return m_ClientVersion;
			}
		}

		public static bool UseNewMobileIncoming
		{
			get
			{
				if (ClientVersion.Major > 7)
				{
					return true;
				}
				else if (ClientVersion.Major == 7)
				{
					if (ClientVersion.Minor > 0 || ClientVersion.Build >= 33)
					{
						return true;
					}
				}

				return false;
			}
		}

		public static bool UsePostHSChanges {
			get {
				if ( ClientVersion.Major > 7 ) {
					return true;
				} else if ( ClientVersion.Major == 7 ) {
					if ( ClientVersion.Minor > 0 ) {
						return true;
					} else if ( ClientVersion.Build >= 9 ) {
						return true;
					}
				}

				return false;
			}
		}

		public static bool UsePostSAChanges
		{
			get
			{
				if (ClientVersion.Major >= 7)
				{
					return true;
				}

				return false;
			}
		}

		public static bool UsePostKRPackets 
		{
			get 
			{
				if ( ClientVersion.Major >= 7 )
				{
					return true;
				}
				else if ( ClientVersion.Major >= 6 )
				{
					if ( ClientVersion.Minor == 0 )
					{
						if ( ClientVersion.Build == 1 )
						{
							if ( ClientVersion.Revision >= 7 )
								return true;
						}
						else if ( ClientVersion.Build > 1 )
						{
							return true;
						}
					}
					else
					{
						return true;
					}
				}

				return false; 
			}
		}

		public static string ExePath{ get{ return Process.GetCurrentProcess().MainModule.FileName; } }
		public static Razor MainWindow{ get{ return m_MainWnd; } }
		public static bool Running{ get{ return m_Running; } }
		public static Form ActiveWindow{ get{ return m_ActiveWnd; } set{ m_ActiveWnd = value; } }
		
		public static string Version 
		{ 
			get
			{ 
				if ( m_Version == null )
				{
					Version v = Assembly.GetCallingAssembly().GetName().Version;
					m_Version = String.Format( "{0}.{1}.{2}", v.Major, v.Minor, v.Build );//, v.Revision
				}

				return m_Version; 
			}
		}

		private static Razor m_MainWnd;
		private static Form m_ActiveWnd;
		//private static Thread m_TimerThread;
		private static bool m_Running;
		private static string m_Version;

		[STAThread]
		public static void Main( string[] Args ) 
		{
			m_Running = true;
            Thread.CurrentThread.Name = "Razor Main Thread";

#if !DEBUG
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler( CurrentDomain_UnhandledException );
			Directory.SetCurrentDirectory( Config.GetInstallDirectory() );
#endif

            m_EngineOpts.Load();

            if ( ClientCommunication.InitializeLibrary( Engine.Version ) == 0 )
				throw new InvalidOperationException( "This Razor installation is corrupted." );

			if ( !Language.Load(m_EngineOpts.m_Language) )
			{
				MessageBox.Show( String.Format("Fatal Error: Unable to load required file Language/Razor_lang.{0}\nRazor cannot continue.", m_EngineOpts.m_Language), "No Language Pack", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				return;
			}

			Launcher launcher = new Launcher();
			m_ActiveWnd = launcher;
			if ( launcher.ShowDialog() == DialogResult.Cancel )
				return;
            m_EngineOpts.m_ClientEncryption = launcher.PatchEncryption;
            m_EngineOpts.m_ClientDir = launcher.Client;
            m_EngineOpts.m_DataDir = launcher.DataDirectory;

			if (dataDir != null && Directory.Exists(dataDir)) {
				Ultima.Files.SetMulPath(dataDir);
			}

			Language.LoadCliLoc();

			Initialize( typeof( Assistant.Engine ).Assembly ); //Assembly.GetExecutingAssembly()

			Config.LoadCharList();
            Config.LoadLastProfile();

            ClientCommunication.SetConnectionInfo(IPAddress.None, -1);

			ClientCommunication.Loader_Error result = ClientCommunication.Loader_Error.UNKNOWN_ERROR;

			if ( launch == ClientType.Classic )
				clientPath = Ultima.Files.GetFilePath("client.exe");
			else if ( launch == ClientType.ThirdDawn )
				clientPath = Ultima.Files.GetFilePath( "uotd.exe" );

			if ( clientPath != null && File.Exists( clientPath ) )
				result = ClientCommunication.LaunchClient( clientPath );

			if ( result != ClientCommunication.Loader_Error.SUCCESS )
			{
				if ( clientPath == null && File.Exists( clientPath ) )
					MessageBox.Show( String.Format( "Unable to find the client specified.\n{0}: \"{1}\"", launch.ToString(), clientPath != null ? clientPath : "-null-" ), "Could Not Start Client", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				else
					MessageBox.Show( String.Format( "Unable to launch the client specified. (Error: {2})\n{0}: \"{1}\"", launch.ToString(), clientPath != null ? clientPath : "-null-", result ), "Could Not Start Client", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				return;
			}

			string addr = Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "LastServer" );
			int port = Utility.ToInt32( Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "LastPort" ), 0 );

			// if these are null then the registry entry does not exist (old razor version)
			IPAddress ip = Resolve( addr );
			if ( ip == IPAddress.None || port == 0 )
			{
				MessageBox.Show( Language.GetString( LocString.BadServerAddr ), "Bad Server Address", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				return;
			}

			ClientCommunication.SetConnectionInfo( ip, port );

			Ultima.Multis.PostHSFormat = UsePostHSChanges;

			m_MainWnd = new Razor();
			Application.Run( m_MainWnd );
			
			m_Running = false;

			try { PacketPlayer.Stop(); } catch {}
			try { AVIRec.Stop(); } catch {}

			ClientCommunication.Close();
			Counter.Save();
			Macros.MacroManager.Save();
			Config.Save();
		}

		public static void EnsureDirectory( string dir )
		{
			if ( !Directory.Exists( dir ) )
				Directory.CreateDirectory( dir );
		}

		private static void Initialize( Assembly a )
		{
			Type[] types = a.GetTypes();

			for (int i=0;i<types.Length;i++)
			{
				MethodInfo init = types[i].GetMethod( "Initialize", BindingFlags.Static | BindingFlags.Public );

				if ( init != null )
					init.Invoke( null, null );
			}
		}

		private static IPAddress Resolve( string addr )
		{
			IPAddress ipAddr = IPAddress.None;

			if ( addr == null || addr == string.Empty )
				return ipAddr;

			try
			{
				ipAddr = IPAddress.Parse( addr );
			}
			catch
			{
				try
				{
					IPHostEntry iphe = Dns.GetHostEntry( addr );

					if ( iphe.AddressList.Length > 0 )
						ipAddr = iphe.AddressList[iphe.AddressList.Length - 1];
				}
				catch
				{
				}
			}

			return ipAddr;
		}

		public static bool IsElevated {
			get {
				return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
			}
		}
    }
}

