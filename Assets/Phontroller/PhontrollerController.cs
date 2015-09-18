using UnityEngine;
using System.Collections;

namespace Phontroller
{
	public class PhontrollerController : MonoBehaviour 
	{
		public int deviceId = 0;

		public bool useFingers;
		public GameObject fingerPrefab;
		public Vector3 FingersScale = new Vector3(1f, 1f, 1f);
		Transform[] fingers;

		PhontrollerDevice device;
		public PhontrollerDevice phontrollerDevice { get { return device; } }

		#region MonoBehaviour

		void Start()
		{
			device = PhontrollerService.Instance.loadDevice(deviceId);
			device.OnConnected += HandleOnConnected;
			device.OnDisconnected += HandleOnDisconnected;;
			InitFingers();
		}

		void Update()
		{
			transform.localRotation = device.GetLocalRotation;

			UpdateFingers(device.Touches, device.TouchesMaxWidth, device.TouchesMaxHeight);
		}
		
		#endregion 

		// create gameobjects for our fingers - initially disabled
		void InitFingers()
		{
			if (fingerPrefab != null)
			{
				fingers = new Transform[device.maxTouches];
				for (int i = 0; i < fingers.Length; i++)
				{
					GameObject go = (GameObject)Instantiate(fingerPrefab, transform.position, transform.rotation);
					go.transform.SetParent(transform, false);
					go.SetActive(false);
					fingers[i] = go.transform;
				}
			}
		}
		
		// show/hide our finger objects based on touches
		void UpdateFingers(PhontrollerDevice.TouchPoint[] touches, float TouchesMaxWidth, float TouchesMaxHeight)
		{
			for (int i = 0; i < touches.Length; i++)
			{
				if (touches[i].isPressed)
				{
					fingers[i].gameObject.SetActive(true);
					// position it from 
					float x = touches[i].position.x / TouchesMaxWidth - 0.5f;
					float y = touches[i].position.y / TouchesMaxHeight - 0.5f;
					
					fingers[i].gameObject.transform.localPosition = Vector3.Scale(new Vector3(-x, y, -0.06f), FingersScale);
				}
				else
				{
					fingers[i].gameObject.SetActive(false);
				}
			}
		}

		void HandleOnConnected ()
		{
			Debug.Log ("Phontroller Connected");
			
			SendMessage("PhontrollerConnected", device, SendMessageOptions.DontRequireReceiver);
		}
		
		void HandleOnDisconnected ()
		{
			Debug.Log ("Phontroller Disconnected");
			
			SendMessage("PhontrollerDisonnected", device, SendMessageOptions.DontRequireReceiver);
		}
	}
}