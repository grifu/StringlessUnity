//  Stringless: [RCSender Editor] Remote Control Transmiter [EDITOR]
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
using System.Collections;
using System.Reflection;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR

[CustomEditor(typeof(RCSender))] 
public class RCSenderEditor : Editor
{
	private static object GetField(object inObj, string fieldName) { object ret = null; FieldInfo info = inObj.GetType().GetField(fieldName); if (info != null) ret = info.GetValue(inObj); return ret; }
	
	
	public override void OnInspectorGUI()
	{
		RCSender myTarget = (RCSender)target;
		
		Component[] allComponents;
		string[] _choices;
		string[] _propertyChoice;
		string[] _serverChoice;
		string addressName; // OSC Address
		

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
		//myTarget.objectComponent = myTarget.GetComponentInChildren(allComponents[myTarget._componentIndex].GetType());

		myTarget.objectComponent = myTarget.GetComponent(allComponents[myTarget._componentIndex].GetType());
		Type typeComponent = myTarget.objectComponent.GetType();
		
		const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly  | BindingFlags.Public | 
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
					// TODO: Select the CanRead property to constraint the selection
//					if(propertyInfo.CanRead) {
						_propertyChoice[i] = (string)propertyInfo.Name;
						i++;
//					}
				}
				
				// Pop up for choosing the control properties
				int old_index = myTarget._generalIndex;
				myTarget._generalIndex = EditorGUILayout.Popup("Property to control", myTarget._generalIndex, _propertyChoice);
				myTarget.propertyObject = properties[myTarget._generalIndex];
				
				if (old_index != myTarget._generalIndex) myTarget.address = "";

				myTarget.sendEveryFrame = EditorGUILayout.Toggle ("Send every frame", myTarget.sendEveryFrame);
				
				
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
			myTarget._generalIndex = EditorGUILayout.Popup("Method to control", myTarget._generalIndex, _propertyChoice);
			if(methods.Length > myTarget._generalIndex) myTarget.methodObject = methods[myTarget._generalIndex];
			myTarget._extra = EditorGUILayout.IntField("Blendshape Index",myTarget._extra);
			addressName = "/"+myTarget.transform.name;

		} else if(myTarget._controlIndex == 2)
		{
			// --------------------------------------------------------- [ FIELDS ]
			// Check for variables only if there are no main properties
			// 
			FieldInfo[] fields = typeComponent.GetFields(flags);
			
			_propertyChoice = new string[fields.Length];
			i=0;
			foreach (FieldInfo fieldInfo in fields)
			{
				_propertyChoice[i] = (string)fieldInfo.Name;
				i++;
				
			}
			myTarget._generalIndex = EditorGUILayout.Popup("Variable to control", myTarget._generalIndex, _propertyChoice);
			if(fields.Length > myTarget._generalIndex) myTarget.fieldObject = fields[myTarget._generalIndex];

			myTarget.sendEveryFrame = EditorGUILayout.Toggle ("Send every frame", myTarget.sendEveryFrame);
			addressName = "/"+myTarget.transform.name;

			
		}

		// check if there is new addresses 
		if(addressName == myTarget.address || myTarget.address == null)
		{
			myTarget.address = EditorGUILayout.TextField("OSC Address",addressName);
		} else
		{
			// get the manually wroten address
			if(myTarget.address.Length > 0) addressName = myTarget.address ;
			myTarget.address = EditorGUILayout.TextField("OSC Address",addressName);

		}


		
		// Pop up for choosing the input port
		//
		// TODO: Should check if the port is active or not (the script), maybe in the future an editor way to create the ports
		//


		OSC[] instancesOSC;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];
		_serverChoice = new string[instancesOSC.Length];

		if (instancesOSC.Length > 0 && instancesOSC.Length > myTarget._portIndex)
		{
			i = 0;
			foreach(OSC item in instancesOSC)
			{
				_serverChoice[i] = item.getPortOUT().ToString();
				i++;
			}
			myTarget._portIndex = EditorGUILayout.Popup("Output Port", myTarget._portIndex, _serverChoice);
			myTarget.OSCtransmitPort = instancesOSC [myTarget._portIndex];
		} else
		{
			Debug.Log("There are no Output Ports active, setup a port on the OSC behavior");
			myTarget._portIndex = 0;
		}


	}
}

#endif