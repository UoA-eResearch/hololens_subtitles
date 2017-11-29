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
	public GameObject menu;
	public GameObject cursor;
	private bool rotateText = true;
	public AudioSource tapSound;
	private float lastChange = 0;
	public float colorInterval = .5f;

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
		string message = reader.ReadToEnd();
		Debug.Log("MESSAGE: " + message);
		text = message;
	}
#endif
	private void Update()
	{
		target.text = text;

		foreach (Text child in menu.GetComponentsInChildren<Text>())
		{
			if (Time.time - lastChange > colorInterval) {
				child.color = Color.white;
			}
		}

		var c = Camera.main.transform;
		var t = target.transform.parent.transform;
		if (rotateText)
		{
			t.localRotation = c.localRotation;
		}
		RaycastHit hit;
		Physics.Raycast(c.position, c.forward, out hit);
		if (hit.distance > 0)
		{
			if (!manuallyPositioned)
			{
				t.localPosition = new Vector3(c.localPosition.x, c.localPosition.y - .1f, c.localPosition.z);
				t.localPosition += c.forward * hit.distance;
				t.localScale = Vector3.one * .0005f * hit.distance;
			}
			var go = hit.collider.gameObject;
			if (Time.time - lastChange > colorInterval)
			{
				go.GetComponentInChildren<Text>().color = Color.yellow;
			}
		}
	}

	private void Awake()
	{
		ManipulationRecognizer = new GestureRecognizer();
		ManipulationRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.ManipulationTranslate);
		ManipulationRecognizer.TappedEvent += ManipulationRecognizer_TappedEvent;
		ManipulationRecognizer.ManipulationUpdatedEvent += ManipulationRecognizer_ManipulationUpdatedEvent;
		ManipulationRecognizer.ManipulationCompletedEvent += ManipulationRecognizer_ManipulationCompletedEvent;
		ManipulationRecognizer.ManipulationCanceledEvent += ManipulationRecognizer_ManipulationCompletedEvent;
		ManipulationRecognizer.StartCapturingGestures();
	}

	private void ManipulationRecognizer_TappedEvent(InteractionSourceKind source, int tapCount, Ray ray)
	{
		Debug.Log("tap");
		tapSound.Play();
		var c = Camera.main.transform;
		RaycastHit hit;
		string go = "";
		if (Physics.Raycast(c.position, c.forward, out hit))
		{
			go = hit.collider.gameObject.name;
			try
			{
				hit.collider.gameObject.GetComponentInChildren<Text>().color = Color.green;
				lastChange = Time.time;
			}
			catch
			{

			}
		}
		var d = Mathf.Clamp(hit.distance, .5f, 3);
		Debug.Log(go);
		if (go == "sizeplus")
		{
			target.transform.localScale *= 1.1f;
		}
		else if (go == "sizeminus")
		{
			target.transform.localScale *= .9f;
		}
		else if (go == "reset")
		{
			manuallyPositioned = false;
			menu.SetActive(false);
			cursor.SetActive(false);
		} else if (go == "toggle_rotation")
		{
			rotateText = !rotateText;
		}
		else
		{
			if (menu.activeInHierarchy)
			{
				menu.SetActive(false);
				cursor.SetActive(false);
			} else
			{
				menu.SetActive(true);
				cursor.SetActive(true);
				cursor.transform.localPosition = new Vector3(cursor.transform.localPosition.x, cursor.transform.localPosition.y, d - .1f);
				menu.transform.position = c.position;
				menu.transform.rotation = c.rotation;
				menu.transform.position += c.forward * (d - .1f);
			}
		}
	}

	private void ManipulationRecognizer_ManipulationUpdatedEvent(InteractionSourceKind source, Vector3 position, Ray ray)
	{
		tapSound.Play();
		var t = target.transform.parent.transform;
		var moveVector = position - manipulationPreviousPosition;
		manipulationPreviousPosition = position;
		t.localPosition += moveVector * 2;
		manuallyPositioned = true;
	}

	private void ManipulationRecognizer_ManipulationCompletedEvent(InteractionSourceKind source, Vector3 position, Ray ray)
	{
		manipulationPreviousPosition = Vector3.zero;
	}
}
