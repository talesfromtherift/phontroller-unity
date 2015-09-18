using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Phontroller
{
	public class UdpServer : MonoBehaviour
	{
		#region Udp Server

		public int port = 3010;
		UdpClient server;
		Thread serverThread;
		string rawDataMsg;

		void Start()
		{
			StartUdpServer(port: port);
		}
		
		public void OnApplicationQuit()
		{
			// It is crucial in the editor that we stop the background thread when we exit play mode
			ShutdownUdpServer();
		}
		
		public virtual void Update()
		{
			ProcessNewUdpMessages();
		}
		
		void StartUdpServer(int port)
		{
			try
			{
				server = new UdpClient(port);
				Debug.Log ("Listening on: " + server.Client.LocalEndPoint.ToString());
				serverThread = new Thread(new ThreadStart(ThreadUdpServer));
				serverThread.Start();
			}
			catch (Exception e)
			{
				print(e.ToString());
			}
		}
		
		void ShutdownUdpServer()
		{
			if (server != null)
			{
				server.Close();
				serverThread.Abort();
			}
		}
		
		void ThreadUdpServer()
		{
			IPEndPoint receivingEndPoint = new IPEndPoint(IPAddress.Any, port);
			while (true)
			{
				try
				{
					// Wait for a udp message
					Byte[] recvMsg = server.Receive(ref receivingEndPoint);
					
					// Decode into a string
					string s = Encoding.ASCII.GetString(recvMsg);
					
					// Save the data message to be picked up on the main thread..
					rawDataMsg = s;
					
					// Don't overload the CPU
					Thread.Sleep(1);
				}
				catch (SocketException) 
				{
					// allow the thread to exit on a socket error
					return;
				}
			}
		}
		
		
		void ProcessNewUdpMessages()
		{
			if (rawDataMsg != null)
			{
				string s = rawDataMsg;
				rawDataMsg = null;
				OnUdpMessage(s);
			}
		}

		public virtual void OnUdpMessage(string s)
		{
		}
		
		#endregion
	}


	public class PhontrollerService : UdpServer
	{
		private static PhontrollerService _instance;
		static public PhontrollerService Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<PhontrollerService>();
				}
				return _instance;
			}
		}

		Dictionary<int, PhontrollerDevice> devices = new Dictionary<int, PhontrollerDevice>();

		public PhontrollerDevice loadDevice(int id)
		{
			if (!devices.ContainsKey(id))
			{
				devices[id] = new PhontrollerDevice(id);
			}
			return devices[id];
		}

		public override void Update()
		{
			try { base.Update(); } catch {}

			foreach (PhontrollerDevice device in devices.Values)
			{
				try { device.Update(); } catch {}
			}
		}

		#region UdpServer Data

		public class PhontrollerDataPacket 
		{
			public bool validInfo = false;
			public int id;
			public double deviceTime;
			public string deviceId;
			public string sessionId;
			public long sequenceNumber;

			public bool validRotation = false;
			public Quaternion rotation;

			public bool validAcceleration = false;
			public Vector3 acceleration;

			public bool validTouches = false;
			public int touchesCount = 0;
			public class TouchPoint
			{
				public Vector2 position;
				public bool isPressed;
			}
			public TouchPoint[] Touches = new TouchPoint[11];
			public float TouchesMaxWidth;
			public float TouchesMaxHeight;

			public bool validButtons = false;
			public bool buttonsVolumeUp;
			public bool buttonsVolumeDown;
		}


		public void OnPhontrollerDataPacket(PhontrollerDataPacket data)
		{
			// get the device
			PhontrollerDevice device = loadDevice(data.id);
			device.SetData(data);
		}

		public override void OnUdpMessage(string s)
		{
			PhontrollerDataPacket data = new PhontrollerDataPacket();

			// split into sub-messages (seperated by newline)
			String[] msgs = s.Split('\n');
			foreach (String nextmsg in msgs)
			{	
				string msg = nextmsg;
				
				// All messages start with "<method code>,<protocol version number>"
				//Debug.Log ("msg: " + msg);
				
				String[] words = msg.Split(':');
				if (words.Length > 0)
				{
					String[] parts = words[0].Split(',');
					if (parts.Length > 0)
					{
						string methodCode = parts[0];
						int methodVersion = 0;
						if (parts.Length > 1)
						{
							int.TryParse(parts[1], out methodVersion);
						}
						
						DecodeDataMessage(methodCode, methodVersion, words, ref data);
					}
				}
			}

			if (data.validInfo)
			{
				OnPhontrollerDataPacket(data);
			}
		}

		// Received a new message to decode
		void DecodeDataMessage(string methodCode, int methodVersion, String[] words, ref PhontrollerDataPacket data)
		{
			string[] parts;
			
			// info
			if (methodCode == "i")
			{
				if (words.Length > 1)
				{
					parts = words[1].Split(',');
					int id = Convert.ToInt16(parts[0]);
					double time = Convert.ToDouble(parts[1]);
					string deviceId = "";
					string sessionId = "";
					long sequenceNumber = -1;
					//	Debug.Log ("Time = " + time);
					if (parts.Length > 2)
					{
						deviceId = parts[2];
						sessionId = parts[3];
						sequenceNumber = Convert.ToInt64(parts[4]);
					}
					data.id = id;
					data.deviceTime = time;
					data.deviceId = deviceId;
					data.sessionId = sessionId;
					data.sequenceNumber = sequenceNumber;
					data.validInfo = true;
				}
			}
			// rotation
			else if (methodCode == "q")
			{
				if (words.Length > 1)
				{
					parts = words[1].Split(',');
					float x = Convert.ToSingle(parts[0]);
					float y = Convert.ToSingle(parts[1]);
					float z = Convert.ToSingle(parts[2]);
					float w = Convert.ToSingle(parts[3]);
					data.rotation = new Quaternion(x, y, z, w);
					data.validRotation = true;
				}
			}
			// accelerometer
			else if (methodCode == "a")
			{
				if (words.Length > 1)
				{
					parts = words[1].Split(',');
					if (parts.Length >= 3)
					{
						data.acceleration = new Vector3(Convert.ToSingle(parts[0]), Convert.ToSingle(parts[1]), Convert.ToSingle(parts[2]));
						data.validAcceleration = true;
					}
				}
			}
			// Touches
			else if (methodCode == "t")
			{
				if (words.Length > 1)
				{
					// update our fingers array
					for (int i = 0; i < data.Touches.Length && i < words.Length - 2; i++)
					{
						int word = i+2;

						if (words[word].Length == 0)
						{
							data.Touches[i] = new PhontrollerDataPacket.TouchPoint(){isPressed = false, position = Vector2.zero};
						}
						else
						{
							String[] values = words[word].Split(',');
							data.Touches[i] = new PhontrollerDataPacket.TouchPoint(){isPressed = true, position = new Vector2(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]))};
						}
					}
					
					String[] size = words[1].Split(',');
					float w = Convert.ToSingle(size[0]);
					float h = Convert.ToSingle(size[1]);
					data.TouchesMaxWidth = w;
					data.TouchesMaxHeight = h;
					data.validTouches = true;
				}
			}
			// buttons
			else if (methodCode == "b")
			{
				if (words.Length > 1)
				{
					parts = words[1].Split(',');
					if (parts.Length >= 2)
					{
						data.buttonsVolumeUp = parts[0] == "1";
						data.buttonsVolumeDown = parts[1] == "1";
						data.validButtons = true;
					}
				}
			}
		}
		
		#endregion

	}
}