//  Stringless: [RCReceiver] Remote Control Receiver 
//  ------------------------------------
//
// 	Allows the user to map the receving OSC message to any component
//  Version 2.0 Beta
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
public class RCReceiver : MonoBehaviour {

	public OSC oscReference;

	[SerializeField]
	public Component objectComponent;
	public int _componentIndex = 0;
	public int _generalIndex = 0;
	public int _portIndex = 0;			// network port
	public int _controlIndex = 0;
	public int _extra = 0;
	[SerializeField]
	public PropertyInfo propertyObject;
	public PropertyInfo minMapping, maxMapping;
	public List<float> minRange, maxRange;
	public bool enableMapping = false;
	public bool learnOut = false;
	[SerializeField]
	public MethodInfo methodObject;
	public string address;
	public bool relativeAt = false;
	public OSC RCPortInPort;
	private ObjectRequirements requirements;	// requirments arguments (number and type)
	private object relativeValue;				// keep the original value for relative option

	private SkinnedMeshRenderer meshTemp;		// mesh for blendshapes
	//private bool oscEnable = true;


	
	/// <summary>
	///  Check required arguments for a given property
	/// </summary>
	Type CheckArgumentType(PropertyInfo info)
	{
		Type type = info.PropertyType;
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
				else return type; // error

		return type;

	}

	

	/// <summary>
	/// Initialization
	/// </summary>
	void Start () 
	{

		OSC[] instancesOSC;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];
		GameObject oscGameobject;
		oscGameobject = this.gameObject;
		if (instancesOSC.Length > 0)
		{
			if(RCPortInPort != null)
			{;
				foreach(OSC item in instancesOSC)
				{
					if(item.getPortIN() == RCPortInPort.getPortIN()) oscGameobject = item.gameObject;
				}
				oscReference = oscGameobject.GetComponent<OSC> ();
				oscReference.SetAddressHandler( address , OnReceive );
			}

		}else 
		{
			Debug.Log ("NO OSC!!! You have to drag a OSC behavior to initalize the plugin");


		}

		// Initialize property object by defining a new one
		if (propertyObject == null)
		{

			Type typeComponent = objectComponent.GetType();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly  | BindingFlags.Public | 
				BindingFlags.Instance | BindingFlags.Static;
			PropertyInfo[] properties = typeComponent.GetProperties(flags);
			if(properties.Length > _generalIndex) propertyObject = properties[_generalIndex];

		}

		// Initialize methods object by defining a new one
		if (methodObject == null)
		{
			Type typeComponent = objectComponent.GetType();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly  | BindingFlags.Public | 
				BindingFlags.Instance | BindingFlags.Static;

			MethodInfo[] methods = typeComponent.GetMethods(flags);
			if(methods.Length > _generalIndex) methodObject = methods[_generalIndex];

		}
		requirements = new ObjectRequirements();

		// this is for blendshapes
		GameObject objectTemp = this.gameObject;
		meshTemp = objectTemp.GetComponent<SkinnedMeshRenderer>();
		if (meshTemp != null) 
		{
			Mesh mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
			if(_extra+1 > mesh.blendShapeCount) _extra = 0;					// verify if we have enought blenshapes
		}


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
	public void Activate(bool value1)
	{
		Component[] oscComponents;
		oscComponents = this.GetComponents<RCReceiver> ();
		bool processActivate = true;

		// check how many behaviors are attatched to this object
		foreach(RCReceiver item in oscComponents)
		{

			if(item.methodObject != null) 
				if(item.methodObject.Name == "Activate") // we will not touch in the Activate control behavior
					processActivate = false;
				else
					processActivate = true;
			else
				processActivate = true;
			// change everything but the activate behavior
			if(processActivate)	item.enabled = value1;


		}

		//oscEnable = value1;
	}



	/// <summary>
	/// Listener for incoming messages provided by OSC
	/// </summary>
	/// <param name="packet">
	/// A <see cref="OscMessage"/>
	/// </param>
	/// 

	void OnReceive(OscMessage packet) {


	if(this.enabled)
		{
		if(_controlIndex == 0)
		{
			// fill arguments from object -- requirments.requiredArguments can be local variable!!!
			if(requirements.requiredArgumentsAmount == 0) requirements.requiredArgumentstype = CheckArgumentType(propertyObject); // TODO: can be optimized
			
			
			// Create a new vector for receving the packets
			int numberArguments = packet.values.Count;	// how many arguments are we receiving
			object[] typeObject = new object[requirements.requiredArgumentsAmount]; // the size of the required arguments


		
			// Only if we have more then one message and if the required messages are more then 1
			if(requirements.requiredArgumentsAmount > 1 && packet.values.Count > 1)
			{
				for ( int i = 0 ; i < packet.values.Count ; i++) 
					{
						typeObject[i] = packet.GetFloat(i);
					}
			}

		// NUMBER OF ARGUMENTS IS LESS THEN REQUIRED
		// If there are no sufficient arguments as required lets fill the vector with 0's
		if(numberArguments < requirements.requiredArgumentsAmount)
		{
			for (int x = numberArguments; x< (requirements.requiredArgumentsAmount); x++) 
			{

				if (packet.values[0].GetType() == typeof(Single))
					typeObject[x] = 0F; // Put 0's if the value is a float
					 else 
					typeObject[x] = packet.GetInt(0); // if we do not know the type, lets assign the first value to it (TODO: solve it)

			}
			
		} 

		object tempVar;
		tempVar = null;
		
		// TODO: optimize please
		// Let's save the original position for the future
		if(relativeAt) 
		{	
			relativeValue = propertyObject.GetValue(objectComponent,null);
			relativeAt = false; // to run just once
		}
		
		// Assign the correct type to the value
		if(requirements.requiredArgumentsAmount == 1)
		{
			

			// check if is relative
			if(relativeValue != null)
			{
				if(requirements.requiredArgumentstype == typeof(int)){
					tempVar = (object)((int)relativeValue + packet.GetInt(0));
					if(enableMapping) tempVar =(object)((int)relativeValue +(int)Mathf.Lerp(minRange[0],maxRange[0],packet.GetInt(0)));
						}
				else if(requirements.requiredArgumentstype == typeof(float)){
					tempVar = (object)((float)relativeValue + packet.GetFloat(0));
					if(enableMapping) tempVar =(object)((int)relativeValue +Mathf.Lerp(minRange[0],maxRange[0],packet.GetFloat(0)));
						}
				
			} else
			{
				tempVar = packet.GetFloat(0);
				if(enableMapping) tempVar =Mathf.Lerp(minRange[0],maxRange[0],packet.GetFloat(0));
			}

			// check if is a bool
			if(requirements.requiredArgumentstype == typeof(bool))  tempVar = ((float)packet.GetInt(0) == 1) ? true : false;//	Assuming Boolean
			
		} else if(requirements.requiredArgumentsAmount == 2) //	Assuming vector 2 as floats
			{
				if(relativeValue != null)
					tempVar = (object)((Vector2)relativeValue + new Vector2((float)typeObject[0],(float)typeObject[1]));
					if(enableMapping) tempVar =(object)((Vector2)relativeValue +new Vector2(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1])));
				else		
					tempVar = new Vector2((float)typeObject[0],(float)typeObject[1]);
					if(enableMapping) tempVar =new Vector2(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]));
				
		} else if(requirements.requiredArgumentsAmount == 3) //	Assuming vector 3 as floats
			{
				if(relativeValue != null) 	// for relative values 
				{
					// TODO: R2 - Solve the offset problem
					tempVar = (object)((Vector3)relativeValue + new Vector3((float)typeObject[0],(float)typeObject[1],(float)typeObject[2]));
					if(enableMapping) tempVar = (object)((Vector3)relativeValue +new Vector3(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]), Mathf.Lerp(minRange[2],maxRange[2],(float)typeObject[2])));
				}
				else
				{
					tempVar = new Vector3((float)typeObject[0],(float)typeObject[1],(float)typeObject[2]);
					if(enableMapping) tempVar =new Vector3(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]), Mathf.Lerp(minRange[2],maxRange[2],(float)typeObject[2]));

				}
		} else if(requirements.requiredArgumentsAmount == 4)//	Assuming vector 4 as floats
			{
				if(relativeValue != null)
					tempVar = (object)((Vector4)relativeValue + new Vector4((float)typeObject[0],(float)typeObject[1],(float)typeObject[2],(float)typeObject[3]));
					if(enableMapping) tempVar =(object)((Vector4)relativeValue +new Vector4(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]), Mathf.Lerp(minRange[2],maxRange[2],(float)typeObject[2]), Mathf.Lerp(minRange[3],maxRange[3],(float)typeObject[3])));
				else
					tempVar = new Vector4((float)typeObject[0],(float)typeObject[1],(float)typeObject[2],(float)typeObject[3]);
					if(enableMapping) tempVar =new Vector4(Mathf.Lerp(minRange[0],maxRange[0],(float)typeObject[0]), Mathf.Lerp(minRange[1],maxRange[1],(float)typeObject[1]), Mathf.Lerp(minRange[2],maxRange[2],(float)typeObject[2]), Mathf.Lerp(minRange[3],maxRange[3],(float)typeObject[3]));
				}

		
			// EXECUTE THE COMMAND
			if (tempVar != null)
				propertyObject.SetValue(objectComponent,tempVar,new object[]{});

		} else if(_controlIndex == 1) // ------------------- METHODS
		{
			
			if(methodObject != null)
			{
				
				//if(methodObject.GetParameters().Length == packet.Data.Count)
				if(methodObject.GetParameters().Length == packet.values.Count)
				{
					// create an object vector to be sent when invoking the method as the parameters
					object[] typeObject = new object[methodObject.GetParameters().Length]; // the size of the required arguments
					ParameterInfo[] parameters;
					
					parameters = methodObject.GetParameters();
					
					int x = 0;
					bool checkTypes = true;		
					foreach (ParameterInfo parameter in parameters)
					{
						// for comparing the target method input types with the incoming OSC data types
						if(parameter.ParameterType != packet.values[x].GetType() && parameter.ParameterType != typeof(bool)) checkTypes = false;
						
						// Bool is a special case because it arrives in 0's and 1's and should be converted to true and false
						if(parameter.ParameterType == typeof(bool))
						{
							if(packet.values[0].GetType().Name == "Int32")
							{
								typeObject[x] = (packet.GetInt(0) == 1) ? true : false;
							} else if(packet.values[0].GetType().Name == "Single")
							{
								typeObject[x] = (packet.GetFloat(0) == 1) ? true : false;
							}
							
						} else
						{
							typeObject[x] = packet.values[x]; 
						}
						x++;
					}
					
					
					
					if(checkTypes)
					{
						if(methodObject.IsStatic)
							methodObject.Invoke(null, typeObject);
						else
							methodObject.Invoke(this, typeObject);
						
					}
				} else
				{
					
					// for now just for BlendShapes
					if(methodObject.Name == "SetBlendShapeWeight") 
					{
						
						object[] typeObject = new object[methodObject.GetParameters().Length]; // the size of the required arguments
						//						ParameterInfo[] parameters;
						Type[] objectType = new Type[2];
						
						typeObject[0] = (int)0;
						typeObject[1] = (float)3;
						
						objectType[0] = typeof(int);
						objectType[1] = typeof(float);

						if(!enableMapping)
						{
							if(meshTemp != null) meshTemp.SetBlendShapeWeight(_extra,packet.GetFloat(0));
						} else {
							if(meshTemp != null) meshTemp.SetBlendShapeWeight(_extra,Mathf.Lerp(minRange[_extra],maxRange[_extra],packet.GetFloat(0)));

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
	void OnEnable() {}


	/// <summary>
	/// Updates the processing
	/// </summary>
	void Update () {


		if (RCPortInPort != null) {
			// verify if its enable

			if(RCPortInPort.enabled)
			{
				// FOR FUTURE IMPLEMENTATION
			}

		}
		int receivedMessage = 0;
		float floatMessage = 0;

		if (propertyObject != null)
		{
			// FOR RECEIVING

			if(receivedMessage > 0)
			{
				// floatMessage
				propertyObject.SetValue(objectComponent,floatMessage,new object[]{});
	
			}
		}
	}


}
