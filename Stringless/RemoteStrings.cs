/// <summary>
/// Remote Strings is a Classe for Stringless to support RemoteUI
/// Version 0.01
/// This is just a prototype and it's not been tested yet
/// Use it  at your own risk
/// May 2017
/// After a testing phase, the script should be optimized
/// </summary>


using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

public class RemoteStrings : MonoBehaviour {

	public int startup_port = 7000;				
	private int server_port = 25748; //25748
	private string server_ip;
	private string binaryName = "stringless";		// this could be concatenated with the name of the gameobject
	private IPAddress group_address = IPAddress.Parse("224.0.0.224");

	private UdpClient udp_client;
	private IPEndPoint remote_end;
	private OSC oscController;						// OSC controller for broadcast IP and port
	private OSC oscHandshaker;						// OSC to handshake
	private string localIP;

	private float _duration = 1f;
	private float _timer = 0f;

	private bool oscInit = true;
	private bool sendOsc = false;
	private bool portOK = false;
	private bool connected = false;
	private bool doHandShake = false;
	private bool reConnect = false;					// for connecting directly yo client

	private int countTries = 0;
	private string ipBroadcast;

	// Use this for initialization
	void Start () {
		Application.runInBackground = true;	// it is required for this remote string
		oscController = new OSC ();

		// Let's handle the IP
		localIP = Network.player.ipAddress;
		if (localIP == "0.0.0.0") 
			localIP = "127.0.0.1";

		ipBroadcast = localIP.ToString ().Substring (0, localIP.LastIndexOf (".")) + ".255";	// assuming subnet mask 255.255.255.0
		Debug.Log ("IP = " + localIP);

		StartStringlessClient ();

		// For future impementation
		//StartStringlessServer ();
	}

	/// <summary>
	/// Destroy method and sends a message that we are gone!
	/// </summary>
	void OnDestroy(){
		oscController.Close ();
		OscMessage messageOSC = new OscMessage();
		messageOSC.address = "CIAO";
		oscHandshaker.Send (messageOSC);
		oscHandshaker.Close ();
		udp_client.Close ();
		Network.Disconnect ();
	}


	void Update () {

			_timer += Time.deltaTime;
		if (_timer >= _duration) {
			_timer = 0f;
	//		if (!portOK) StartGameClient ();				// The port was in use, let's try new port connection
			if (portOK && oscInit) InitializeOSC();		// let's initialize OSC
			if (portOK && sendOsc) ConnectOsc ();		// port OK, OSC intializing OK, let's make a connection
			if (doHandShake) HandShake();				// let's try handshake every 1 sencond

		}
		if (reConnect) {
			oscHandshaker.Close ();
			Destroy (oscHandshaker);
			DialogOSC ();
			reConnect = false;
		}
	}

	/// <summary>
	/// Starts the stringless server.
	/// </summary>

	void StartStringlessServer()
	{
		Network.InitializeServer (10, server_port, false);
		//NetworkServerSimple servidor = new NetworkServerSimple ();
		StartCoroutine("StartBroadcast");
	}



	/// <summary>
	/// Starts the stringless client.
	/// </summary>

	void StartStringlessClient(){

					// multicast receive setup

			NetworkConnectionError error = Network.InitializeServer (1, startup_port, false);		// check if Port is free
			if (error == NetworkConnectionError.NoError) {
				Network.Disconnect ();
				Debug.Log ("Let's connect to port = " + startup_port);

				// multicast receive setup
				remote_end = new IPEndPoint (IPAddress.Any, startup_port);

				udp_client = new UdpClient (remote_end);
				udp_client.JoinMulticastGroup (group_address);

				// async callback for multicast
				udp_client.BeginReceive (new AsyncCallback (ServerLookup), null);
				StartCoroutine ("MakeConnection");
				portOK = true;
				udp_client.Close ();
				DialogOSC ();
			} else {
				Debug.Log ("Error, Port " + startup_port + " already in use. Let's try another one");
				startup_port = UnityEngine.Random.Range (5000, 60000);
			}


	}

	/// <summary>
	/// Initializes the OS.
	/// </summary>
	public void InitializeOSC(){

		oscController.setup (10000, ipBroadcast,25748);
		oscInit = false;		// dont want to do this again, only if needed!!!
		sendOsc = true; 		// start sending OSC
	}


	/// <summary>
	/// Starts the dialog using OSC
	/// </summary>
	void DialogOSC(){
		Debug.Log ("Starting dialog ... | local IP = "+localIP+" remote host = "+server_ip+" ipBroadcast = "+ipBroadcast);
		if (server_ip != null && !reConnect) {
			if (server_ip.Length > 0)
				ipBroadcast = server_ip;		// send directly to the host
		}

		if(!reConnect) oscHandshaker = gameObject.AddComponent<OSC>();
		oscHandshaker.listenPort = startup_port;
		oscHandshaker.broadcastPort = startup_port+1;
		oscHandshaker.broadcastHost = ipBroadcast;
		oscHandshaker.setup (startup_port, ipBroadcast,startup_port+1);		// Listen to the startup_port and send on this port + 1
		oscHandshaker.SetAllMessageHandler (OnReceive);						// receive messages OnReceive

	}
		

	/// <summary>
	/// Remotes the clients.
	/// </summary>
	void RemoteClients(){

		RCReceiver[] instancesReceveir;
		instancesReceveir = FindObjectsOfType (typeof(RCReceiver)) as RCReceiver[];

		GameObject receveirGameobject;
		receveirGameobject = this.gameObject;
		if (instancesReceveir.Length > 0) {
			string nameObject = "";
			foreach(RCReceiver item in instancesReceveir){
				//if(item.getPortIN() == RCPortInPort.getPortIN()) oscGameobject = item.gameObject;
				//Debug.Log("item = "+item.oscReference.getPortIN());
				Type itemType = item.getTypeArguments();
				OscMessage presetOSC = new OscMessage();


				if (nameObject != item.gameObject.name) {
					nameObject = item.gameObject.name;
					presetOSC.address = "SEND SPA " + nameObject;
					presetOSC.values.Add (nameObject);	// last limit
					presetOSC.values.Add (230);	// first limit	-- need to see how many arguments

					Color colorRGB = new Color(110,50,120,255);

//					if(item.GetTypeComponent() == typeof(Transform)) colorRGB = new Color(255,10,10,255);
//					else if(item.GetTypeComponent() == typeof(Light)) colorRGB = new Color(10,255,10,255);
//					else if(item.GetTypeComponent() == typeof(Rigidbody)) colorRGB = new Color(100,255,10,255);
//					else if(item.GetTypeComponent() == typeof(Camera)) colorRGB = new Color(100,100,100,255);

						presetOSC.values.Add (colorRGB.r);	
						presetOSC.values.Add (colorRGB.g);	
						presetOSC.values.Add (colorRGB.b);	

					oscHandshaker.Send (presetOSC);
				}
				OscMessage messageOSC = new OscMessage();

				// the same port, let's retrieve the parameters
				if (startup_port == item.listenPort) {
					

					if (item.getNumArguments () > 1) {
							List<float> vectorValues = new List<float> ();		// lets assume float
							if (itemType == typeof(Vector2)) {
								Vector2 tempVector = (Vector2)item.getObject ();
								for (int j = 0; j < 2; j++) {
									vectorValues.Add (tempVector [j]);
								}
							} else if (itemType == typeof(Vector3)) {
								Vector3 tempVector = (Vector3)item.getObject ();
								for (int j = 0; j < 3; j++) {
									vectorValues.Add (tempVector [j]);
								}
							} else if (itemType == typeof(Vector4)) {
								Vector4 tempVector = (Vector4)item.getObject ();
								for (int j = 0; j < 4; j++) {
									vectorValues.Add (tempVector [j]);
								}
							}
							

						for (int i = 0; i < vectorValues.Count; i++) {
							
							messageOSC.values.Clear ();
								messageOSC.address = "SEND FLT " + item.address.Substring (1, item.address.ToString ().Length - 1)+"##"+i;
							messageOSC.values.Add (vectorValues[i]);	// actual value
								float lowRange = 0.0f;
								float highRange = 1.0f;
								if (item.minRange.Count > 0)
									lowRange = item.minRange [i];
								if (item.maxRange.Count > 0)
									highRange = item.maxRange [i];
								messageOSC.values.Add (lowRange);	// first limit	-- need to see how many arguments
								messageOSC.values.Add (highRange);	// last limit
								oscHandshaker.Send (messageOSC);

						}

						
					} else {
						
						if (itemType == typeof(Single)) {
						
							messageOSC.address = "SEND FLT " + item.address.Substring (1, item.address.ToString ().Length - 1);
							messageOSC.values.Add (item.getFloatValue ());	// actual value

							float lowRange = 0.0f;
							float highRange = 1.0f;
							if (item.minRange.Count > 0)
								lowRange = item.minRange [0];
							if (item.maxRange.Count > 0)
								highRange = item.maxRange [0];
							messageOSC.values.Add (lowRange);	// first limit	-- need to see how many arguments
							messageOSC.values.Add (highRange);	// last limit
							oscHandshaker.Send (messageOSC);
		
						} else if (itemType == typeof(int)) {

							messageOSC.address = "SEND INT " + item.address.Substring (1, item.address.ToString ().Length - 1);
							messageOSC.values.Add (item.getIntValue ());	// actual value

							int lowRange = 0;
							int highRange = 1;
							if (item.minRange.Count > 0)
								lowRange = (int)item.minRange [0];
							if (item.maxRange.Count > 0)
								highRange = (int)item.maxRange [0];
							messageOSC.values.Add (lowRange);	// first limit	-- need to see how many arguments
							messageOSC.values.Add (highRange);	// last limit
							oscHandshaker.Send (messageOSC);

						} else if (itemType == typeof(bool)) {
							messageOSC.address = "SEND BOL " + item.address.Substring (1, item.address.ToString ().Length - 1);
							messageOSC.values.Add (item.getBoolValue ());	// actual value
							oscHandshaker.Send (messageOSC);
						}
					}

				}


			}
				
		}


	}


	/// <summary>
	/// Raises the receive event.
	/// </summary>
	/// <param name="packet">Packet.</param>

	void OnReceive(OscMessage packet)
	{
		OscMessage messageOSC = new OscMessage();
		string remoteHostIP = oscHandshaker.RemoteHost ();

//		print ("address = "+packet.address+" full = "+packet.ToString ());
		if (packet.ToString () == "HELO") {

			Debug.Log ("server says Hi, sending a Hello");

			messageOSC.address = "HELO";
			oscHandshaker.Send (messageOSC);


		} else if (packet.ToString () == "TEST") {
			messageOSC.address = "TEST";
			oscHandshaker.Send (messageOSC);
		} else if (packet.ToString () == "REQU") {
			RemoteClients();
			messageOSC.address = "REQU OK";
			oscHandshaker.Send (messageOSC);
		} else if (packet.ToString () == "PREL") {
			messageOSC.address = "PREL PRESET_NAME_LIST(test)";
			oscHandshaker.Send (messageOSC);
		} else if (packet.ToString () == "PREL OK") {
			connected = true;	
		} else if (packet.ToString () == "CIAO") { 
			oscInit = true;
			connected = false;		
		} else {

		}
	}

	/// <summary>
	/// Connects the osc.
	/// </summary>

	public void ConnectOsc(){
		Debug.Log ("connect");
		ArrayList objectList = new ArrayList();
		string valueSend = "HELO";


		objectList.Add (startup_port);
		objectList.Add (localIP);
		objectList.Add (binaryName);
		objectList.Add (countTries);

		OscMessage messageOSC = new OscMessage();
		messageOSC.address = "";
		messageOSC.values = objectList;
		oscController.Send(messageOSC);
	
		countTries++;

		doHandShake = true;

	}

	/// <summary>
	/// Handshaking sequence (send hello message) 
	/// </summary>

	void HandShake()
	{
		ArrayList objectList = new ArrayList();
		string valueSend = "HELO";
		objectList.Add (valueSend);
		OscMessage messageOSC = new OscMessage();
		messageOSC.address = "";
		messageOSC.values = objectList;
		oscHandshaker.Send(messageOSC);
		//Debug.Log ("handshake...send HELO to "+oscHandshaker.broadcastHost);
	}



	IEnumerator MakeConnection()
	{

		while (server_ip != null)
			yield return null;


		if (server_ip != null) {
			while (Network.peerType == NetworkPeerType.Disconnected) {
				Debug.Log ("connecting: " + server_ip + ":" + server_port);
				NetworkConnectionError error;
				error = Network.Connect (server_ip, server_port);

				Debug.Log ("status: " + error);
				yield return new WaitForSeconds (1);

			}
		}

	}



	/// <summary>
	/// Lockup for server
	/// </summary>
	/// <param name="arvar">Arvar.</param>

	void ServerLookup (IAsyncResult arvar)
	{
		byte[] receiveBytes = udp_client.EndReceive (arvar, ref remote_end);
		server_ip = remote_end.Address.ToString ();
		if(server_ip.Length>0) oscInit = true;
	}


	/// <summary>
	/// Starts the broadcast.
	/// </summary>
	/// <returns>The broadcast.</returns>


	IEnumerator StartBroadcast()
	{
		udp_client = new UdpClient ();
		udp_client.JoinMulticastGroup (group_address);
		remote_end = new IPEndPoint (group_address, startup_port);

		while (true) {
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes("StringlessServer"); 
			udp_client.Send (buffer, buffer.Length, remote_end);
			yield return new WaitForSeconds (1);
		}
	}

}
