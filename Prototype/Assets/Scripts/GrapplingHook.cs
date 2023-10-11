using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    //References
    private PlayerMovement playerMovement;
    [SerializeField] public Transform cam;
    [SerializeField] public Transform gunTip;
    [SerializeField] public LayerMask grapplingLayer;
    [SerializeField] public LineRenderer lineRenderer;

    //Grappling
    [SerializeField] public float maxGrappleDistance;
    [SerializeField] public float grappleDelayTime;
    [SerializeField] public float overshootYAxis;

    private Vector3 grapplePoint;

    //Grappling Cooldown
    [SerializeField] public float grapplingCooldown;
    [SerializeField] private float grapplingCooldownTimer;

    //Keybind for Grapple
    [SerializeField] public KeyCode grappleKey = KeyCode.Mouse1;
    
    private bool isGrappling;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();  //Get reference to the PlayerMovement script
    }

    private void StartGrapple()
    {
        if(grapplingCooldownTimer > 0) //You should NOT be able to grapple if there is a remaining cooldown!!
        {
            return;
        }

        isGrappling = true;

        playerMovement.freeze = true; //Set to true when I start to grapple

        //Throw the hook
        RaycastHit hit;
        if(Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, grapplingLayer)) //if the ray hits
        {
            grapplePoint = hit.point;

            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else //if the ray does not hit
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance; //set to max distance

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lineRenderer.enabled = true; //enable the line renderer
        lineRenderer.SetPosition(1, grapplePoint); //grapplePoint is the end position of the line renderer
    }

    private void ExecuteGrapple()
    {
        playerMovement.freeze = false; //Set to false when I execute the grapple

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z); //Also a mathematical calculation
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if(grapplePointRelativeYPos < 0)
        {
            highestPointOnArc = overshootYAxis;
        }

        playerMovement.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        playerMovement.freeze = false; //Set to false when I stop the grapple

        isGrappling = false;

        grapplingCooldownTimer = grapplingCooldown;

        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(grappleKey))
        {
            StartGrapple();
        }

        if(grapplingCooldownTimer > 0)
        {
            grapplingCooldownTimer -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (isGrappling)
        {
            lineRenderer.SetPosition(0, gunTip.position);
        }
    }
}
