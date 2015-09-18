using UnityEngine;
using System.Collections;

namespace Phontroller
{
	public class PhontrollerDevice 
	{
		public int id = -1;
		public string deviceId;
		public string sessionId;
		public long sequenceNumber;
		public double deviceTime;

		float DisconnectDetectionTimeout = 15f;
		float firstUpdatedTime;
		float lastUpdatedTime;

		public delegate void ConnectedEvent();
		public event ConnectedEvent OnConnected;
		void FireConnected() { if (OnConnected != null) { Debug.Log ("OnConnected"); OnConnected();} }	

		public delegate void DisconnectedEvent();
		public event DisconnectedEvent OnDisconnected;
		void FireDisconnected() { if (OnDisconnected != null) { Debug.Log ("OnDisconnected"); OnDisconnected();} }	

		public PhontrollerDevice(int id)
		{
			this.id = id;
		}

		bool _Connected = false;
		public bool Connected
		{
			get
			{
				return _Connected;
			}
		}
		public float ConnectedDuration
		{
			get
			{
				if (_Connected) { return Time.realtimeSinceStartup - firstUpdatedTime; }  
				return lastUpdatedTime - firstUpdatedTime;
			}
		}


		Quaternion recenterRotation = Quaternion.identity;
		public void Recenter()
		{
			recenterRotation = Quaternion.Inverse(rotation);
		}

		Quaternion rotation = Quaternion.identity;
		public Quaternion GetLocalRotation 
		{
			get { return Quaternion.Euler(0, 270, 90) * recenterRotation * rotation; }
		}

		Vector3 acceleration = Vector3.zero;
		public Vector3 GetAcceleration 
		{
			get { return acceleration; }
		}

		bool _ButtonVolumeUpPressedDown = false;
		bool _ButtonVolumeUpPressed = false;
		bool _ButtonVolumeUpPressedUp = false;
		bool ButtonVolumeUpPressed 
		{
			get { return _ButtonVolumeUpPressed; }
			set 
			{ 
				_ButtonVolumeUpPressedDown = _ButtonVolumeUpPressedUp = false;
				if (value != _ButtonVolumeUpPressed) { _ButtonVolumeUpPressedDown = value;  _ButtonVolumeUpPressedUp = !value; _ButtonVolumeUpPressed = value; } 
			}
		}
		bool _ButtonVolumeDownPressedDown = false;
		bool _ButtonVolumeDownPressed = false;
		bool _ButtonVolumeDownPressedUp = false;
		bool ButtonVolumeDownPressed 
		{
			get { return _ButtonVolumeDownPressed; }
			set 
			{ 
				_ButtonVolumeDownPressedDown = _ButtonVolumeDownPressedUp = false;
				if (value != _ButtonVolumeDownPressed) { _ButtonVolumeDownPressedDown = value;  _ButtonVolumeDownPressedUp = !value; _ButtonVolumeDownPressed = value; } 
			}
		}

		public enum Button
		{
			ButtonVolumeUp,
			ButtonVolumeDown
		}

		public bool GetButtonDown(Button button)
		{
			if (button == Button.ButtonVolumeUp)
			{
				return _ButtonVolumeUpPressedDown;
			}
			if (button == Button.ButtonVolumeDown)
			{
				return _ButtonVolumeDownPressedDown;
			}
			return false;
		}

		public bool GetButtonUp(Button button)
		{
			if (button == Button.ButtonVolumeUp)
			{
				return _ButtonVolumeUpPressedUp;
			}
			if (button == Button.ButtonVolumeDown)
			{
				return _ButtonVolumeDownPressedUp;
			}
			return false;
		}

		float _TouchesMaxWidth = 0;
		public float TouchesMaxWidth { get { return _TouchesMaxWidth; } }
		float _TouchesMaxHeight = 0;
		public float TouchesMaxHeight { get { return _TouchesMaxHeight; } }
		public int maxTouches  { get { return _Touches.Length; } }
		int _TouchesCount = 0;
		public int TouchesCount  { get { return _TouchesCount; } }

		public class TouchPoint
		{
			public Vector2 previousPosition;
			public bool previousIsPressed;
			public Vector2 position;
			public bool isPressed;
		}
		TouchPoint[] _Touches = new TouchPoint[11];
		public TouchPoint[] Touches
		{
			get 
			{ 
				for (int i = 0; i < _Touches.Length; i++)
				{
					if (_Touches[i] == null)
					{
						_Touches[i] = new TouchPoint(){isPressed = false, position = new Vector2(), previousIsPressed = false, previousPosition = new Vector2()};
					}
				}
				return _Touches; 
			}
		}

		public void Update()
		{
			// handle a disconnection
			if (_Connected && (Time.realtimeSinceStartup - lastUpdatedTime) > DisconnectDetectionTimeout)
			{
				_Connected = false;
				FireDisconnected();

			}
		}

		public void SetData(PhontrollerService.PhontrollerDataPacket data)
		{
			deviceId = data.deviceId;

			// did the session restart?
			if (data.sessionId != sessionId)
			{
				sessionId = data.sessionId;
				sequenceNumber = -1;
			}

			// reject data if its an old packet
			if (data.sequenceNumber <= sequenceNumber)
			{
				return;
			}

			sequenceNumber = data.sequenceNumber;
			deviceTime = data.deviceTime;

			// rotation
			if (data.validRotation)
			{
				rotation = data.rotation;
			}

			// acceletation
			if (data.validAcceleration)
			{
				acceleration = data.acceleration;
			}

			// buttons
			if (data.validButtons)
			{
				ButtonVolumeUpPressed = data.buttonsVolumeUp;
				ButtonVolumeDownPressed = data.buttonsVolumeDown;
			}

			// touches
			if (data.validTouches)
			{
				for (int i = 0; i < data.Touches.Length && i < _Touches.Length; i++)
				{
					if (_Touches[i] == null)
					{
						_Touches[i] = new TouchPoint(){isPressed = false, position = new Vector2()};
					}
					_Touches[i].previousIsPressed = _Touches[i].isPressed;
					_Touches[i].previousPosition = _Touches[i].position;
					_Touches[i].isPressed = data.Touches[i].isPressed;
					_Touches[i].position = data.Touches[i].position;
				}
				_TouchesCount = data.touchesCount;
				_TouchesMaxWidth = data.TouchesMaxWidth;
				_TouchesMaxHeight = data.TouchesMaxHeight;			
			}

			// if we're now connected, fire the event
			lastUpdatedTime = Time.realtimeSinceStartup;
			if (_Connected == false)
			{
				firstUpdatedTime = lastUpdatedTime;
				_Connected = true;
				FireConnected();
			}


		}
	}
}