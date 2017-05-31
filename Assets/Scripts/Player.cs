using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	
	public bool facingRight = true;
    public bool jump = false;
    public float moveForce = 365f;
    public float maxSpeed = 5f;
    public float jumpForce = 1000f;
    public Transform groundCheck;
							
							
    private bool grounded = false;
    private Rigidbody2D rb2d;
	private NetworkView nView;

	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

									
    // Use this for initialization
    void Awake() {
		rb2d = GetComponent<Rigidbody2D>();
		nView = GetComponent<NetworkView>();
    }
	                       
	// Update is called once per frame
	void Update() {
		if (nView.isMine){
			InputMovement();
		}else{
			SyncedMovement();
		}		
	}

	void InputMovement(){
		grounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));
		if (Input.GetButtonDown("Jump") && grounded){
			jump = true;
		}

		float h = Input.GetAxis("Horizontal");
	    if (h * rb2d.velocity.x < maxSpeed){
		    rb2d.AddForce(Vector2.right * h * moveForce);
		}                                                                                 if (Mathf.Abs (rb2d.velocity.x) > maxSpeed){
			rb2d.velocity = new Vector2(Mathf.Sign (rb2d.velocity.x) * maxSpeed, rb2d.velocity.y);
		}
		if (h > 0 && !facingRight){
			Flip();
		}else if (h < 0 && facingRight){
		    Flip();
		}
	    if (jump){
		    rb2d.AddForce(new Vector2(0f,jumpForce));
		    jump = false;
		}
	}	
		
	private void SyncedMovement(){
		syncTime += Time.deltaTime;
		rb2d.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
	}

	void Flip(){
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info){
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		if (stream.isWriting){
			syncPosition = rb2d.position;
			stream.Serialize(ref syncPosition);

			syncVelocity = rb2d.velocity;
			stream.Serialize(ref syncVelocity);
		}else{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;

			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rb2d.position;
		}
	}
}
