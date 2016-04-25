//  Stringless Editor: Remote Control Interface for Unity 
//  ------------------------------------
//
//	Stringless OSC Window Tracer
//	Version 1.0 Beta
//
//	Updated for Unity 5 (need to optimize the get componnent issue
//
//	This code was adapted from UnityOSC from Jorge Garcia
//	
//	Copyright (c) 2015-2016 Luis Leite (Grifu)
//	www.virtualmarionette.grifu.com

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Collections;


/// <summary>
/// Editor window to trace incoming and outgoing OSC messages.
/// This script should be placed at the /Editor folder.
/// To access this window select "Window->Stringless" from the Unity Menu.
/// </summary>
/// 

public class Stringless : EditorWindow
{
	#region Member variables
	public OSC oscReference;
	private string _status = "";
	private string _selected = "none";
	public List<string> _output = new List<string>();

	private int inSelected = -1;
	private int outSelected = -1;
	public OSC[] instancesOSC;

	//TODO: remove this dictionary
	private Dictionary<int, OSC> _clientOSC = new Dictionary<int, OSC>();
	private Dictionary<int, OSC> _serverOSC = new Dictionary<int, OSC>();

	//TODO: This new dictionaty is compatible with Unity5, instead of pairing with OSC lets save the gameobject
	private Dictionary<int, GameObject> _clientOSCg = new Dictionary<int, GameObject>();
	private Dictionary<int, GameObject> _serverOSCg = new Dictionary<int, GameObject>();

	#endregion

	
	/// <summary>
	/// Initializes the OSC Helper and creates an entry in the Unity menu.
	/// </summary>
	[MenuItem("Window/Stringless")]
	static void Init ()
	{
		Stringless window = (Stringless)EditorWindow.GetWindow (typeof(Stringless));
		window.Show();

	}

	void OnEnable()
	{
		outSelected = -1;
		inSelected = -1;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];

		if (instancesOSC.Length > 0)
		{
			
			foreach(OSC item in instancesOSC)
			{
				if(!_clientOSC.ContainsKey(item.getPortIN())) _clientOSC.Add(item.getPortIN(), item.gameObject.GetComponent<OSC>());
				if(!_serverOSC.ContainsKey(item.getPortOUT())) _serverOSC.Add(item.getPortOUT(), item.gameObject.GetComponent<OSC>());
				if(!_clientOSCg.ContainsKey(item.getPortIN())) _clientOSCg.Add(item.getPortIN(), item.gameObject);
				if(!_serverOSCg.ContainsKey(item.getPortOUT())) _serverOSCg.Add(item.getPortOUT(), item.gameObject);
			}
			_output.Clear();
		}
	}

	/// <summary>
	/// Executes OnGUI in the panel within the Unity Editor
	/// </summary>
	void OnGUI ()
	{	

		bool change = false;

		if(EditorApplication.isPlaying)
		{
			_status = "";
			GUILayout.Label(_status, EditorStyles.boldLabel);
			GUILayout.Label(String.Concat("SELECTED: ", _selected));

			foreach(KeyValuePair<int, OSC> pair in _clientOSC)
			{
				if(pair.Key > 0) 
				{
					if(GUILayout.Button(String.Format("Input port: {0}", pair.Key))) 
					{
						inSelected = pair.Key;
						change = true;
						_output.Clear();
					}
				}

			}
			if(change)
			{
				foreach(KeyValuePair<int, OSC> pair in _clientOSC)
				{
					if (_clientOSC [pair.Key] != null) {
						GameObject objOSC = _clientOSC [pair.Key].gameObject;
						objOSC.GetComponent<OSC> ().SetAllMessageHandler (null);
					}
				}
				change = false;
			}

			foreach(KeyValuePair<int, OSC> pair in _serverOSC)
			{
				if(pair.Key > 0) 
				{
					if(GUILayout.Button(String.Format("Output port: {0}", pair.Key))) 
					{
						outSelected = pair.Key;
						_output.Clear();
					}
				}
			}
			if(change)
			{
				foreach(KeyValuePair<int, OSC> pair in _serverOSC)
				{
					GameObject objOSC = _serverOSC[pair.Key].gameObject;
					objOSC.GetComponent<OSC> ().setSendMessage(null);
					
				}
				change = false;
			}


			if(inSelected> 0 || outSelected > 0)  GUILayout.TextArea(FromListToString(_output));

		}
		else
		{
			_status = "\n Enter the play mode in the Editor to see \n running clients and servers";
			GUILayout.Label(_status, EditorStyles.boldLabel);
		}
	}


	/// <summary>
	/// Updates the logs of the running clients and servers.
	/// </summary>
	void Update()
	{
		if(EditorApplication.isPlaying)
		{
			//Debug.Log ("client = " + _clientOSC.Count + " itenm= "+_clientOSC[inSelected].);
			if((inSelected > 0) && (_clientOSC.ContainsKey(inSelected)))
			{

				if (instancesOSC.Length > 0)
				{
					
						if (_clientOSC [inSelected].getPortIN () == inSelected) {

				//			_clientOSC [inSelected].SetAllMessageHandler (OnReceive);
							GameObject objOSC = _clientOSCg [inSelected];
							objOSC.GetComponent<OSC> ().SetAllMessageHandler (OnReceive);
						}
				}

			}
			if((outSelected > 0) && (_serverOSC.ContainsKey(outSelected)))
			{
				
				//if (instancesOSC.Length > 0)
				{
					if(_serverOSC[outSelected].getPortOUT() == outSelected)
					{
					//	_serverOSC[outSelected].setSendMessage(OnReceive);
						GameObject objOSC = _serverOSCg[outSelected];
						objOSC.GetComponent<OSC> ().setSendMessage(OnReceive);
						//_clientOSC[inSelected].SetAllMessageHandler(OnReceive);
					}
					
				}
			}


			Repaint();
		}
	}




	/// <summary>
	/// Formats a collection of strings to a single concatenated string.
	/// </summary>
	/// <param name="input">
	/// A <see cref="List<System.String>"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string FromListToString(List<string> input)
	{
		string output = "";
		
		foreach(string value in input)
		{
			output += value;
		}
		
		return output;	
	}


	/// <summary>
	/// Gets received message from OSC
	/// </summary>
	/// <param name="OscMessage">
	/// A <see cref="OscMessage packet"/>
	/// </param>
	/// <returns>
	/// A <see cref="OscMessage"/>
	/// </returns>
	private void OnReceive(OscMessage packet)
	{
		if(_output.Count < 25)
		{
			_output.Add(String.Concat(DateTime.UtcNow.ToString(),".",
		                          FormatMilliseconds(DateTime.Now.Millisecond), " : ",
		                          packet.address," ", DataToString(packet.values)));

		}	else
		{
			_output.RemoveAt(0);
			_output.Add(String.Concat(DateTime.UtcNow.ToString(),".",
			                          FormatMilliseconds(DateTime.Now.Millisecond), " : ",
			                          packet.address," ", DataToString(packet.values)));
		}

	}
	
	/// <summary>
	/// Formats a milliseconds number to a 000 format. E.g. given 50, it outputs 050. Given 5, it outputs 005 (adapted from Jorge Garcia's UnityOSC)
	/// </summary>
	/// <param name="milliseconds">
	/// A <see cref="System.Int32"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string FormatMilliseconds(int milliseconds)
	{	
		if(milliseconds < 100)
		{
			if(milliseconds < 10)
				return String.Concat("00",milliseconds.ToString());
			
			return String.Concat("0",milliseconds.ToString());
		}
		
		return milliseconds.ToString();
	}

	/// <summary>
	/// Converts a collection of object values to a concatenated string. (adapted from Jorge Garcia's UnityOSC)
	/// </summary>
	/// <param name="data">
	/// A <see cref="List<System.Object>"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string DataToString(ArrayList data)
	{
		string buffer = "";
		
		for(int i = 0; i < data.Count; i++)
		{
			buffer += data[i].ToString() + " ";
		}
		
		buffer += "\n";
		
		return buffer;
	}


}