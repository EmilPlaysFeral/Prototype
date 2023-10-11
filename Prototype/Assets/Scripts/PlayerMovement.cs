using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float walkSpeed; //Ta bort om jag inte ska göra sprint plattform till plattform i zombie byn

    [SerializeField] public float playerHeight;
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] public bool isGrounded;
    [SerializeField] public float groundDrag;

    [SerializeField] public float jumpForce;
    [SerializeField] public float jumpCooldown;
    [SerializeField] public float airMultiplier;
    [SerializeField] public bool isReadyToJump;
    
    [SerializeField] public Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private KeyCode jumpKey = KeyCode.Space;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState //enums for my StateMachine attempt
    {
        walking,
        air,
        freeze

    }

    [SerializeField] public bool freeze;
    [SerializeField] public bool activeGrapple;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); //Get the rigidbody and assign it to rb
        rb.freezeRotation = true; //Stop player from falling over
        isReadyToJump = true;
    }

    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && isReadyToJump && isGrounded)
        {
            isReadyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown); //be able to continously jump if holding jump key 
        }
    }

    void Update()
    {
        //Groundcheck
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        //The drag handling
        if (isGrounded && !activeGrapple)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }

        PlayerInput();
        SpeedControl();
        StateHandler();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void StateHandler() //StateMachine 
    {
        //Freeze (the window when the Grappling hook is launched BEFORE pull)
        if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0;
            rb.velocity = Vector3.zero;
        }

        //Walking
        else if (isGrounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        } 
        //Jumping
        else
        {
            state = MovementState.air;
        }

    }

    private void MovePlayer()
    {
        if (activeGrapple) //stop movement while grappling
        {
            return;
        }

        //Calculating the movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (isGrounded) //if you are on the ground
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!isGrounded) //if you are in the air
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        if (activeGrapple) //stop movement while grapplinmg
        {
            return;
        }

        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //Limit the velocity if it goes above what I want
        if(flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }

    }

    private void Jump()
    {
        //First reset the Y velocity (set it to 0) to always jump the same height
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        isReadyToJump = true;
    }

    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight) //Call upon the advanced calculation
    {
        activeGrapple = true;       
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight, 1.5f);
        Invoke(nameof(SetVelocity), 0.1f); //velocity applied after 0,1sce

        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet;
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<GrapplingHook>().StopGrapple();
        }
    }

    //La till forwardDistanceMultiplier för att mixtra med värdena
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight, float forwardDistanceMultiplier) //Sebastian Lague's Complex Mathematical Calculation for calculating how much force is needed to push the player to the grapple point
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);

        //Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));
        Vector3 velocityXZ = displacementXZ * forwardDistanceMultiplier / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity)); //My own touch on the calculation

        return velocityXZ + velocityY;
    }

}
