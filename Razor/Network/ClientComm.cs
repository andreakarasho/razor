using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Net;
using Assistant.Core;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Assistant
{
	public unsafe sealed class ClientCommunication
	{
		public enum UONetMessage
		{
			Send = 1,
			Recv = 2,
			Ready = 3,
			NotReady = 4,
			Connect = 5,
			Disconnect = 6,
			KeyDown = 7,
			Mouse = 8,
			Activate = 9,
			Focus = 10,
			Close = 11,
			NotoHue = 13,
			DLL_Error = 14,
			SetGameSize = 19,
			FindData = 20,
			SmartCPU = 21,
			SetMapHWnd = 23
		}

		public enum UONetMessageCopyData
		{
			Position = 1,
		}

		public const int WM_USER = 0x400;

		public const int WM_COPYDATA = 0x4A;
		public const int WM_UONETEVENT = WM_USER+1;

		private enum InitError
		{
			SUCCESS,
			NO_UOWND,
			NO_TID,
			NO_HOOK,
			NO_SHAREMEM,
			LIB_DISABLED,
			NO_PATCH,
			NO_MEMCOPY,
			INVALID_PARAMS,

			UNKNOWN
		}

		private const int SHARED_BUFF_SIZE = 524288; // 262144; // 250k

		[StructLayout( LayoutKind.Explicit, Size=8+SHARED_BUFF_SIZE )]
		private struct Buffer
		{
			[FieldOffset( 0 )] public int Length;
			[FieldOffset( 4 )] public int Start;
			[FieldOffset( 8 )] public byte Buff0;
		}

		[DllImport( "Crypt.dll" )]
		private static unsafe extern int InstallLibrary(IntPtr razorWnd, IntPtr uoWnd, int flags);
		[DllImport("Crypt.dll")]
		private static unsafe extern void Shutdown();
		[DllImport( "Crypt.dll" )]
		private static unsafe extern IntPtr GetSharedAddress();
		[DllImport( "Crypt.dll" )]
		private static unsafe extern IntPtr GetCommMutex();
		[DllImport( "Crypt.dll" )]
		internal static unsafe extern uint TotalIn();
		[DllImport( "Crypt.dll" )]
		internal static unsafe extern uint TotalOut();
		[DllImport( "Crypt.dll" )]
		internal static unsafe extern void CalibratePosition( uint x, uint y, uint z, byte dir );
		[DllImport( "Crypt.dll" )]
		private static unsafe extern void SetServer( uint ip, ushort port );
		[DllImport( "Crypt.dll" )]
		internal static unsafe extern string GetUOVersion();

		public enum Loader_Error
		{
			SUCCESS = 0,
			NO_OPEN_EXE,
			NO_MAP_EXE,
			NO_READ_EXE_DATA,

			NO_RUN_EXE,
			NO_ALLOC_MEM,

			NO_WRITE,
			NO_VPROTECT,
			NO_READ,

			UNKNOWN_ERROR = 99
		};

		[DllImport( "Loader.dll" )]
		private static unsafe extern uint Load( string exe, string dll, string func, void *dllData, int dataLen, out uint pid );

		private static Queue<Packet> m_SendQueue = new Queue<Packet>();
		private static Queue<Packet> m_RecvQueue = new Queue<Packet>();

		private static bool m_QueueRecv;
		private static bool m_QueueSend;

		private static Buffer *m_InRecv;
		private static Buffer *m_OutRecv;
		private static Buffer *m_InSend;
		private static Buffer *m_OutSend;
		private static Mutex CommMutex;
		private static Process ClientProc;

		private static bool m_Ready = false;
		private static DateTime m_ConnStart;
		private static IPAddress m_LastConnection;

		public static DateTime ConnectionStart { get{ return m_ConnStart; } }
		public static IPAddress LastConnection{ get{ return m_LastConnection; } }
		public static Process ClientProcess{ get{ return ClientProc; } }

		public static bool ClientRunning
		{
			get
			{
				try
				{
					return ClientProc != null && !ClientProc.HasExited;
				}
				catch
				{
					return ClientProc != null && Windows.UOWindow != IntPtr.Zero;
				}
			}
		}

		/* TODO: Look into this */
		public static void SetMapWndHandle( Form mapWnd )
		{
			Windows.PostMessage(Windows.UOWindow, WM_UONETEVENT, (IntPtr)UONetMessage.SetMapHWnd, mapWnd.Handle );
		}

		public static void SetCustomNotoHue( int hue )
		{
			Windows.PostMessage(Windows.UOWindow, WM_UONETEVENT, (IntPtr)UONetMessage.NotoHue, (IntPtr)hue );
		}

		public static void SetSmartCPU(bool enabled)
		{
			if (enabled)
				try { ClientCommunication.ClientProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal; } catch { }

			Windows.PostMessage(Windows.UOWindow, WM_UONETEVENT, (IntPtr)UONetMessage.SmartCPU, (IntPtr)(enabled ? 1 : 0));
		}

		public static void SetGameSize( int x, int y )
		{
			Windows.PostMessage(Windows.UOWindow, WM_UONETEVENT, (IntPtr)UONetMessage.SetGameSize, (IntPtr)((x&0xFFFF)|((y&0xFFFF)<<16)) );
		}

		public static Loader_Error LaunchClient( string client )
		{
			string dll = Path.Combine( Config.GetInstallDirectory(), "Crypt.dll" );
			uint pid = 0;
			Loader_Error err = (Loader_Error)Load( client, dll, "OnAttach", null, 0, out pid );

			if ( err == Loader_Error.SUCCESS )
			{
				try
				{
					ClientProc = Process.GetProcessById( (int)pid );
				}
				catch
				{
				}
			}

			if ( ClientProc == null )
				return Loader_Error.UNKNOWN_ERROR;
			else
				return err;
		}

		internal static bool InstallHooks( IntPtr razorWindow )
		{
			InitError error;
			int flags = 0;

			Windows.FindUOWindow(ClientProc.Id);

			error = (InitError)InstallLibrary( razorWindow, Windows.UOWindow, flags );

			if ( error != InitError.SUCCESS )
			{
				FatalInit( error );
				return false;
			}

			byte *baseAddr = (byte*)GetSharedAddress().ToPointer();

			m_InRecv = (Buffer*)baseAddr;
			m_OutRecv = (Buffer*)(baseAddr+sizeof(Buffer));
			m_InSend = (Buffer*)(baseAddr+sizeof(Buffer)*2);
			m_OutSend = (Buffer*)(baseAddr+sizeof(Buffer)*3);

			SetServer( m_ServerIP, m_ServerPort );

			CommMutex = new Mutex();
#pragma warning disable 618
			CommMutex.Handle = GetCommMutex();
#pragma warning restore 618

			// TODO: Move this out.
			try
			{
				string path = Ultima.Files.GetFilePath("art.mul");
				if (path != null && path != string.Empty)
				{
					Windows.InitTitleBar(Path.GetDirectoryName(path));
				}
				else
				{
					Windows.InitTitleBar(Ultima.Files.Directory);
				}
			}
			catch
			{
				Windows.InitTitleBar("");
			}

			return true;
		}

		private static uint m_ServerIP;
		private static ushort m_ServerPort;

		internal static void SetConnectionInfo( IPAddress addr, int port )
		{
#pragma warning disable 618
			m_ServerIP = (uint)addr.Address;
#pragma warning restore 618
			m_ServerPort = (ushort)port;
		}

		public static bool Attach( int pid )
		{
			ClientProc = null;
			ClientProc = Process.GetProcessById( pid );
			return ClientProc != null;
		}

		public static void Close()
		{
			Windows.FreeTitleBar();
			Shutdown();
			if ( ClientProc != null && !ClientProc.HasExited )
				ClientProc.CloseMainWindow();
			ClientProc = null;
		}

		private static void FatalInit( InitError error )
		{
			StringBuilder sb = new StringBuilder( Language.GetString( LocString.InitError ) );
			sb.AppendFormat( "{0}\n", error );
			sb.Append( Language.GetString( (int)(LocString.InitError + (int)error) ) );

			MessageBox.Show( Engine.ActiveWindow, sb.ToString(), "Init Error", MessageBoxButtons.OK, MessageBoxIcon.Stop );
		}

		public static void OnLogout()
		{
			OnLogout( true );
		}

		private static void OnLogout( bool fake )
		{
			if ( !fake )
			{
				PacketHandlers.Party.Clear();

				Windows.SetTitleStr( "" );
				Engine.MainWindow.UpdateTitle();
				UOAssist.PostLogout();
				m_ConnStart = DateTime.MinValue;
			}

			World.Player = null;
			World.Items.Clear();
			World.Mobiles.Clear();
			Macros.MacroManager.Stop();
			ActionQueue.Stop();
			Counter.Reset();
			   GoldPerHourTimer.Stop();
			   BandageTimer.Stop();
			   GateTimer.Stop();
			   BuffsTimer.Stop();
			StealthSteps.Unhide();
			Engine.MainWindow.OnLogout();
			if( Engine.MainWindow.MapWindow != null )
				Engine.MainWindow.MapWindow.Close();
			PacketHandlers.Party.Clear();
			PacketHandlers.IgnoreGumps.Clear();
			Config.Save();
		}

		internal static bool OnMessage( MainForm razor, uint wParam, int lParam )
		{
			bool retVal = true;

			switch ( (UONetMessage)(wParam&0xFFFF) )
			{
				case UONetMessage.Ready: //Patch status
					if ( lParam == (int)InitError.NO_MEMCOPY )
					{
						if ( MessageBox.Show( Engine.ActiveWindow, Language.GetString( LocString.NoMemCpy ), "No Client MemCopy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning ) == DialogResult.No )
						{
							m_Ready = false;
							ClientProc = null;
							Engine.MainWindow.CanClose = true;
							Engine.MainWindow.Close();
							break;
						}
					}

					m_Ready = true;
					break;

				case UONetMessage.NotReady:
					m_Ready = false;
					FatalInit( (InitError)lParam );
					ClientProc = null;
					Engine.MainWindow.CanClose = true;
					Engine.MainWindow.Close();
					break;

					// Network events
				case UONetMessage.Recv:
					OnRecv();
					break;
				case UONetMessage.Send:
					OnSend();
					break;
				case UONetMessage.Connect:
					m_ConnStart = DateTime.UtcNow;
					try
					{
						m_LastConnection = new IPAddress( (uint)lParam );
					}
					catch
					{
					}
					break;
				case UONetMessage.Disconnect:
					OnLogout( false );
					break;
				case UONetMessage.Close:
					OnLogout();
					ClientProc = null;
					Engine.MainWindow.CanClose = true;
					Engine.MainWindow.Close();
					break;

				// Hot Keys
				case UONetMessage.Mouse:
					HotKey.OnMouse( (ushort)(lParam&0xFFFF), (short)(lParam>>16) );
					break;
				case UONetMessage.KeyDown:
					retVal = HotKey.OnKeyDown( lParam );
					break;

				// Activation Tracking
				case UONetMessage.Activate:
					break;

				case UONetMessage.Focus:
					if ( Config.GetBool( "AlwaysOnTop" ) )
					{
						if ( lParam != 0 && !razor.TopMost )
						{
							razor.TopMost = true;
							Windows.SetForegroundWindow( Windows.UOWindow );
						}
						else if ( lParam == 0 && razor.TopMost )
						{
							razor.TopMost = false;
							razor.SendToBack();
						}
					}

					// always use smartness for the map window
					if ( razor.MapWindow != null && razor.MapWindow.Visible )
					{
						if ( lParam != 0 && !razor.MapWindow.TopMost )
						{
							razor.MapWindow.TopMost = true;
							Windows.SetForegroundWindow( Windows.UOWindow );
						}
						else if ( lParam == 0 && razor.MapWindow.TopMost )
						{
							razor.MapWindow.TopMost = false;
							razor.MapWindow.SendToBack();
						}
					}

					break;

				case UONetMessage.DLL_Error:
				{
					string error = "Unknown";
					MessageBox.Show( Engine.ActiveWindow, "An Error has occured : \n" + error, "Error Reported", MessageBoxButtons.OK, MessageBoxIcon.Warning );
					break;
				}

				case UONetMessage.FindData:
					FindData.Message( (wParam&0xFFFF0000)>>16, lParam );
					break;

				// Unknown
				default:
					MessageBox.Show( Engine.ActiveWindow, "Unknown message from uo client\n" + ((int)wParam).ToString(), "Error?" );
					break;
			}

			return retVal;
		}

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct CopyData
		{
			public int dwData;
			public int cbDAta;
			public IntPtr lpData;
		};

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct Position
		{
			public ushort x;
			public ushort y;
			public ushort z;
		};

		internal static unsafe bool OnCopyData(IntPtr wparam, IntPtr lparam)
		{
			CopyData copydata = (CopyData)Marshal.PtrToStructure(lparam, typeof(CopyData));

			switch ((UONetMessageCopyData)copydata.dwData)
			{
				case UONetMessageCopyData.Position:
					if (World.Player != null)
					{
						Position pos = (Position)Marshal.PtrToStructure(copydata.lpData, typeof(Position));
						Point3D pt = new Point3D();

						pt.X = pos.x;
						pt.Y = pos.y;
						pt.Z = pos.z;

						World.Player.Position = pt;
					}
					return true;
			}

			return false;
		}

		internal static void SendToServer( Packet p )
		{
			if ( !m_Ready )
				return;

			if ( !m_QueueSend )
			{
				ForceSendToServer( p );
			}
			else
			{
				m_SendQueue.Enqueue( p );
			}
		}

		internal static void SendToServer( PacketReader pr )
		{
			if ( !m_Ready )
				return;

			SendToServer( MakePacketFrom( pr ) );
		}

		internal static void SendToClient( Packet p )
		{
			if ( !m_Ready || p.Length <= 0 )
				return;

			if ( !m_QueueRecv )
			{
				ForceSendToClient( p );
			}
			else
			{
				m_RecvQueue.Enqueue( p );
			}
		}

		internal static void SendToClient( PacketReader pr )
		{
			if ( !m_Ready )
				return;

			SendToClient( MakePacketFrom( pr ) );
		}

		internal static void ForceSendToClient( Packet p )
		{
			byte[] data = p.Compile();

			CommMutex.WaitOne();
			fixed ( byte *ptr = data )
			{
				Packet.Log( PacketPath.RazorToClient, ptr, data.Length );
				CopyToBuffer( m_OutRecv, ptr, data.Length );
			}
			CommMutex.ReleaseMutex();
		}

		internal static void ForceSendToServer( Packet p )
		{
			if ( p == null || p.Length <= 0 )
				return;

			byte[] data = p.Compile();

			CommMutex.WaitOne();
			InitSendFlush();
			fixed ( byte *ptr = data )
			{
				Packet.Log( PacketPath.RazorToServer, ptr, data.Length );
				CopyToBuffer( m_OutSend, ptr, data.Length );
			}
			CommMutex.ReleaseMutex();
		}

		private static void InitSendFlush()
		{
			if ( m_OutSend->Length == 0 )
				Windows.PostMessage( Windows.UOWindow, WM_UONETEVENT, (IntPtr)UONetMessage.Send, IntPtr.Zero );
		}

		private static void CopyToBuffer( Buffer *buffer, byte *data, int len )
		{
			Windows.memcpy( (&buffer->Buff0) + buffer->Start + buffer->Length, data, len );
			buffer->Length += len;
		}

		internal static Packet MakePacketFrom( PacketReader pr )
		{
			byte[] data = pr.CopyBytes( 0, pr.Length );
			return new Packet( data, pr.Length, pr.DynamicLength );
		}

		private static void HandleComm( Buffer *inBuff, Buffer *outBuff, Queue<Packet> queue, PacketPath path )
		{
			CommMutex.WaitOne();
			while ( inBuff->Length > 0 )
			{
				byte *buff = (&inBuff->Buff0) + inBuff->Start;

				int len = PacketsTable.GetPacketLength( buff[0] );
				if ( len > inBuff->Length || len <= 0 )
					break;

				inBuff->Start += len;
				inBuff->Length -= len;

				bool viewer = false;
				bool filter = false;

				switch ( path )
				{
					case PacketPath.ClientToServer:
						viewer = PacketHandler.HasClientViewer( buff[0] );
						filter = PacketHandler.HasClientFilter( buff[0] );
						break;
					case PacketPath.ServerToClient:
						viewer = PacketHandler.HasServerViewer( buff[0] );
						filter = PacketHandler.HasServerFilter( buff[0] );
						break;
				}

				Packet p = null;
				PacketReader pr = null;
				if ( viewer )
				{
					pr = new PacketReader( buff, len, PacketsTable.GetPacketLength(buff[0]) == -1 );
					if ( filter )
						p = MakePacketFrom( pr );
				}
				else if ( filter )
				{
					byte[] temp = new byte[len];
					fixed ( byte *ptr = temp )
						Windows.memcpy( ptr, buff, len );
					p = new Packet( temp, len, PacketsTable.GetPacketLength(buff[0]) == -1);
				}

				bool blocked = false;
				switch ( path )
				{
					// yes it should be this way
					case PacketPath.ClientToServer:
					{
						blocked = PacketHandler.OnClientPacket( buff[0], pr, p );
						break;
					}
					case PacketPath.ServerToClient:
					{
						blocked = PacketHandler.OnServerPacket( buff[0], pr, p );
						break;
					}
				}

				if ( filter )
				{
					byte[] data = p.Compile();
					fixed ( byte *ptr = data )
					{
						Packet.Log( path, ptr, data.Length, blocked );
						if ( !blocked )
							CopyToBuffer( outBuff, ptr, data.Length );
					}
				}
				else
				{
					Packet.Log( path, buff, len, blocked );
					if ( !blocked )
						CopyToBuffer( outBuff, buff, len );
				}

				while ( queue.Count > 0 )
				{
					p = (Packet)queue.Dequeue();

					byte[] data = p.Compile();
					fixed ( byte *ptr = data )
					{
						CopyToBuffer( outBuff, ptr, data.Length );
						Packet.Log( (PacketPath)(((int)path)+1), ptr, data.Length );
					}
				}
			}
			CommMutex.ReleaseMutex();
		}

		private static void OnRecv()
		{
			m_QueueRecv = true;
			HandleComm( m_InRecv, m_OutRecv, m_RecvQueue, PacketPath.ServerToClient );
			m_QueueRecv = false;
		}

		private static void OnSend()
		{
			m_QueueSend = true;
			HandleComm( m_InSend, m_OutSend, m_SendQueue, PacketPath.ClientToServer );
			m_QueueSend = false;
		}
	}

}

