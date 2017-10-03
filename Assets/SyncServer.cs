using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.Input;

#if WINDOWS_UWP
using Windows.Networking.Sockets;
#endif

public class SyncServer : MonoBehaviour
{
	public Text target;
	private string text = "Waiting for server...";
	private GestureRecognizer ManipulationRecognizer;
	private Vector3 manipulationPreviousPosition = Vector3.zero;
	private bool manuallyPositioned = false;

#if WINDOWS_UWP
	private DatagramSocket socket;
	
	async void Start()
	{
		socket = new DatagramSocket();
		socket.MessageReceived += Socket_MessageReceived;
		try
		{
			await socket.BindEndpointAsync(null, "15000");
		}
		catch (Exception e)
		{
			Debug.Log(e.ToString());
			Debug.Log(SocketError.GetStatus(e.HResult).ToString());
			return;
		}
	}
	
	private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
		Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
	{
		//Read the message that was received from the UDP echo client.
		Stream streamIn = args.GetDataStream().AsStreamForRead();
		StreamReader reader = new StreamReader(streamIn);
		string message = await reader.ReadLineAsync();
		Debug.Log("MESSAGE: " + message);
		text = message;
	}
#endif
	private void Update()
	{
		target.text = text;
		var c = Camera.main.transform;
		var t = target.transform.parent.transform;
		t.localRotation = c.localRotation;
		if (manuallyPositioned) return;
		RaycastHit hit;
		Physics.Raycast(c.position, c.forward, out hit);
		if (hit.distance > 0)
		{
			t.localPosition = c.localPosition;
			t.localPosition += c.forward * hit.distance;
			t.localScale = Vector3.one * .0005f * hit.distance;
		}
	}

	private void Awake()
	{
		ManipulationRecognizer = new GestureRecognizer();
		ManipulationRecognizer.SetRecognizableGestures(GestureSettings.ManipulationTranslate);
		ManipulationRecognizer.ManipulationUpdatedEvent += ManipulationRecognizer_ManipulationUpdatedEvent;
		ManipulationRecognizer.StartCapturingGestures();
	}

	private void ManipulationRecognizer_ManipulationUpdatedEvent(InteractionSourceKind source, Vector3 position, Ray ray)
	{
		var t = target.transform.parent.transform;
		var moveVector = position - manipulationPreviousPosition;
		manipulationPreviousPosition = position;
		t.localPosition += moveVector * 2;
		manuallyPositioned = true;
	}
}
