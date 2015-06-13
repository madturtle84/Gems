using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gem : MonoBehaviour {

	/* Public Properties (Getter/Setter) */
	public GemType gemType { get; set; }
	public Grid grid { get; set; }
	public GemGroup group { get; set; }
	public int x { 
		get {
			return grid.x;
		} 
	}
	public int y {
		get {
			return grid.y;
		}
	}
	/* Private Fields */
	private Vector3 dragPos;
	private float boardZ= 0; // Initial board's z-axis
	private bool pickedMode = false;
	private int layerOrderNormal = 1;
	private int layerOrderPicked = 2;
	private SpriteRenderer sprRenderer;

	/* Static Stuff */
	static private int numGemType = 5;
	static private List<float> preCalculatedFallingDistances = new List<float>();


	void Awake() {
		/* Initialize variables */
		sprRenderer = GetComponent<SpriteRenderer>();
		/* Created with Random Type */
		ChangeToRandomType();
		//ChangeGemType((GemType) Random.Range(0, numGemType));

		/* Pre-calculate falling data */
		if (preCalculatedFallingDistances.Count == 0) {
			for (float t = 0; t < 3f; t += Time.fixedDeltaTime) {
				preCalculatedFallingDistances.Add(0.5f * 50 * t*t);
			}
		}
	}

	void Update() {
		if (pickedMode) {
			FollowCursor();
		}

		/* Debugging */
		if (testGrid && Input.GetKeyDown(KeyCode.Space)) {
			FallDownTo(testGrid.transform.position.y);
		}
	}

	void FollowCursor() {
		/* Set Piece's Position to mouse's position in world coordinate */
		dragPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		dragPos.z = boardZ;
		transform.position = dragPos;		
	}
	/* Start following cursor */
	public void PickUp() {
		pickedMode = true;
		sprRenderer.sortingOrder = layerOrderPicked;
	}
	
	/// <summary>
	/// Stop following cursor.
	/// </summary>
	public void Drop() {
		pickedMode = false;
		sprRenderer.sortingOrder = layerOrderNormal;
	}

	public void FallDownTo(float groundY) {
		//StartCoroutine(FallDownCoroutine(groundY));
		StartCoroutine(FallDownCoroutineCheap(groundY));		
	}

	private float gravity = 5f;
	private float currentSpeed = 0;
	private float bouncingSpeedModifier = 0.5f;
	private float speedThreshold = 1f;
	public Grid testGrid;
	private IEnumerator FallDownCoroutine(float groundY) {
		speedThreshold = gravity * bouncingSpeedModifier;
		int timeout = 300;
		Vector2 finalPosition = transform.position;
		finalPosition.y = groundY;
		do {
			currentSpeed -= gravity;
			//distanceToGround = transform.position.y - groundY;
			/* Bouncing */
			//transform.Translate(currentSpeed * Vector3.up * Time.deltaTime);
			transform.Translate(currentSpeed * Vector3.up * Time.fixedDeltaTime);
			if (transform.position.y < groundY) {
				transform.position = finalPosition;
				currentSpeed = Mathf.Abs(currentSpeed * bouncingSpeedModifier);
			}
			//yield return new WaitForEndOfFrame();
			yield return new WaitForFixedUpdate();

			/* Debugging */
			if (timeout-- < 0) {
				print("Current Speed = " + currentSpeed);
			}
		} while (Mathf.Abs(currentSpeed) > speedThreshold );
		
		transform.position = finalPosition;
	}
	private IEnumerator FallDownCoroutineCheap(float groundY) {
		Vector3 currentPos = transform.position;
		float startY = currentPos.y;
		int frameCount = 0;
		while (currentPos.y > groundY) {
			transform.position = currentPos;
			currentPos.y = startY - preCalculatedFallingDistances[frameCount];
			frameCount++;			
			yield return new WaitForFixedUpdate();
		}
		currentPos.y = groundY;
		transform.position = currentPos;		
	}

	public void ChangeToRandomType() {
		ChangeGemType((GemType)Random.Range(0, numGemType));
	}

	public void ChangeGemType(GemType newType) {
		if (sprRenderer == null) {
			sprRenderer = GetComponent<SpriteRenderer>();
			print("WHAT????");
		}
		gemType = newType;		
		/* Change Appearance */
		switch (gemType) {
			case GemType.Fire:
				sprRenderer.sprite=	Resources.Load<Sprite>("GemTextures/fire");
				break;
			case GemType.Water:
				sprRenderer.sprite = Resources.Load<Sprite>("GemTextures/water");
				break;
			case GemType.Dark:
				sprRenderer.sprite = Resources.Load<Sprite>("GemTextures/dark");
				break;
			case GemType.Light:
				sprRenderer.sprite = Resources.Load<Sprite>("GemTextures/light");
				break;
			case GemType.Earth:
				sprRenderer.sprite = Resources.Load<Sprite>("GemTextures/earth");
				break;
		}
	}
	public void ChangeGemType(char tp) {
		GemType gemType=GemType.Dark;
		if (tp == 'b') gemType = GemType.Water;
		else if (tp == 'r') gemType = GemType.Fire;
		else if (tp == 'g') gemType = GemType.Earth;
		else if (tp == 'y') gemType = GemType.Light;
		else if (tp == 'd') gemType = GemType.Dark;
		ChangeGemType(gemType);
	}
}

public enum GemType {Fire, Water, Earth, Light, Dark}