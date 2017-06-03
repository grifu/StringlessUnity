//  Stringless: [RCReceiver Editor] Remote Control Transmiter [EDITOR]
//  ------------------------------------
//
// 	Allows the user to map the receving OSC message to any component
//  Version 2.1 Beta
//
//  Remote Control for Unity - part of Digital Puppet Tools, A digital ecosystem for Virtual Marionette Project
//
//	Copyright (c) 2015-2016 Luis Leite (Grifu)
//	www.virtualmarionette.grifu.com
//
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(RCReceiver))]
public class LevelScriptEditor : Editor 
{

	private static object GetField(object inObj, string fieldName) { object ret = null; FieldInfo info = inObj.GetType().GetField(fieldName); if (info != null) ret = info.GetValue(inObj); return ret; }

	public override void OnInspectorGUI()
	{

		RCReceiver myTarget = (RCReceiver)target;

		Component[] allComponents;
		string[] _choices;
		string[] _propertyChoice;
		string[] _serverChoice;
		string addressName; // OSC Address
		int test;


		myTarget.teste = EditorGUI.IntField (Rect.zero, myTarget.teste);

		addressName = "";
		// --------------------------------------------------------- [ COMPONENTS ]
		// get components from gameobject
		allComponents = myTarget.GetComponents<Component>();
		_choices = new string[allComponents.Length];

		int i = 0;
		foreach (Component component in allComponents) // create a list of components
		{
			_choices[i] = (string)component.GetType().Name;
			i++;
		}

		// Display popup
		int oldComponentIndex = myTarget._componentIndex;
		myTarget._componentIndex = EditorGUILayout.Popup("Component to control", myTarget._componentIndex, _choices);

		// reset components
		if (oldComponentIndex != myTarget._componentIndex) 
		{
			myTarget.address = "";
			myTarget._controlIndex = 0;
			myTarget._generalIndex = 0;
		}

		//Component childrenComponents;
		myTarget.objectComponent = myTarget.GetComponent(allComponents[myTarget._componentIndex].GetType());
		Type typeComponent = myTarget.objectComponent.GetType();

		const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly  | BindingFlags.Public | 
			BindingFlags.Instance | BindingFlags.Static;

		// let the user choose what to control
		int oldControlIndex = myTarget._controlIndex;
		String[] exposeOptions = { "Properties", "Methods", "Fields" };
		myTarget._controlIndex = EditorGUILayout.Popup("Control", myTarget._controlIndex, exposeOptions);
		// reset control
		if (oldControlIndex != myTarget._controlIndex) 
		{
			myTarget.address = "";
			myTarget._generalIndex = 0;
		}

		if(myTarget._controlIndex == 0) // properties
		{
			// To retrieve the properties
			PropertyInfo[] properties = typeComponent.GetProperties(flags);
			if(properties.Length > 0) // check if we have properties
			{
				_propertyChoice = new string[properties.Length];
				i = 0;
				foreach (PropertyInfo propertyInfo in properties)
				{
					// TODO: Select the CanWrite property to constraint the selection
					_propertyChoice[i] = (string)propertyInfo.Name;
					i++;
				}

				// Pop up for choosing the control properties
				int old_index = myTarget._generalIndex;
				myTarget._generalIndex = EditorGUILayout.Popup("Property to control", myTarget._generalIndex, _propertyChoice);
				myTarget.propertyObject = properties[myTarget._generalIndex];

				if (old_index != myTarget._generalIndex) myTarget.address = "";
				myTarget.relativeAt = EditorGUILayout.Toggle ("Relative to source", myTarget.relativeAt);
			



				// UPDATE the address field
				 addressName = "/"+myTarget.transform.name+"_"+myTarget.propertyObject.Name; 


			}
		} else if(myTarget._controlIndex == 1) // METHODS
		{
			// --------------------------------------------------------- [ METHODS ]
			// To retrieve the blendshapes
			MethodInfo[] methods = typeComponent.GetMethods(flags);
			_propertyChoice = new string[methods.Length];
			i=0;

			foreach (MethodInfo methodInfo in methods)
			{
				_propertyChoice[i] = (string)methodInfo.Name;
				i++;

			}
		//	Debug.Log (" index = " + myTarget._generalIndex);
			myTarget._generalIndex = EditorGUILayout.Popup("Method to control", myTarget._generalIndex, _propertyChoice);

//			Debug.Log (" index = " + myTarget._generalIndex);
			if (methods.Length > myTarget._generalIndex) {
				myTarget.methodObject = methods [myTarget._generalIndex];
				serializedObject.ApplyModifiedProperties ();
				addressName = "/" + myTarget.transform.name;
				myTarget._extra = EditorGUILayout.IntField ("Blendshape Index", myTarget._extra);
			}
		} else if(myTarget._controlIndex == 2)
		{
			// --------------------------------------------------------- [ FIELDS ]
			// Check for variables only if there are no main properties
			FieldInfo[] fields = typeComponent.GetFields(flags);

			int sizeField = fields.Length;

			_propertyChoice = new string[sizeField];
			i=0;
			foreach (FieldInfo fieldInfo in fields)
			{
				_propertyChoice[i] = (string)fieldInfo.Name;
				i++;
			}

			myTarget._generalIndex = EditorGUILayout.Popup("Variable to control", myTarget._generalIndex, _propertyChoice);

			if (fields.Length > myTarget._generalIndex) {
				myTarget.fieldObject = fields[myTarget._generalIndex];
			}

			addressName = "/"+myTarget.transform.name;
		}

		// check if there is new addresses 
		if(addressName == myTarget.address || myTarget.address == null)
		{
			myTarget.address = EditorGUILayout.TextField("OSC Address",addressName);
		} else
		{
			string maddresses = EditorPrefs.GetString("maddress");
			//Debug.Log ("madrresss = " + maddresses);
			// get the manually wroten address
			if(myTarget.address.Length > 0) addressName = myTarget.address ;
			myTarget.address = EditorGUILayout.TextField("OSC Address",addressName);

			EditorPrefs.SetString ("maddress", myTarget.address);
			
		}

		// TODO: Please optimize this
		// create a dialog for a list of known addresses
		int addresses = EditorPrefs.GetInt("Addresses");

		if(addresses > 0)
		{
			string[] addrOSC = new string[addresses+1];
			addrOSC[0] = myTarget.address.Replace("/","\\");	
			for(int a=0;a<addresses; a++)
			{
								/* Future implementation of TUIO
								string tempAddress = EditorPrefs.GetString ("address" + a.ToString ());
								if (tempAddress == "/tuio/2Dcur")
								*/
										
				addrOSC[a+1] = (EditorPrefs.GetString("address"+a.ToString()));



			}
			int indexAddress = myTarget._addressIndex;
			myTarget._addressIndex = EditorGUILayout.Popup("Last addresses",myTarget._addressIndex, addrOSC);
			if(indexAddress != myTarget._addressIndex)	// change the address
			{
				myTarget.address = addrOSC[myTarget._addressIndex].Replace("\\","/");
				myTarget._addressIndex = 0;
			}
		}


		// this should be concatenated with the OSC class
		RemoteStrings[] instanceRemoteStrings;
		instanceRemoteStrings = FindObjectsOfType (typeof(RemoteStrings)) as RemoteStrings[];
		//GameObject remoteGameobject = myTarget.gameObject;
		_serverChoice = new string[instanceRemoteStrings.Length];
		if (instanceRemoteStrings.Length > 0) {
			i = 0;
			foreach(RemoteStrings item in instanceRemoteStrings)
			{
				_serverChoice [i] = item.startup_port.ToString();
				i++;
//
//				if(item.startup_port == myTarget.RCPortInPort.listenPort) Debug.Log("jhsdkfh"); // remoteGameobject = item.gameObject;
			}
			myTarget._portIndex = EditorGUILayout.Popup("Input Port", myTarget._portIndex, _serverChoice);
			myTarget.listenPort = instanceRemoteStrings [myTarget._portIndex].startup_port;
			myTarget._remoteString = true;
			myTarget.enableMapping = true;
		}

		// Check for avaiable ports

		OSC[] instancesOSC;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];
		_serverChoice = new string[instancesOSC.Length];

		// Search for avaiable ports!
		if (instancesOSC.Length > 0 && instancesOSC.Length > myTarget._portIndex)
		{
			i = 0;
			foreach(OSC item in instancesOSC)
			{
				_serverChoice[i] = item.getPortIN().ToString();
				i++;
			}

			myTarget._portIndex = EditorGUILayout.Popup("Input Port", myTarget._portIndex, _serverChoice);
			myTarget.RCPortInPort = instancesOSC [myTarget._portIndex];
		} else
		{
			Debug.Log ("No active ports! Please drag OSC script into scene and setup ports");
		}
		//********************
		//
		//
		// TODO: this procedure should be optimized in the future
		
		if (myTarget._controlIndex == 0) {

			#if UNITY_EDITOR
			myTarget.enableMapping = EditorGUILayout.Toggle ("Enable Mapping", myTarget.enableMapping);
			if (myTarget.enableMapping) 
			{
				bool initList = false;
				bool resetMapping = false;
				myTarget.learnOut = EditorGUILayout.Toggle ("Learn", myTarget.learnOut);
				resetMapping = EditorGUILayout.Toggle ("Reset", resetMapping);
				if (resetMapping) {
					resetMapping = false;
					myTarget.minRange.Clear ();
					myTarget.maxRange.Clear ();
					myTarget.minRange = resetList (myTarget.propertyObject, myTarget.objectComponent, 0);
					myTarget.maxRange = resetList (myTarget.propertyObject, myTarget.objectComponent, 1);
					myTarget.learnOut = false;
				}
				Type type;
				type = myTarget.propertyObject.GetValue(myTarget.objectComponent,null).GetType();
				if (myTarget.minRange != null && myTarget.maxRange != null) 
				{
					if (myTarget.minRange.Count > 0 && myTarget.maxRange.Count > 0) 
					{
						for (int a=0; a <2; a++) 
						{
							string label = "";
							List<float> tempList = new List<float> ();
							if (a == 0) {
								tempList = myTarget.minRange;
								label = "Min";
							}
							if (a == 1) {
								tempList = myTarget.maxRange;
								label = "Max";
							}

							if (type == typeof(float) || type == typeof(int)) {
								float minValue = tempList [0];
								minValue = EditorGUILayout.FloatField (label, minValue);
								tempList [0] = minValue;
							} else if (type == typeof(Vector2)) {
								Vector2 minVec = convertVector2 (tempList);
								minVec = EditorGUILayout.Vector2Field (label, minVec);
								tempList = convertList (minVec);
							} else if (type == typeof(Vector3)) {
								
								Vector3 minVec = convertVector3 (tempList);
								minVec = EditorGUILayout.Vector3Field (label, minVec);
								tempList = convertList (minVec);
							} else if (type == typeof(Vector4)) {
								
								Vector4 minVec = convertVector4 (tempList);
								minVec = EditorGUILayout.Vector4Field (label, minVec);
								myTarget.minRange = convertList (minVec);
							} 
							if (a == 0)
								myTarget.minRange = tempList;
							if (a == 1)
								myTarget.maxRange = tempList;
							
						}
						
					} else
					{
						initList = true;
					}
					
				} else {
					initList = true;
				}
				
				if(myTarget.learnOut)
				{
					if (myTarget.minRange != null && myTarget.maxRange != null) 
					{
						if (myTarget.minRange.Count > 0 && myTarget.maxRange.Count > 0) 
						{
							List<float> listObjectTemp;
							listObjectTemp = propConvertList (myTarget.propertyObject, myTarget.objectComponent);
							
							if (myTarget.minRange.Count == listObjectTemp.Count && myTarget.maxRange.Count == listObjectTemp.Count) 
							{
								for (int x=0; x<listObjectTemp.Count; x++) {	
									if (listObjectTemp [x] < myTarget.minRange [x])
										myTarget.minRange [x] = listObjectTemp [x];
									if (listObjectTemp [x] > myTarget.maxRange [x])
										myTarget.maxRange [x] = listObjectTemp [x];
								}	
							}
						}
					} else {
						initList = true;
					}
				}
				if(initList)
				{
					myTarget.minRange = resetList (myTarget.propertyObject, myTarget.objectComponent, 0);
					myTarget.maxRange = resetList (myTarget.propertyObject, myTarget.objectComponent, 1);
				}
			}
			#endif
			
		} else if (myTarget._controlIndex == 1) {
			#if UNITY_EDITOR

			if(myTarget.methodObject!= null) if(myTarget.methodObject.Name == "SetBlendShapeWeight") myTarget.enableMapping = EditorGUILayout.Toggle ("Enable Mapping", myTarget.enableMapping);

			if (myTarget.enableMapping) 
			{
				bool initList = false;
				bool resetMapping = false;
				myTarget.learnOut = EditorGUILayout.Toggle ("Learn", myTarget.learnOut);
				resetMapping = EditorGUILayout.Toggle ("Reset", resetMapping);
				if (resetMapping) {
					resetMapping = false;
					myTarget.minRange.Clear ();
					myTarget.maxRange.Clear ();
					myTarget.learnOut = false;
				}



				if(myTarget.methodObject!= null) if(myTarget.methodObject.Name == "SetBlendShapeWeight")
					{
						GameObject objectTemp = myTarget.gameObject;
						SkinnedMeshRenderer meshTemp = objectTemp.GetComponent<SkinnedMeshRenderer>();
						Mesh m = meshTemp.sharedMesh;
						if (myTarget.minRange != null && myTarget.maxRange != null) 
						{

							// if there is not enought values in the list, lets create new
							if(myTarget.minRange != null && myTarget.maxRange != null)
							{
								if (myTarget.minRange.Count < m.blendShapeCount || myTarget.maxRange.Count < m.blendShapeCount) 
								{	
									initList = true;

								} else
								{
									//float minValue = myTarget.minRange [myTarget._extra];
								float minValue = myTarget.minRange [0];
									minValue = EditorGUILayout.FloatField ("min", minValue);
									//myTarget.minRange[myTarget._extra] = minValue;
								myTarget.minRange[0] = minValue;

									//float maxValue = myTarget.maxRange [myTarget._extra];
								float maxValue = myTarget.maxRange [0];
									maxValue = EditorGUILayout.FloatField ("max", maxValue);
									//myTarget.maxRange[myTarget._extra] = maxValue;
								myTarget.maxRange[0] = maxValue;

									if(myTarget.learnOut)
									{

//										if(meshTemp.GetBlendShapeWeight(myTarget._extra) < myTarget.minRange[myTarget._extra]) myTarget.minRange[myTarget._extra] = meshTemp.GetBlendShapeWeight(myTarget._extra);
//										if(meshTemp.GetBlendShapeWeight(myTarget._extra) > myTarget.maxRange[myTarget._extra]) myTarget.maxRange[myTarget._extra] = meshTemp.GetBlendShapeWeight(myTarget._extra);
									if(meshTemp.GetBlendShapeWeight(myTarget._extra) < myTarget.minRange[0]) myTarget.minRange[0] = meshTemp.GetBlendShapeWeight(myTarget._extra);
									if(meshTemp.GetBlendShapeWeight(myTarget._extra) > myTarget.maxRange[0]) myTarget.maxRange[0] = meshTemp.GetBlendShapeWeight(myTarget._extra);


									}
								}
							} else
							{
								initList = true;
							}

						} else 
						{
							initList = true;
						}
						if(initList)
						{

							if(myTarget.minRange == null || myTarget.maxRange == null) 
							{
								myTarget.minRange = new List<float>();
								myTarget.maxRange = new List<float>();
							}

							for(i=myTarget.minRange.Count; i<m.blendShapeCount;i++)
								{
									myTarget.minRange.Add(0);
									myTarget.maxRange.Add(1);
								}
							}

					}
			}
			#endif
		}
		//**********************************
		if (GUI.changed == true) {
			EditorSceneManager.MarkAllScenesDirty();
		}
		//**********************************

	}
	


	/// <summary>
	/// Reset list, fill the list with 0's or 1's
	/// float valueReset should be 0 or 1
	/// </summary>
	/// <param name="propObject, objectC, valueReset">
	/// A <see cref="PropertyInfo, Component, float"/>
	/// </param>
	/// <returns>
	/// A <see cref="List<float>"/>
	/// </returns>
	/// 

	List<float> resetList(PropertyInfo propObject, Component objectC, float valueReset)
	{
		List<float> listObjects = new List<float>();
		Type type = propObject.PropertyType;
		
		if (type == typeof(int) || type == typeof(float))
		{
			listObjects.Add(valueReset);
		} else if (type == typeof(Vector2))
		{
			for(int a=0;a < 2; a++){
				listObjects.Add(valueReset);
			}
		} else if (type == typeof(Vector3))
		{
			for(int a=0;a < 3; a++){
				listObjects.Add(valueReset);
			}
		} else if (type == typeof(Vector4))
		{
			for(int a=0;a < 4; a++){
				listObjects.Add(valueReset);
			}
		}
		return listObjects;
	}

	/// <summary>
	/// Convert properties to a list of floats
	/// </summary>
	/// <param name="propObject, objectC, valueReset">
	/// A <see cref="PropertyInfo, Component, float"/>
	/// </param>
	/// <returns>
	/// A <see cref="List<float>"/>
	/// </returns>
	/// 

	List<float> propConvertList(PropertyInfo propObject, Component objectC)
	{
		List<float> listObjects = new List<float>();
		Type type = propObject.PropertyType;

		if (type == typeof(int) || type == typeof(float))
		{
			listObjects.Add((float)(propObject.GetValue (objectC, null)));
		} else if (type == typeof(Vector2))
		{
			for(int a=0;a < 2; a++){
				listObjects.Add(((Vector2)(propObject.GetValue (objectC, null)))[a]);
			}
		} else if (type == typeof(Vector3))
		{
			for(int a=0;a < 3; a++){
				listObjects.Add(((Vector3)(propObject.GetValue (objectC, null)))[a]);
			}
		} else if (type == typeof(Vector4))
		{
			for(int a=0;a < 4; a++){
			listObjects.Add(((Vector4)(propObject.GetValue (objectC, null)))[a]);
			}
		}
		return listObjects;
	}

	/// <summary>
	/// Converts Vector2 to List of floats
	/// </summary>
	List<float> convertList(Vector2 vectorValue)
	{
		List<float> listVector = new List<float>();
		for(int y=0; y<2; y++)
		{
			listVector.Add(vectorValue[y]);
		}
		if (listVector.Count > 0)
						return listVector;

		return null;
	}

	/// <summary>
	/// Converts Vector3 to List of floats
	/// </summary>
	List<float> convertList(Vector3 vectorValue)
	{
		List<float> listVector = new List<float>();
		for(int y=0; y<3; y++)
		{
			listVector.Add(vectorValue[y]);
		}
		if (listVector.Count > 0)
			return listVector;
		
		return null;
	}

	/// <summary>
	/// Converts Vector4 to List of floats
	/// </summary>
	List<float> convertList(Vector4 vectorValue)
	{
		List<float> listVector = new List<float>();
		for(int y=0; y<4; y++)
		{
			listVector.Add(vectorValue[y]);
		}
		if (listVector.Count > 0)
			return listVector;
		
		return null;
	}

	/// <summary>
	/// Converts list of floats to Vector2
	/// </summary>
	Vector2 convertVector2(List<float> listValues)
	{
		Vector2 values;
		values = Vector2.zero;
		for(int y=0;y<listValues.Count;y++)
		{
			values[y] = listValues[y];
		}
		
		return values;
	}

	/// <summary>
	/// Converts list of floats to Vector3
	/// </summary>
	Vector3 convertVector3(List<float> listValues)
	{
		Vector3 values;
		values = Vector3.zero;
		for(int y=0;y<listValues.Count;y++)
		{
			values[y] = listValues[y];
		}

		return values;
	}

	/// <summary>
	/// Converts list of floats to Vector4
	/// </summary>
	Vector4 convertVector4(List<float> listValues)
	{
		Vector4 values;
		values = Vector4.zero;
		for(int y=0;y<listValues.Count;y++)
		{
			values[y] = listValues[y];
		}
		
		return values;
	}
	void onReceiveMessages(OscMessage packet) {
		Debug.Log("message = "+packet.address);
		}

}
#endif

