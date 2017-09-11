using System;
using System.Reflection;
using System.Threading;
using System.Collections;
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
		public static MainForm MainWindow{ get{ return m_MainWnd; } }
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

		private static MainForm m_MainWnd;
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

			if ( ClientCommunication.InitializeLibrary( Engine.Version ) == 0 )
				throw new InvalidOperationException( "This Razor installation is corrupted." );

			bool patch = Utility.ToInt32( Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "PatchEncy" ), 1 ) != 0;
			ClientLaunch launch = ClientLaunch.TwoD;
			string dataDir;

			ClientCommunication.ClientEncrypted = false;

			// check if the new ServerEncryption option is in the registry yet
			dataDir = Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "ServerEnc" );
			if ( dataDir == null )
			{
				// if not, add it (copied from UseOSIEnc)
				dataDir = Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "UseOSIEnc" );
				if ( dataDir == "1" )
				{
					ClientCommunication.ServerEncrypted = true;
					Config.SetRegString( Microsoft.Win32.Registry.CurrentUser, "ServerEnc", "1" );
				}
				else
				{
					Config.SetRegString( Microsoft.Win32.Registry.CurrentUser, "ServerEnc", "0" );
					ClientCommunication.ServerEncrypted = false;
				}

				Config.SetRegString( Microsoft.Win32.Registry.CurrentUser, "PatchEncy", "1" ); // reset the patch encryption option to TRUE
				patch = true;

				Config.DeleteRegValue( Microsoft.Win32.Registry.CurrentUser, "UseOSIEnc" ); // delete the old value
			}
			else
			{
				ClientCommunication.ServerEncrypted = Utility.ToInt32( dataDir, 0 ) != 0;
			}
			dataDir = null;

			if ( !Language.Load( "ENU" ) )
			{
				SplashScreen.End();
				MessageBox.Show( "Fatal Error: Unable to load required file Language/Razor_lang.enu\nRazor cannot continue.", "No Language Pack", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				return;
			}

			string defLang = Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "DefaultLanguage" );
			if ( defLang != null && !Language.Load( defLang ) )
				MessageBox.Show( String.Format( "WARNING: Razor was unable to load the file Language/Razor_lang.{0}\nENU will be used instead.", defLang ), "Language Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			
			string clientPath = "";

			SplashScreen.End();

			WelcomeForm welcome = new WelcomeForm();
			m_ActiveWnd = welcome;
			if ( welcome.ShowDialog() == DialogResult.Cancel )
				return;
			patch = welcome.PatchEncryption;
			launch = welcome.Client;
			dataDir = welcome.DataDirectory;
			if ( launch == ClientLaunch.Custom )
				clientPath = welcome.ClientPath;

			SplashScreen.Start();
			m_ActiveWnd = SplashScreen.Instance;

			if (dataDir != null && Directory.Exists(dataDir)) {
				Ultima.Files.SetMulPath(dataDir);
			}

			Language.LoadCliLoc();

			SplashScreen.Message = LocString.Initializing;

			Initialize( typeof( Assistant.Engine ).Assembly ); //Assembly.GetExecutingAssembly()

			SplashScreen.Message = LocString.LoadingLastProfile;
			Config.LoadCharList();
			if ( !Config.LoadLastProfile() )
				MessageBox.Show( SplashScreen.Instance, "The selected profile could not be loaded, using default instead.", "Profile Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );

            ClientCommunication.SetConnectionInfo(IPAddress.None, -1);

			ClientCommunication.Loader_Error result = ClientCommunication.Loader_Error.UNKNOWN_ERROR;

			SplashScreen.Message = LocString.LoadingClient;
				
			if ( launch == ClientLaunch.TwoD )
				clientPath = Ultima.Files.GetFilePath("client.exe");
			else if ( launch == ClientLaunch.ThirdDawn )
				clientPath = Ultima.Files.GetFilePath( "uotd.exe" );

			if ( clientPath != null && File.Exists( clientPath ) )
				result = ClientCommunication.LaunchClient( clientPath );

			if ( result != ClientCommunication.Loader_Error.SUCCESS )
			{
				if ( clientPath == null && File.Exists( clientPath ) )
					MessageBox.Show( SplashScreen.Instance, String.Format( "Unable to find the client specified.\n{0}: \"{1}\"", launch.ToString(), clientPath != null ? clientPath : "-null-" ), "Could Not Start Client", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				else
					MessageBox.Show( SplashScreen.Instance, String.Format( "Unable to launch the client specified. (Error: {2})\n{0}: \"{1}\"", launch.ToString(), clientPath != null ? clientPath : "-null-", result ), "Could Not Start Client", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				SplashScreen.End();
				return;
			}

			string addr = Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "LastServer" );
			int port = Utility.ToInt32( Config.GetRegString( Microsoft.Win32.Registry.CurrentUser, "LastPort" ), 0 );

			// if these are null then the registry entry does not exist (old razor version)
			IPAddress ip = Resolve( addr );
			if ( ip == IPAddress.None || port == 0 )
			{
				MessageBox.Show( SplashScreen.Instance, Language.GetString( LocString.BadServerAddr ), "Bad Server Address", MessageBoxButtons.OK, MessageBoxIcon.Stop );
				SplashScreen.End();
				return;
			}

			ClientCommunication.SetConnectionInfo( ip, port );

			Ultima.Multis.PostHSFormat = UsePostHSChanges;

			if ( Utility.Random(4) != 0 )
				SplashScreen.Message = LocString.WaitingForClient;
			else
				SplashScreen.Message = LocString.RememberDonate;

			m_MainWnd = new MainForm();
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

