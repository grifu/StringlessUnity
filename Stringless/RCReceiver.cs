//  Stringless: [RCReceiver] Remote Control Receiver 
//  ------------------------------------
//
// 	Allows the user to map the receving OSC message to any component
//  Version 2.2 Beta
//
//  Remote Control for Unity - part of Digital Puppet Tools, A digital ecosystem for Virtual Marionette Project
//
//	Copyright (c) 2015-2016 Luis Leite (Grifu)
//	www.virtualmarionette.grifu.com
//
// 	Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	of the Software.
//
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	IN THE SOFTWARE.
//


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public struct ObjectRequirements
{
	public int requiredArgumentsAmount;
	public Type requiredArgumentstype;

}

/// <summary>
/// Receives OSC packadges from selected parameters of the Gameobject
/// OSC messages are mapped to any selected property
/// 
/// Fields are not supported yet
/// 
/// TODO: This is a working proof of concept it still needs a lot of optimization
/// </summary>
/// 
[Serializable]
public class RCReceiver : MonoBehaviour
{

	public OSC oscReference;

	public Component objectComponent;
	public int _componentIndex = 0;
	public int _generalIndex = 0;
	public int _portIndex = 0;
	// network port
	public int _controlIndex = 0;
	public int _extra = 0;
	public bool _remoteString = false;
	public int teste;
	private List<string> oscAddress = new List<string> ();
	public int _addressIndex = 0;
	public PropertyInfo propertyObject;
	public PropertyInfo minMapping, maxMapping;
	public List<float> minRange, maxRange;
	public bool enableMapping = false;
	public bool learnOut = false;
	public MethodInfo methodObject;
	public FieldInfo fieldObject;
	public string address;
	public bool relativeAt = false;
	public OSC RCPortInPort;
	public int listenPort = 0;
	private ObjectRequirements requirements;
	private bool checkZeros = false;
	private int zeroIndex = 0;
	// requirments arguments (number and type)
	private object relativeValue;
	// keep the original value for relative option
	private SkinnedMeshRenderer meshTemp;
	// mesh for blendshapes
	private object[] oldValue;
	//private bool oscEnable = true;
	private Type typeComponent;
	private Type localType;
	private bool initString = false;
	
	/// <summary>
	///  Check required arguments for a given property
	/// </summary>
	Type CheckArgumentType (PropertyInfo info)
	{
		Type type = null;
		if (info != null) {
			type = info.PropertyType;
			//	requirements.requiredArgumentstype = type;
			if (type == typeof(int) || type == typeof(float) || type == typeof(bool) || type == typeof(string))
				requirements.requiredArgumentsAmount = 1;
			else if (type == typeof(Vector2))
				requirements.requiredArgumentsAmount = 2;
			else if (type == typeof(Vector3))
				requirements.requiredArgumentsAmount = 3;
			else if (type == typeof(Vector4))
				requirements.requiredArgumentsAmount = 4;
			else if (type == typeof(Enum))
				return type; // error
					else
				return type; // error
		} else {
//			String tempInfo = (String)info.ToString();
//			Debug.Log ("ingo = " + tempInfo);
			//	Debug.Log ("ingo = " + info.ToString().Split(' '));
			Debug.Log ("requirements = " + requirements.requiredArgumentstype);

		}
		localType = type;
		return type;

	}

	public int getNumArguments ()
	{
		return requirements.requiredArgumentsAmount;
	}

	public Type GetTypeComponent()
	{
		return typeComponent;
	}


	// function to retrieve the current value to RemoteString
	// have to work with the fields
	public float getFloatValue()
	{
		float returnFloat = 0;
		if (_controlIndex == 0) {
			returnFloat = (float)propertyObject.GetValue (objectComponent, null);
		} else if (_controlIndex == 1) {
			if (methodObject.Name == "SetBlendShapeWeight") {
				returnFloat = meshTemp.GetBlendShapeWeight (_extra);
			}else {
				returnFloat = (float) methodObject.GetParameters ().GetValue (0);
				}
		}

		return returnFloat;
	}

	public object getObject(){
		object abstractObject = new object();

		if (_controlIndex == 0) {
			abstractObject = (object)propertyObject.GetValue (objectComponent, null);
		} else if (_controlIndex == 1) {
			abstractObject = (object) methodObject.GetParameters ().GetValue (0);
		}

		return abstractObject;

	}


	// have to add support for fields
	public float getIntValue()
	{
		int returnInt = 0;
		if (_controlIndex == 0) {
			returnInt = (int)propertyObject.GetValue (objectComponent, null);
		} else if (_controlIndex == 1) {
			returnInt = (int) methodObject.GetParameters ().GetValue (0);
		}

		return returnInt;
	}



	public int getBoolValue()
	{
		int returnBool = 0;
		if (_controlIndex == 0) {
			returnBool = ((bool)propertyObject.GetValue (objectComponent, null) == true) ? 1 : 0;//	Assuming Boolean;

			//tempVar = ((float)packet.GetInt (0) == 1) ? true : false;//	Assuming Boolean

		} else if (_controlIndex == 1) {
			if(methodObject.GetParameters ().GetValue(0).GetType() == typeof(bool)) 
				returnBool = ((bool) methodObject.GetParameters ().GetValue (0) == true) ? 1 : 0;

			
		}

		return returnBool;

	}

	public Type getTypeArguments ()
	{


		if (_controlIndex == 0) {

			return CheckArgumentType (propertyObject);
		} else if (_controlIndex == 1){
			if (methodObject.Name == "SetBlendShapeWeight") {
				return typeof(float);
			} else {
				ParameterInfo[] parameters = methodObject.GetParameters ();

				foreach (ParameterInfo parameter in parameters) {
					return parameter.ParameterType;
				}
			}
		}
		return null;

	}

	public ObjectRequirements getRequirements ()
	{
		return requirements;
	}



	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{

		typeComponent = objectComponent.GetType();
		oscAddress.Clear ();		// cleaning the address list
		OSC[] instancesOSC;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];
		GameObject oscGameobject;
		oscGameobject = this.gameObject;


		// Should remove this when _remoteString = true
		// the issue is probably in Start() _remotestring is false, thus, transfer this to update() with a trigger
		if (instancesOSC.Length > 0) {
			if (RCPortInPort != null) {
				foreach (OSC item in instancesOSC) {
					if (item.getPortIN () == RCPortInPort.getPortIN ())
						oscGameobject = item.gameObject;
				}
				oscReference = oscGameobject.GetComponent<OSC> ();
				oscReference.SetAddressHandler (address, OnReceive);
				oscReference.SetAllMessageHandler (OnReceiveAll);
			}

		} else {

			if((FindObjectsOfType (typeof(RemoteStrings)) as RemoteStrings[]).Length < 1)
			Debug.Log ("NO OSC or RemoteString!!! You have to drag a OSC behavior or RemoteString to initalize the plugin");


		}
			
		// Initialize property object by defining a new one
		if (propertyObject == null) {

			Type typeComponent = objectComponent.GetType ();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly | BindingFlags.Public |
			                           BindingFlags.Instance | BindingFlags.Static;
			PropertyInfo[] properties = typeComponent.GetProperties (flags);
			if (properties.Length > _generalIndex)
				propertyObject = properties [_generalIndex];

		}

		// Initialize methods object by defining a new one
		if (methodObject == null) {
			Type typeComponent = objectComponent.GetType ();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly | BindingFlags.Public |
			                           BindingFlags.Instance | BindingFlags.Static;

			MethodInfo[] methods = typeComponent.GetMethods (flags);
			if (methods.Length > _generalIndex)
				methodObject = methods [_generalIndex];

		}
		requirements = new ObjectRequirements ();

		// this is for blendshapes
		GameObject objectTemp = this.gameObject;
		meshTemp = objectTemp.GetComponent<SkinnedMeshRenderer> ();
		if (meshTemp != null) {
			Mesh mesh = GetComponent<SkinnedMeshRenderer> ().sharedMesh;
			if (_extra + 1 > mesh.blendShapeCount)
				_extra = 0;					// verify if we have enought blenshapes
		}

		localType = getTypeArguments ();
	}


	/// <summary>
	/// Activate and deactivate all the RCReceiver behaviors attacthed to this object
	/// this is important to be able to control objects with same addresses and to switch their controls
	/// imagine that we have three lights with the same osc_address maped to a iphone osc fader
	/// we can create a switch object in pd or maxmsp that switch control between them each time that we press a specific button
	/// the button will send a activate and deactivate messages to the target behaviors with activate method
	/// </summary>
	/// <param name="value1">
	/// A <see cref="bool"/>
	/// </param>
	/// 
	public void Activate (bool value1)
	{
		Component[] oscComponents;
		oscComponents = this.GetComponents<RCReceiver> ();
		bool processActivate = true;

		// check how many behaviors are attatched to this object
		foreach (RCReceiver item in oscComponents) {

			if (item.methodObject != null)
			if (item.methodObject.Name == "Activate") // we will not touch in the Activate control behavior
					processActivate = false;
			else
				processActivate = true;
			else
				processActivate = true;
			// change everything but the activate behavior
			if (processActivate)
				item.enabled = value1;


		}

		//oscEnable = value1;
	}



	/// <summary>
	/// deactivate all the RCReceiver behaviors attacthed to this object
	/// this is important to be able to control objects with same addresses and to switch their controls
	/// imagine that we have three lights with the same osc_address maped to a iphone osc fader
	/// we can create a switch object in pd or maxmsp that switch control between them each time that we press a specific button
	/// the button will send a activate and deactivate messages to the target behaviors with activate method
	/// </summary>
	/// <param name="value1">
	/// A <see cref="bool"/>
	/// </param>
	/// 
	public void deActivate (bool value1)
	{
		Component[] oscComponents;
		oscComponents = this.GetComponents<RCReceiver> ();
		bool processActivate = true;
		
		// check how many behaviors are attatched to this object
		foreach (RCReceiver item in oscComponents) {
			
			if (item.methodObject != null)
			if (item.methodObject.Name == "Activate") // we will not touch in the Activate control behavior
					processActivate = false;
			else
				processActivate = true;
			else
				processActivate = true;
			// change everything but the activate behavior
			if (processActivate)
				item.enabled = !value1;
			
			
		}
		
		//oscEnable = value1;
	}

	/// <summary>
	/// Camera Switch
	/// </summary>
	/// <param name="value1">
	/// A <see cref="bool"/>
	/// </param>
	/// 
	public void cameraActivate (bool value1)
	{
		Camera validCamera;
		validCamera = this.GetComponent<Camera> ();
		if (validCamera != null) {
			validCamera.enabled = value1;
		}
	}

	void updateEditorAddress (string messageReceived)
	{

		string messageAddress = messageReceived.Replace ("/", "\\");			// change the slash
		#if UNITY_EDITOR
		EditorPrefs.SetString ("address" + oscAddress.Count.ToString (), messageAddress);
		#endif
		oscAddress.Add (messageReceived);		// this might be moved

		#if UNITY_EDITOR
		EditorPrefs.SetInt ("Addresses", oscAddress.Count);		// lets save the address
		#endif
	}



	// TODO: SHOULD be optimized
	void OnReceiveAll (OscMessage packet)
	{
		// just to help gathering a list of addresses to faciliate the selection

		if (!oscAddress.Contains (packet.address))
			updateEditorAddress (packet.address);

	
	}
	void OnReceiveStrings (OscMessage packet)
	{
		OscMessage message = new OscMessage ();
		message.address = packet.address;
		List<float> vectorValues = new List<float>();
		checkZeros = true;
		int trackingValue = 0;
		int trackDelimiter = message.address.IndexOf ("##");			// lets find the delimiter to extract the number
		if (trackDelimiter > 0) 
			trackingValue = int.Parse(message.address.Substring (trackDelimiter + 2, 1));


		for (int i = 0; i < requirements.requiredArgumentsAmount; i++) {
			if (i == trackingValue) {
				message.values.Add (packet.GetFloat (0));
				zeroIndex = i;
			} else {
				message.values.Add (0);
			}

		}

		OnReceive(message);

		//if(requirements.requiredArgumentsAmount == 3


	}


	/// <summary>
	/// Listener for incoming messages provided by OSC
	/// </summary>
	/// <param name="packet">
	/// A <see cref="OscMessage"/>
	/// </param>
	/// 

	void OnReceive (OscMessage packet)
	{

		if (this.enabled) {


			if (_controlIndex == 0) {
				object[] typeObject;

				// TODO: future implementation of TUIO
				// TUIO specification 1.1 

				/*
								if (packet.address == "/tuio/2Dcur") {
								// if (valor == "set")
								*/

				// fill arguments from object -- requirments.requiredArguments can be local variable!!!
				if (requirements.requiredArgumentsAmount == 0)
					requirements.requiredArgumentstype = CheckArgumentType (propertyObject); // TODO: can be optimized, can be done only once and registered in var


				// Create a new vector for receving the packets
				int numberArguments = packet.values.Count;	// how many arguments are we receiving
				typeObject = new object[requirements.requiredArgumentsAmount]; // the size of the required arguments


				// NUMBER OF ARGUMENTS IS LESS THEN REQUIRED
				// If there are no sufficient arguments as required lets fill the vector with 0's
				// TODO change 0's to the current value
				if (numberArguments < requirements.requiredArgumentsAmount) {

					for (int x = 0; x < (requirements.requiredArgumentsAmount); x++) {

						if (packet.values [0].GetType () == typeof(Single))
							typeObject [x] = 0F; // Put 0's if the value is a float
else
							typeObject [x] = packet.GetInt (0); // if we do not know the type, lets assign the first value to it (TODO: solve it)
					}
				} 


				// Only if we have more then one message and if the required messages are more then 1
				if (requirements.requiredArgumentsAmount > 1 && packet.values.Count > 1) {

					for (int i = 0; i < packet.values.Count; i++) {
						typeObject [i] = packet.GetFloat (i);
					}

				} else {
					// only one parameter arriving, lets chooose from the mapping

					if (enableMapping && !_remoteString) {
						int valueAssign = 0;
						for (int i = 0; i < requirements.requiredArgumentsAmount; i++) {
							if (maxRange [i] != 0 || minRange [i] != 0)
								valueAssign = i;
						}
						typeObject [valueAssign] = packet.GetFloat (0);
					} else {
						if (propertyObject.PropertyType == typeof(bool)) {
							typeObject [0] = ((int)packet.GetInt (0) == 1) ? true : false;//	Assuming Boolean
						} else if (propertyObject.PropertyType == typeof(int)) {
							typeObject [0] = packet.GetInt (0);
						} else {

							typeObject [0] = packet.GetFloat (0);
						}
					}
				}


		
				object tempVar;
				tempVar = null;
		
				// TODO: optimize please
				// Let's save the original position for the future
				if (relativeAt) {	
					relativeValue = propertyObject.GetValue (objectComponent, null);
					relativeAt = false; // to run just once
				}

				if (oldValue == null)
					oldValue = typeObject;			// save position for later comparison with the relative
		
				Type tempType = propertyObject.PropertyType;	// temporary type

				if (requirements.requiredArgumentstype != null)
					tempType = requirements.requiredArgumentstype;
				
				// Assign the correct type to the value
				if (requirements.requiredArgumentsAmount == 1) {
			
					// check if is relative
					if (relativeValue != null) {
						

						if (tempType == typeof(int)) {

							/*TODO:  CHECK this out
							int tempDif = 0;
							if (oldValue != typeObject) tempDif = (int)oldValue[0]-(int)typeObject [0];
							tempVar = (object)((int)relativeValue - tempDif);
*/
							tempVar = (object)((int)relativeValue + packet.GetInt (0));
							if (enableMapping && !_remoteString)
								tempVar = (object)((int)relativeValue + (int)Mathf.Lerp (minRange [0], maxRange [0], packet.GetInt (0)));
						} else if (tempType == typeof(float)) {
							/* TODO:  CHECK this out
							float tempDif = 0;
							if (oldValue != typeObject) tempDif = (float)oldValue[0]-(float)typeObject [0];
							tempVar = (object)((float)relativeValue - tempDif);
*/
							tempVar = (object)((float)relativeValue + packet.GetFloat (0));
							if (enableMapping && !_remoteString)
								tempVar = (object)((int)relativeValue + Mathf.Lerp (minRange [0], maxRange [0], packet.GetFloat (0)));
						}
				
					} else {

						// check if is a bool
						if (tempType == typeof(bool)) {
							tempVar = (packet.GetInt (0) == 1) ? true : false;//	Assuming Boolean
						} else {
							tempVar = packet.GetFloat (0);
							if (enableMapping && !_remoteString)
								tempVar = Mathf.Lerp (minRange [0], maxRange [0], packet.GetFloat (0));
						}
					}


			
				} else if (requirements.requiredArgumentsAmount == 2) { //	Assuming vector 2 as floats
					if (relativeValue != null) {
						Vector2 tempDif = new Vector2 (0, 0);
						if (oldValue != typeObject)
							tempDif = new Vector2 ((float)oldValue [0] - (float)typeObject [0], (float)oldValue [1] - (float)typeObject [1]);
						tempVar = (object)((Vector2)relativeValue - tempDif);

						if (enableMapping && !_remoteString)
							tempVar = (object)((Vector2)relativeValue + new Vector2 (Mathf.Lerp (minRange [0], maxRange [0], (float)typeObject [0]), Mathf.Lerp (minRange [1], maxRange [1], (float)typeObject [1])));
					} else {
						tempVar = new Vector2 ((float)typeObject [0], (float)typeObject [1]);
						if (enableMapping && !_remoteString)
							tempVar = new Vector2 (Mathf.Lerp (minRange [0], maxRange [0], (float)typeObject [0]), Mathf.Lerp (minRange [1], maxRange [1], (float)typeObject [1]));
					}
				} else if (requirements.requiredArgumentsAmount == 3) { //	Assuming vector 3 as floats
					if (relativeValue != null) { 	// for relative values 
						// save and compare the difference between the old and the new position 
						Vector3 tempDif = Vector3.zero;						
						if (oldValue != typeObject)
							tempDif = new Vector3 ((float)oldValue [0] - (float)typeObject [0], (float)oldValue [1] - (float)typeObject [1], (float)oldValue [2] - (float)typeObject [2]);

						//TODO: apply this to the not related also

						// TODO: must do this for the other vectors
						// check zeros to avoid assigning positions or rotation to 0. The problem is to understand if it is comming from a source with just one argument

						if (checkZeros && _remoteString) {

							Vector3 tempVector = Vector3.zero;
							Vector3 relativeVector = (Vector3)propertyObject.GetValue (objectComponent, null);
							if (zeroIndex == 0)
								tempVector = new Vector3 ((float)typeObject [0], ((Vector3)relativeVector).y, ((Vector3)relativeVector).z);
							else if (zeroIndex == 1)
								tempVector = new Vector3 (((Vector3)relativeVector).x, (float)typeObject [1], ((Vector3)relativeVector).z);
							else if (zeroIndex == 2)
								tempVector = new Vector3 (((Vector3)relativeVector).x, ((Vector3)relativeVector).y, (float)typeObject [2]); 


							tempVar = (object)tempVector;
						} else {
							tempVar = (object)((Vector3)relativeValue - tempDif);
						}

						if (enableMapping && !_remoteString)
							tempVar = (object)((Vector3)relativeValue + new Vector3 (Mathf.Lerp (minRange [0], maxRange [0], (float)tempDif.x), Mathf.Lerp (minRange [1], maxRange [1], (float)tempDif.y), Mathf.Lerp (minRange [2], maxRange [2], (float)tempDif.z)));
						
//					if(enableMapping) tempVar = (object)((Vector3)relativeValue +new Vector3(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]), Mathf.Lerp(minRange[2],maxRange[2],(float)typeObject[2])));
					} else {

						if (checkZeros && _remoteString) {

							Vector3 tempVector = Vector3.zero;
							Vector3 relativeVector = (Vector3)propertyObject.GetValue (objectComponent, null);

							if (zeroIndex == 0)
								tempVector = new Vector3 ((float)typeObject [0], ((Vector3)relativeVector).y, ((Vector3)relativeVector).z);
							else if (zeroIndex == 1)
								tempVector = new Vector3 (((Vector3)relativeVector).x, (float)typeObject [1], ((Vector3)relativeVector).z);
							else if (zeroIndex == 2)
								tempVector = new Vector3 (((Vector3)relativeVector).x, ((Vector3)relativeVector).y, (float)typeObject [2]); 
		

							tempVar = (object)tempVector;
						} else {

							tempVar = new Vector3 ((float)typeObject [0], (float)typeObject [1], (float)typeObject [2]);
						}
						if (enableMapping && !_remoteString)
							tempVar = new Vector3 (Mathf.Lerp (minRange [0], maxRange [0], (float)typeObject [0]), Mathf.Lerp (minRange [1], maxRange [1], (float)typeObject [1]), Mathf.Lerp (minRange [2], maxRange [2], (float)typeObject [2]));

					}
				} else if (requirements.requiredArgumentsAmount == 4) {//	Assuming vector 4 as floats
					if (relativeValue != null) {
						// save and compare the difference between the old and the new position 
						Vector4 tempDif = Vector4.zero;						
						if (oldValue != typeObject)
							tempDif = new Vector4 ((float)oldValue [0] - (float)typeObject [0], (float)oldValue [1] - (float)typeObject [1], (float)oldValue [2] - (float)typeObject [2], (float)oldValue [3] - (float)typeObject [3]);
						tempVar = (object)((Vector4)relativeValue - tempDif);
		
						if (enableMapping && !_remoteString)
							tempVar = (object)((Vector4)relativeValue + new Vector4 (Mathf.Lerp (minRange [0], maxRange [0], (float)typeObject [0]), Mathf.Lerp (minRange [1], maxRange [1], (float)typeObject [1]), Mathf.Lerp (minRange [2], maxRange [2], (float)typeObject [2]), Mathf.Lerp (minRange [3], maxRange [3], (float)typeObject [3])));
					} else {
						tempVar = new Vector4 ((float)typeObject [0], (float)typeObject [1], (float)typeObject [2], (float)typeObject [3]);
						if (enableMapping && !_remoteString)
							tempVar = new Vector4 (Mathf.Lerp (minRange [0], maxRange [0], (float)typeObject [0]), Mathf.Lerp (minRange [1], maxRange [1], (float)typeObject [1]), Mathf.Lerp (minRange [2], maxRange [2], (float)typeObject [2]), Mathf.Lerp (minRange [3], maxRange [3], (float)typeObject [3]));
					}
				}

					
				// EXECUTE THE COMMAND
				if (tempVar != null)
					propertyObject.SetValue (objectComponent, tempVar, new object[]{ });

			} else if (_controlIndex == 1) { // ------------------- METHODS
			
				if (methodObject != null) {

					//if(methodObject.GetParameters().Length == packet.Data.Count)

					// for now just for BlendShapes
					if (methodObject.Name == "SetBlendShapeWeight") {

						object[] typeObject = new object[methodObject.GetParameters ().Length]; // the size of the required arguments
						//						ParameterInfo[] parameters;
						Type[] objectType = new Type[2];

						typeObject [0] = (int)0;
						typeObject [1] = (float)3;

						objectType [0] = typeof(int);
						objectType [1] = typeof(float);

						if (!enableMapping || _remoteString) {
							if (meshTemp != null)
								meshTemp.SetBlendShapeWeight (_extra, packet.GetFloat (0));
						} else {
							if (meshTemp != null)
								meshTemp.SetBlendShapeWeight (_extra, Mathf.Lerp (minRange [0], maxRange [0], packet.GetFloat (0)));
							//	meshTemp.SetBlendShapeWeight (_extra, Mathf.Lerp (minRange [_extra], maxRange [_extra], packet.GetFloat (0)));
						}


					} else {

						if ((methodObject.GetParameters ().Length == packet.values.Count) || _remoteString) {

							// create an object vector to be sent when invoking the method as the parameters
							object[] typeObject = new object[methodObject.GetParameters ().Length]; // the size of the required arguments
							ParameterInfo[] parameters;
					
							parameters = methodObject.GetParameters ();
					
							int x = 0;
							bool checkTypes = true;		
							foreach (ParameterInfo parameter in parameters) {
							
								// for comparing the target method input types with the incoming OSC data types
								if (parameter.ParameterType != packet.values [x].GetType () && parameter.ParameterType != typeof(bool))
									checkTypes = false;
						
								// Bool is a special case because it arrives in 0's and 1's and should be converted to true and false
								if (parameter.ParameterType == typeof(bool)) {
								
									if (packet.values [0].GetType ().Name == "Int32") {
										typeObject [x] = (packet.GetInt (0) == 1) ? true : false;
									} else if (packet.values [0].GetType ().Name == "Single") {
										typeObject [x] = (packet.GetFloat (0) == 1) ? true : false;
									}
							
								} else {
									typeObject [x] = packet.values [x]; 
								}
								x++;
							}
					
					
					
							if (checkTypes) {
								if (methodObject.IsStatic)
									methodObject.Invoke (null, typeObject);
								else
									methodObject.Invoke (this, typeObject);
						
							}
						} 
					}
				}
			

			}

		}

	}


	/// <summary>
	///  For future implementation
	/// </summary>
	void OnEnable ()
	{
	}


	void RemoteStringCheck(){
		// let's create the reference -- If we have a remote string active
		RemoteStrings[] instanceRemoteStrings;
		instanceRemoteStrings = FindObjectsOfType (typeof(RemoteStrings)) as RemoteStrings[];
		GameObject remoteGameobject = this.gameObject;
		if (instanceRemoteStrings.Length > 0) {
			foreach(RemoteStrings item in instanceRemoteStrings)
			{
				if(item.startup_port == listenPort) remoteGameobject = item.gameObject;
			}
			oscReference = remoteGameobject.GetComponent<OSC> ();

			// check the type to receive
			string stringType = "";
			if (localType == typeof(float))
				stringType = "FLT";
			else if(localType == typeof(bool)) 
				stringType = "BOL";				
			else if(localType == typeof(int)) 
				stringType = "INT";
			else
				stringType = "FLT";

			//oscReference.SetAddressHandler ("SEND "+stringType+" "+address.Substring(1), OnReceiveRemote);

			if (requirements.requiredArgumentsAmount > 1) {
				for (int i = 0; i < requirements.requiredArgumentsAmount; i++) {
					oscReference.SetAddressHandler ("SEND " + stringType + " " + address.Substring (1)+"##"+i, OnReceiveStrings);
				}
			} else {
				oscReference.SetAddressHandler ("SEND " + stringType + " " + address.Substring (1), OnReceive);
			}
			initString = true;
		}


	}


	/// <summary>
	/// Updates the processing
	/// </summary>
	void Update ()
	{

		// must check remote String after starting
		if (_remoteString && !initString) RemoteStringCheck ();


		//if(oscAddress.Count == 0) updateEditorAddress (address);
		if (RCPortInPort != null) {
			// verify if its enable

			if (RCPortInPort.enabled) {
				// FOR FUTURE IMPLEMENTATION
			}

		}
		int receivedMessage = 0;
		float floatMessage = 0;

		if (propertyObject != null) {
			// FOR RECEIVING

			if (receivedMessage > 0) {
				// floatMessage
				propertyObject.SetValue (objectComponent, floatMessage, new object[]{ });
	
			}
		}
			
	}

	}
