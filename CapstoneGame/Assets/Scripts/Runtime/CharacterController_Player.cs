using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Adam B.
// Script to control player character, including general movement and collision
public class CharacterController_Player : MonoBehaviour
{
    // Physics variables
    Rigidbody rb;
    CapsuleCollider capsule;
    float gravityFac = 9.81f;
    Ray groundRay;
    RaycastHit groundRayHit;
    public Transform groundRayBase;

    // Layer masks involved in character movement
    LayerMask floorLayer;

    // Variables for movement control factors
    Vector3 moveIntent, moveVector;
    public float moveSpeed = 1, jumpPower = 2;
    bool isGrounded = false, isJumping = false, isLocked = false;

    // Variables for melee attack (Note: for the melee attack to do anything, other objects will have to check for hurtboxes on the "hitbox" physics layer later on)
    public Collider meleeHurtbox;
    bool isMeleeing = false;
    float meleeStartTime;
    Quaternion meleeAngle;
    public float meleeDuration = .5f, meleeCooldown = .5f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        capsule = GetComponent<CapsuleCollider>();
        
        floorLayer = LayerMask.NameToLayer("Floor");

        meleeHurtbox.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Check player inputs
        moveIntent = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if(Input.GetButtonDown("Jump"))
        {
            isJumping = true;
        }
        
        if (Input.GetButtonDown("Fire1"))
        {
            isMeleeing = true;
        }
    }

    void FixedUpdate()
    {
        // Ground ray is drawn a little bit above player character's foot
        groundRay = new Ray(groundRayBase.position + Vector3.up * 0.5f, Vector3.down);
        
        // Calculate the angle of the normal that the player is standing on (angle rounded to nearest significant digit; for use in later functionality)
        float groundAngle = Mathf.Round(Vector3.Angle(groundRayHit.normal, groundRay.direction));

        // Check if ground collision is below
        // Added conditional so ground collision is only checked when moving down (velocity-based solution might be better later)
        if (Physics.Raycast(groundRay, out groundRayHit, 1f, 1 << floorLayer, QueryTriggerInteraction.Ignore) && rb.velocity.y <= 0)
        {
            // Set grounded flag on
            isGrounded = true;

            // Readout for angle of normal that is currently stood on
            Debug.Log("Hit!" + ", " + groundAngle % 60 + ", " + groundAngle);
            
            // Move character by distance of ground collision so that they are standing on top of the ground collision point
            rb.position = new Vector3(transform.position.x, groundRayHit.point.y + 1, transform.position.z);
            
            // Draw red line where ground detection is being checked (collision has already happened)
            Debug.DrawRay(groundRayBase.position + Vector3.up * 0.5f, groundRay.direction, Color.red);

            // Movement vector is reset
            moveVector = Vector3.zero;

            // Player's movement intent is added
            moveVector += moveIntent.normalized * moveSpeed;

            // Update character's facing direction
            if (moveIntent.magnitude > 0)
            {
                rb.MoveRotation(Quaternion.Euler(new Vector3(0, Mathf.Rad2Deg * Mathf.Atan2(rb.velocity.x, rb.velocity.z), 0)));
            }

            // Rudimentary jumping (jumping, in this case, is additive onto current movement velocity)
            if (isJumping)
            {
                print("Jump!");
                moveVector += transform.up * jumpPower;
                isJumping = false;
            }
        }
        else
        {
            // Set grounded flag off
            isGrounded = false;
            
            // Set jumping variable to false to prevent complete jump buffering for after landing (partial jump buffering might be wanted later)
            isJumping = false;

            // Add gravity factor when not standing on a surface
            moveVector += Vector3.down * gravityFac * Time.deltaTime;

            // Cap falling speed at gravity factor per second
            if (moveVector.y < -gravityFac)
            {
                moveVector = new Vector3(moveVector.x, -gravityFac, moveVector.z);
            }

            // Draw red line where ground detection is being checked (collision hasn't happened yet)
            Debug.DrawRay(groundRayBase.position + Vector3.up * 0.5f, groundRay.direction, Color.green);
        }

        // Draw current rotation direction
        Debug.DrawRay(groundRayBase.position, transform.forward, Color.blue);

        // Calculate position character is trying to move to for this physics update
        Vector3 nextPosition = rb.position + moveVector * Time.fixedDeltaTime;

        // If not interrupted by attacking or other effects, move to destination
        if (!isLocked)
        {
            // Apply final movement calculation
            rb.MovePosition(nextPosition);
        }

        // If trying to melee and previous melee attack has finished
        if (isMeleeing && Time.fixedTime > meleeStartTime + meleeDuration + meleeCooldown)
        {
            // Reset variables
            meleeStartTime = Time.fixedTime;
            
            // Get angle to derive the arc the weapon will cover when swiping
            meleeAngle = transform.rotation;

            // Reset input flag
            isMeleeing = false;
            
            // Lock character in place during attack (to allow for hover or other movement effects to take over)
            isLocked = true;

            // Make weapon swipe model/effect appear
            meleeHurtbox.gameObject.SetActive(true);
        }

        // If within duration of melee attack
        if (Time.fixedTime < meleeStartTime + meleeDuration)
        {
            // The angle the weapon will be rotating to for this physics update
            Quaternion tempRot = Quaternion.Euler(0, Mathf.Lerp(-90, 90, (Time.fixedTime - meleeStartTime) / meleeDuration), 0);

            // Rotate weapon based on time since attacking
            meleeHurtbox.gameObject.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(meleeAngle.eulerAngles + tempRot.eulerAngles));
        }
        else
        {
            // Unlock character movement
            isLocked = false;
            
            // Make weapon swipe model/effect disappear
            meleeHurtbox.gameObject.SetActive(false);
        }
    }
}