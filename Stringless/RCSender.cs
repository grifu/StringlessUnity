//  Stringless: [RCSender] Remote Control Transmiter
//  ------------------------------------
//
// 	Outputs OSC packadges from selected parameters of the Gameobject to the network (expose parameters)
//  Version 2.1 Beta
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

/// <summary>
/// Outputs OSC packadges from selected parameters of the Gameobject to the network
/// It broacast any selected property to OSC
/// The only method supported yet is Blendshapes
/// Fields are not supported yet
/// 
/// TODO: This is a working proof of concept it still needs a lot of optimization
/// </summary>
public class RCSender : MonoBehaviour {
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
	public MethodInfo methodObject;
	public FieldInfo fieldObject;
	public string address;
	public OSC OSCtransmitPort;
	public bool sendEveryFrame = false;
	private ObjectRequirements requirements;	// requirments arguments (number and type)
	private object relativeValue;				// keep the original value for relative option
	private float blendShapeTemp = 0;
	private object oldPropertyObject;

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
	void Start () {
		if (propertyObject == null)
		{

			Type typeComponent = objectComponent.GetType();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly  | BindingFlags.Public | 
				BindingFlags.Instance | BindingFlags.Static;
			PropertyInfo[] properties = typeComponent.GetProperties(flags);
			if(properties.Length > _generalIndex) 
			{
				propertyObject = properties[_generalIndex];
				oldPropertyObject = propertyObject.GetValue(objectComponent,null);
			}
		}

		
		if (methodObject == null)
		{
			Type typeComponent = objectComponent.GetType();
			const BindingFlags flags = /*BindingFlags.NonPublic | */ BindingFlags.DeclaredOnly  | BindingFlags.Public | 
				BindingFlags.Instance | BindingFlags.Static;
			MethodInfo[] methods = typeComponent.GetMethods(flags);	
			if(methods.Length > _generalIndex) methodObject = methods[_generalIndex];
		}

		OSC[] instancesOSC;
		instancesOSC = FindObjectsOfType (typeof(OSC)) as OSC[];
		GameObject oscGameobject;
		oscGameobject = this.gameObject;
		if (instancesOSC.Length > 0)
		{
			//i = 0;
			foreach(OSC item in instancesOSC)
			{
				if((OSCtransmitPort != null))
				{
					if(item.getPortOUT() == OSCtransmitPort.getPortOUT())
					{
						oscGameobject = item.gameObject;
						oscReference = oscGameobject.GetComponent<OSC> ();
					}
				}
			}

			
		}else 
		{
			Debug.Log ("NO OSC!!! You have to drag a OSC behavior to initalize the plugin");
			
			
		}

		requirements = new ObjectRequirements();
	}


	/// <summary>
	/// Updates the processing
	/// </summary>
	
	void Update () {


		// properties
		if(_controlIndex == 0)
		{
			// check if we have different incoming values to send or if we want to send every frames
			if(propertyObject.GetValue(objectComponent,null).Equals(oldPropertyObject) && !sendEveryFrame)
			{
			} else
			{
				oldPropertyObject = propertyObject.GetValue(objectComponent,null);
				ArrayList objectList = new ArrayList();
				objectList = listConverted(propertyObject);

				if((objectList != null) && (oscReference != null))
				{

					OscMessage messageOSC = new OscMessage();
					messageOSC.address = address;
					messageOSC.values = objectList;
					oscReference.Send(messageOSC);
				}

			}

		} else if(_controlIndex == 1)	// method sending not include yet
		{
						if (methodObject != null) {
								// for now its just for blendhshapes
								// TODO: create a generic method for sending all the data that derives from methodObject
								if (methodObject.Name == "GetBlendShapeWeight") {
										GameObject objectTemp = this.gameObject;
										SkinnedMeshRenderer meshTemp = objectTemp.GetComponent<SkinnedMeshRenderer> ();

										ArrayList objectList = new ArrayList ();
										objectList.Add (meshTemp.GetBlendShapeWeight (_extra));

										if (blendShapeTemp != meshTemp.GetBlendShapeWeight (_extra)) {

												OscMessage messageOSC = new OscMessage ();
												messageOSC.address = address;
												messageOSC.values = objectList;
												oscReference.Send (messageOSC);
												blendShapeTemp = meshTemp.GetBlendShapeWeight (_extra);

										}

								} else {
										/* Future Implementation
										ParameterInfo[] parameters = methodObject.GetParameters ();
										*/
								}
						}
			}else if(_controlIndex == 2)	// fields sending not include yet
				{
					//	if (oldFieldObject != null) print ("valor = "+ oldFieldObject.GetValue(objectComponent));
					if (fieldObject != null) 
					{
								

								// check if we have different incoming values to send or if we want to send every frames
								if (fieldObject.GetValue (objectComponent).Equals (oldPropertyObject) && !sendEveryFrame) {
								} else {
										oldPropertyObject = fieldObject.GetValue(objectComponent);

										ArrayList objectList = new ArrayList();
										objectList = listConvertedField(fieldObject);

										if ((objectList != null) && (oscReference != null)) {

												OscMessage messageOSC = new OscMessage ();
												messageOSC.address = address;
												messageOSC.values = objectList;
												oscReference.Send (messageOSC);
										}


								}

					}

				}				
	}


	

	/// <summary>
	/// Convert propertyInfo to an Arraylist
	/// </summary>
	/// <param name="propObject">
	/// A <see cref="PropertyInfo"/>
	/// </param>
	/// <returns>
	/// A <see cref="List<object>"/>
	/// </returns>
	/// 
	/// 
	ArrayList listConverted(PropertyInfo propObject)
	{
		
		// Add to the list the correct sequence of values
		// TODO: This should be optimized to convert.changetype

		ArrayList objectValuesToSend = new ArrayList(); 
		if(propObject.GetValue(objectComponent,null) != null) 
		{
			Type objectType;
			objectType = propObject.GetValue (objectComponent, null).GetType ();

			if(objectType == typeof(Vector2))
			{
				for(int x=0;x<2;x++)
					objectValuesToSend.Add(((Vector2)propObject.GetValue(objectComponent,null))[x]);
				
			} else if(objectType == typeof(Vector3))
			{	
				for(int x=0;x<3;x++)
					objectValuesToSend.Add(((Vector3)propObject.GetValue(objectComponent,null))[x]);
				
			} else if(objectType == typeof(Vector4))
			{
				for(int x=0;x<4;x++)
					objectValuesToSend.Add(((Vector4)propObject.GetValue(objectComponent,null))[x]);	
			} else if(objectType == typeof(Boolean))
			{
				objectValuesToSend.Add( ((bool)propObject.GetValue(objectComponent,null) == true) ? 1 : 0);//
			} else if(objectType == typeof(Single))
			{
				objectValuesToSend.Add( ((float)propObject.GetValue(objectComponent,null)));
			} else if(objectType == typeof(Transform))	// send all data from transform
			{
				Transform tempTransform;
				tempTransform = (Transform)propObject.GetValue(objectComponent,null);
				
				for(int x=0;x<3;x++)
					objectValuesToSend.Add((float)tempTransform.position[x]);
				for(int x=0;x<3;x++)
					objectValuesToSend.Add((float)tempTransform.localScale[x]);
				for(int x=0;x<4;x++)
					objectValuesToSend.Add((float)tempTransform.rotation[x]);
			} else if(objectType == typeof(Matrix4x4))
			{
				for(int y=0;y<3;y++)
				{
					for(int x=0;x<3;x++)
					{
						float tempFloat = ((Matrix4x4)propObject.GetValue(objectComponent,null))[x,y];
						objectValuesToSend.Add(tempFloat);
					}
				}
				
			} else if(objectType == typeof(int))
			{
				objectValuesToSend.Add((int)propObject.GetValue(objectComponent,null));
				
			} else if(objectType == typeof(Quaternion))
			{
				for(int x=0;x<4;x++)
					objectValuesToSend.Add(((Quaternion)propObject.GetValue(objectComponent,null))[x]);
				
			} else
			{
				
				objectValuesToSend.Add((string)propObject.GetValue(objectComponent,null));
				
			}
			
			
		} 
		return objectValuesToSend;
	}

		// TODO: fuse this two methods into one
		/// <summary>
		/// Convert FieldInfo to an Arraylist
		/// </summary>
		/// <param name="propObject">
		/// A <see cref="PropertyInfo"/>
		/// </param>
		/// <returns>
		/// A <see cref="List<object>"/>
		/// </returns>
		/// 
		/// 
		ArrayList listConvertedField(FieldInfo propObject)
		{

				// Add to the list the correct sequence of values
				// TODO: This should be optimized to convert.changetype

				ArrayList objectValuesToSend = new ArrayList(); 
				if(propObject.GetValue(objectComponent) != null) 
				{
						Type objectType;

						objectType = fieldObject.FieldType;

						if(objectType == typeof(Vector2))
						{
								for(int x=0;x<2;x++)
										objectValuesToSend.Add(((Vector2)propObject.GetValue(objectComponent))[x]);
								
		
						} else if(objectType == typeof(Vector3))
						{	
								for(int x=0;x<3;x++)
										objectValuesToSend.Add(((Vector3)propObject.GetValue(objectComponent))[x]);

						} else if(objectType == typeof(Vector4))
						{
								for(int x=0;x<4;x++)
										objectValuesToSend.Add(((Vector4)propObject.GetValue(objectComponent))[x]);	
						} else if(objectType == typeof(Boolean))
						{
								objectValuesToSend.Add( ((bool)propObject.GetValue(objectComponent) == true) ? 1 : 0);
						} else if(objectType == typeof(Single))
						{
								objectValuesToSend.Add( ((float)propObject.GetValue(objectComponent)));
						} else if(objectType == typeof(Transform))	// send all data from transform
						{
								Transform tempTransform;
								tempTransform = (Transform)propObject.GetValue(objectComponent);

								for(int x=0;x<3;x++)
										objectValuesToSend.Add((float)tempTransform.position[x]);
								for(int x=0;x<3;x++)
										objectValuesToSend.Add((float)tempTransform.localScale[x]);
								for(int x=0;x<4;x++)
										objectValuesToSend.Add((float)tempTransform.rotation[x]);
						} else if(objectType == typeof(Matrix4x4))
						{
								for(int y=0;y<3;y++)
								{
										for(int x=0;x<3;x++)
										{
												float tempFloat = ((Matrix4x4)propObject.GetValue(objectComponent))[x,y];
												objectValuesToSend.Add(tempFloat);
										}
								}

						} else if(objectType == typeof(int))
						{
								objectValuesToSend.Add((int)propObject.GetValue(objectComponent));

						} else if(objectType == typeof(Collision))
						{
								Collision testCollision;
								testCollision = (Collision)propObject.GetValue (objectComponent);


								objectValuesToSend.Add((int)testCollision.contacts.Length);



						} else if(objectType == typeof(Quaternion))
						{
								for(int x=0;x<4;x++)
										objectValuesToSend.Add(((Quaternion)propObject.GetValue(objectComponent))[x]);

						} else
						{

								objectValuesToSend.Add((string)propObject.GetValue(objectComponent));

						}



				} 
				return objectValuesToSend;
		}



}




