using UnityEngine;
using System.Collections;
using Phontroller;

[RequireComponent(typeof(PhontrollerController))]
public class PhontrollerDemo : MonoBehaviour 
{
	public GameObject IntroMessage;
	public GameObject RecenterMessage;
	PhontrollerController phontrollerController;

	#region MonoBehaviour

	void Start()
	{
		phontrollerController = GetComponent<PhontrollerController>();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.C) || phontrollerController.phontrollerDevice.GetButtonDown(PhontrollerDevice.Button.ButtonVolumeUp))
		{
			Debug.Log ("Recenter!");
			IntroMessage.SetActive(false);
			RecenterMessage.SetActive(false);

			phontrollerController.phontrollerDevice.Recenter();
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}

	#endregion


	#region PhontrollerController

	void PhontrollerConnected(PhontrollerDevice device)
	{
		IntroMessage.SetActive(false);
		RecenterMessage.SetActive(true);
	}
	
	void PhontrollerDisonnected(PhontrollerDevice device)
	{
		IntroMessage.SetActive(true);
		RecenterMessage.SetActive(false);
	}

	#endregion

}
