using UnityEngine;
using System.Collections;

public class collision : MonoBehaviour {

		public bool trigger;
	


	// Use this for initialization
	void Start () {
				trigger = false;

	}
	

		void OnCollisionEnter(Collision collisionInfo)
		{

				print("Colisão entre " + gameObject.name + " e " + collisionInfo.collider.name);
				print("Existem " + collisionInfo.contacts.Length + " ponto(s) de contacto");
				print("A sua velocidade relativa é " + collisionInfo.relativeVelocity);
				trigger = true;

		}

		void OnCollisionStay(Collision collisionInfo)
		{
				print(gameObject.name + " e " + collisionInfo.collider.name + " ainda estão em colisão");
		}

		void OnCollisionExit(Collision collisionInfo)
		{
				print(gameObject.name + " e " + collisionInfo.collider.name + " já não colidem");
				trigger = false;
		}

}
