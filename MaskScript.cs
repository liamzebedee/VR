using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;
using VRTK.GrabAttachMechanics;

[RequireComponent (typeof(VRTK_ControllerEvents), typeof(VRTK_FixedJointGrabAttach)) ]
public class MaskScript : VRTK_InteractableObject {
	public GameObject cameraRig;
	public GameObject leftController;
	public GameObject rightController;
	public Light sceneLighting;

	public Shader cameraShader;
	public string cameraShaderTag;

	bool maskOn = false; // future
	bool maskIsBeingPutOn = false;
	bool maskIsBeingTakenOff = false;

	bool maskOnAndLetGo = false;
	const float MASK_ON_OFF_THRESHOLD_DISTANCE = 0.25f;

	// Use this for initialization
	protected void Start () {
		var events = GetComponent<VRTK_ControllerEvents> ();
		if (events == null)
			throw new Exception ("no events");

		events.TouchpadPressed += logDistance;
	}

	void logDistance(object sender, ControllerInteractionEventArgs e) {
		//Debug.Log (getDistance () + ' ' + IsGrabbed().ToString());
	}

	float getDistance() {
		var headsetPos = VRTK_DeviceFinder.HeadsetTransform ().position;
		float d = Vector3.Distance (headsetPos, this.transform.position);
		return d;
	}

	void ChangeLighting(float to) {
		sceneLighting.intensity = Mathf.SmoothStep (sceneLighting.intensity, to, 0.5f);
	}

	// Update is called once per frame
	protected override void LateUpdate() {
		base.LateUpdate();
		if (maskIsBeingTakenOff) {
			var mat = GetComponent<Renderer> ().material;
			mat.color = new Color (mat.color.r, mat.color.g, mat.color.b, 0.6f + (getDistance () - MASK_ON_OFF_THRESHOLD_DISTANCE));
		} else {
			var mat = GetComponent<Renderer> ().material;
			mat.color = new Color (mat.color.r, mat.color.g, mat.color.b, 1f);
		}

		if(maskIsBeingPutOn || maskIsBeingTakenOff) return; 

		if (!maskOn ) {
			PuttingMaskOn ();
		} else {
			TakingMaskOff ();
		}
	}

	void PuttingMaskOn() {
		if (getDistance () <= MASK_ON_OFF_THRESHOLD_DISTANCE && IsGrabbed ()) {
			maskIsBeingPutOn = true;

			Time.timeScale = 0.5f;
			Camera.main.SetReplacementShader (this.cameraShader, this.cameraShaderTag);
			this.GetComponent<MeshRenderer>().enabled = false;
			rightController.GetComponent<VRTK_InteractGrab> ().ForceRelease ();

			rightController.GetComponent<VRTK_ControllerEvents>().AliasGrabOff += (object sender, ControllerInteractionEventArgs e) => {
				maskOn = true;
				maskIsBeingPutOn = false;
			};
		}
	}

	void TakingMaskOff() {
		var controllerEvents = rightController.GetComponent<VRTK_ControllerEvents>();

		bool notGrabbingSomethingElse = grabbingObjects.Count == 0;

		if (rightController.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(VRTK_ControllerEvents.ButtonAlias.Grip_Press) && notGrabbingSomethingElse) {
			float dRight = Vector3.Distance (VRTK_DeviceFinder.HeadsetTransform ().position, rightController.transform.position);

			if((dRight < MASK_ON_OFF_THRESHOLD_DISTANCE)) {
				maskIsBeingTakenOff = true;

				Time.timeScale = 1f;

				// Put mask in controller's hand and grab it.
				this.transform.position = rightController.transform.position;
				this.transform.rotation = rightController.transform.rotation;
				this.GetComponent<MeshRenderer>().enabled = true;
				rightController.GetComponent<VRTK_InteractGrab> ().AttemptGrab ();
				Camera.main.ResetReplacementShader ();


				rightController.GetComponent<VRTK_ControllerEvents>().AliasGrabOff += (object sender, ControllerInteractionEventArgs e) => {
					maskIsBeingTakenOff = false;

					// If the grip is released but the user still wants to keep it on
					if((Vector3.Distance (VRTK_DeviceFinder.HeadsetTransform ().position, rightController.transform.position) < MASK_ON_OFF_THRESHOLD_DISTANCE)) {
						Time.timeScale = 0.5f;
						Camera.main.SetReplacementShader (this.cameraShader, this.cameraShaderTag);
						this.GetComponent<MeshRenderer>().enabled = false;
						rightController.GetComponent<VRTK_InteractGrab> ().ForceRelease ();
						maskOn = true;
						maskIsBeingPutOn = false;
					} else {
						maskOn = false;
					}
				};
			}
		}
	}
}
