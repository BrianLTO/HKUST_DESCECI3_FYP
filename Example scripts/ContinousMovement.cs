using Oculus.Interaction;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ContinousMovement : MonoBehaviour
{
    public static ContinousMovement instance;
    void Awake()
    {
        //singleton script
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    [Header("Fixed variable")]
    public OVRSkeleton handLeft;
    public OVRSkeleton handRight;
    public LayerMask groundLayer;
    public GameObject handLeftVisual, handLeftHP, handLeftCast;
    public GameObject handRightVisual, handRightHP, handRightCast;

    [Header("Assign hand poses")]
    public ActiveStateSelector poseLeft;
    public ActiveStateSelector poseRight;
    public ActiveStateSelector poseLeftJump;
    public ActiveStateSelector poseRightJump;
    public ActiveStateSelector poseLeftBack;
    public ActiveStateSelector poseRightBack;

    [Header("Thresholds")]
    [Range(0, 0.05f)] public float jumpThreshold = 0.025f;
    [Range(0, 50)] public float jumpAngleThreshold = 35f;
    [Range(0, 1)] public float handVisualThreshold = 0.5f;
    [Range(0, 0.5f)] public float invalidTrackingThreshold = 0.1f; //for detecting hand tracking lost

    [Header("Settings")]
    [Range(0, 1000)] public float speed = 50;
    [Range(0, 20)] public float maxSpeed = 2f;
    [Range(0, 2)] public float jumpheight = 1f;
    [Range(0, 100)] public float smoothingInputClamp = 5f;
    [Range(0, 5)] public float smoothingRamp = 0.5f;
    [Range(0, 20)] public float smoothRampBase = 1.5f;
    [Range(0, 120)] public int speedSmoothing = 30;
    [Range(0, 120)] public int directionSmoothing = 5;
    public float gravity = -9.81f;
    public float additionalHeight = 0.01f;
    public float groundSnapHeight = 0.01f;
    public bool canMove = true;

    public static OVRCameraRig rig;

    private CharacterController character;
    private bool leftTrigger = false;
    private bool rightTrigger = false;
    private bool leftTriggerJump = false;
    private bool rightTriggerJump = false;
    private bool leftTriggerBack = false;
    private bool rightTriggerBack = false;
    private bool isJump = false;
    private bool canJump = false;
    private bool isFullBack = false;
    private bool isFullBackLast = false;
    private Vector3 handLeftLastPosition;
    private Vector3 handRightLastPosition;
    private Vector3 lastRawMovementVector;
    private Vector2 rawMovementVector; //movement without taking account of camera direciton
    private List<float> magnitudeList = new List<float>();
    private List<Vector3> movementList = new List<Vector3>();
    private float fallingSpeed = 0;

    // Start is called before the first frame update
    void Start()
    {
        character = GetComponent<CharacterController>();
        poseLeft.WhenSelected += () => { leftTrigger = true; };
        poseLeft.WhenUnselected += () => { leftTrigger = false; };
        poseRight.WhenSelected += () => { rightTrigger = true; };
        poseRight.WhenUnselected += () => { rightTrigger = false; };
        poseLeftJump.WhenSelected += () => { leftTriggerJump = true; };
        poseLeftJump.WhenUnselected += () => { leftTriggerJump = false; };
        poseRightJump.WhenSelected += () => { rightTriggerJump = true; };
        poseRightJump.WhenUnselected += () => { rightTriggerJump = false; };
        poseLeftBack.WhenSelected += () => { leftTriggerBack = true; };
        poseLeftBack.WhenUnselected += () => { leftTriggerBack = false; };
        poseRightBack.WhenSelected += () => { rightTriggerBack = true; };
        poseRightBack.WhenUnselected += () => { rightTriggerBack = false; };
        rig = GetComponent<OVRCameraRig>();
    }

    // Update is called once per frame
    void Update()
    {
        rawMovementVector = GetMovementVector(handRight.Bones[0].Transform, handLeft.Bones[0].Transform);
        isJump = IsJumping(handRight.Bones[0].Transform, handLeft.Bones[0].Transform);
        handLeftLastPosition = handLeft.Bones[0].Transform.position;
        handRightLastPosition = handRight.Bones[0].Transform.position;
        //characterLastPosition = transform.InverseTransformPoint(rig.centerEyeAnchor.position);
    }

    private void FixedUpdate()
    {
        //reset smoothed magnitude if general movement direction is changed
        if (isFullBackLast != isFullBack)
        {
            ClearSmoothedMagnitude();
            isFullBackLast = isFullBack;
        }

        //puts collision capsule on player
        CapsuleFollowHeadset();

        //getting correct 2d movement vector
        Quaternion cq = Camera.main.transform.rotation;
        float cameraYaw = Mathf.Rad2Deg * Mathf.Atan2(2 * cq.y * cq.w - 2 * cq.x * cq.z, 1 - 2 * cq.y * cq.y - 2 * cq.z * cq.z);

        Vector3 actualMovementVector;
        if (rawMovementVector.magnitude > Mathf.Epsilon)
        {
            //player is moving
            actualMovementVector = new Vector3(rawMovementVector.x, 0, rawMovementVector.y);
            actualMovementVector = Quaternion.Euler(0, cameraYaw, 0) * actualMovementVector;
            actualMovementVector = actualMovementVector.normalized * GetSmoothedMagnitude(actualMovementVector.magnitude);
        }
        else
        {
            //player stoped moving
            actualMovementVector = new Vector3(lastRawMovementVector.x, 0, lastRawMovementVector.y);
            actualMovementVector = Quaternion.Euler(0, cameraYaw, 0) * actualMovementVector;
            actualMovementVector = actualMovementVector.normalized * GetSmoothedMagnitude(0);
        }

        actualMovementVector = Vector3.ClampMagnitude(actualMovementVector, maxSpeed);
        if (actualMovementVector.magnitude > handVisualThreshold)
        {
            if (leftTrigger || leftTriggerBack) ShowHandLeftElements(false);
            else ShowHandLeftElements(true);
            if (rightTrigger || rightTriggerBack) ShowHandRightElements(false);
            else ShowHandRightElements(true);
        }
        else
        {
            ShowHandLeftElements(true);
            ShowHandRightElements(true);
        }

        //move character horizontally and update old vector
        Vector3 movementDiff = character.transform.position;
        character.Move(actualMovementVector * Time.fixedDeltaTime);
        movementDiff = character.transform.position - movementDiff;
        handLeftLastPosition += movementDiff;
        handRightLastPosition += movementDiff;
        //handLeftLastPosition += actualMovementVector * Time.fixedDeltaTime;
        //handRightLastPosition += actualMovementVector * Time.fixedDeltaTime;

        //calculating vertical movement
        if (CheckIfGrounded())
        {
            if (fallingSpeed <= 0)
            {
                fallingSpeed = 0;
                canJump = true;
            }
            if (isJump && canJump)
            {
                fallingSpeed = GetJumpAcceleration(jumpheight);
                canJump = false;
            }
        }
        else
        {
            fallingSpeed += gravity * Time.fixedDeltaTime;
        }


        if (fallingSpeed >= -1f && fallingSpeed <= 0)
        {
            //snap character to ground
            Vector3 rayStart = transform.TransformPoint(character.center);
            float rayLength = character.center.y + groundSnapHeight;
            bool hasHit = Physics.SphereCast(rayStart, character.radius, Vector3.down, out RaycastHit hitInfo, rayLength, groundLayer);
            if (hasHit)
            {
                character.Move(Vector3.up * -groundSnapHeight);
            }
        }

        //apply vertical movement
        character.Move(Vector3.up * Time.fixedDeltaTime * fallingSpeed);
        handLeftLastPosition += Vector3.up * Time.fixedDeltaTime * fallingSpeed;
        handRightLastPosition += Vector3.up * Time.fixedDeltaTime * fallingSpeed;
        //movementDiff = character.transform.position;
        //movementDiff = character.transform.position - movementDiff;
        //handLeftLastPosition += movementDiff;
        //handRightLastPosition += movementDiff;
    }

    private Vector2 GetMovementVector(Transform handTransformRight, Transform handTransformLeft)
    {
        Vector3 projectedHandVectorRight = new Vector3();
        Vector3 previousProjectedHandVectorRight = new Vector3();
        Vector3 projectedHandVectorLeft = new Vector3();
        Vector3 previousProjectedHandVectorLeft = new Vector3();
        Vector3 normalPlane = Camera.main.transform.right;
        normalPlane.y = 0;
        Vector2 movementVectorRight = new Vector2(0, 0);
        Vector2 movementVectorLeft = new Vector2(0, 0);

        bool bothMoveBack = true;

        //LEFT thumb pointing right 180 . thumb left 0
        //RIGHT thumb pointing right 180 . thumb left 0

        //RIGHT pointing left -180, right 0
        Quaternion cq = rig.centerEyeAnchor.rotation;
        float cameraYaw = Mathf.Rad2Deg * Mathf.Atan2(2 * cq.y * cq.w - 2 * cq.x * cq.z, 1 - 2 * cq.y * cq.y - 2 * cq.z * cq.z);


        if (rightTrigger || rightTriggerBack)
        {
            //calculate projected distance
            projectedHandVectorRight = Vector3.ProjectOnPlane(handTransformRight.position, normalPlane);
            previousProjectedHandVectorRight = Vector3.ProjectOnPlane(handRightLastPosition, normalPlane);
            float distanceRight = Vector3.Distance(projectedHandVectorRight, previousProjectedHandVectorRight);

            //calculate hand rotations
            Quaternion hq = handTransformRight.transform.rotation;
            float handRollRight = Mathf.Rad2Deg * Mathf.Atan2(2 * hq.x * hq.w - 2 * hq.y * hq.z, 1 - 2 * hq.x * hq.x - 2 * hq.z * hq.z);
            float handYawRight = Mathf.Rad2Deg * Mathf.Atan2(2 * hq.y * hq.w - 2 * hq.x * hq.z, 1 - 2 * hq.y * hq.y - 2 * hq.z * hq.z);
            float handPitchRight = Mathf.Rad2Deg * Mathf.Asin(2 * hq.x * hq.y + 2 * hq.z * hq.w);

            //calculate rotation degrees
            float rotateDegreeRight = -(90 + handYawRight - cameraYaw) * Mathf.Deg2Rad;
            
            //calculate movment vector
            movementVectorRight = new Vector2(-distanceRight * Mathf.Sin(rotateDegreeRight), distanceRight * Mathf.Cos(rotateDegreeRight)) * speed;
            if (rightTriggerBack) movementVectorRight = new Vector2(0, -movementVectorRight.magnitude / 2);
            else bothMoveBack = false;

            //if location difference is too big (tracking lost)
            if (distanceRight > invalidTrackingThreshold) movementVectorRight = new Vector2(0f, 0f);
        }

        normalPlane *= -1;

        if (leftTrigger || leftTriggerBack)
        {
            //calculate projected distance
            projectedHandVectorLeft = Vector3.ProjectOnPlane(handTransformLeft.position, normalPlane);
            previousProjectedHandVectorLeft = Vector3.ProjectOnPlane(handLeftLastPosition, normalPlane);
            float distanceLeft = Vector3.Distance(projectedHandVectorLeft, previousProjectedHandVectorLeft);

            //calculate hand rotations
            Quaternion hq = handTransformLeft.transform.rotation;
            float handRollLeft = Mathf.Rad2Deg * Mathf.Atan2(2 * hq.x * hq.w - 2 * hq.y * hq.z, 1 - 2 * hq.x * hq.x - 2 * hq.z * hq.z);
            float handYawLeft = Mathf.Rad2Deg * Mathf.Atan2(2 * hq.y * hq.w - 2 * hq.x * hq.z, 1 - 2 * hq.y * hq.y - 2 * hq.z * hq.z);
            float handPitchLeft = Mathf.Rad2Deg * Mathf.Asin(2 * hq.x * hq.y + 2 * hq.z * hq.w); ;

            //calculate rotation degrees
            float rotateDegreeLeft = (90 - handYawLeft + cameraYaw) * Mathf.Deg2Rad;

            //calculate movment vector
            movementVectorLeft = new Vector2(-distanceLeft * Mathf.Sin(rotateDegreeLeft), distanceLeft * Mathf.Cos(rotateDegreeLeft)) * speed;
            if (leftTriggerBack) movementVectorLeft = new Vector2(0, -movementVectorLeft.magnitude / 2);
            else bothMoveBack = false;

            if (distanceLeft > invalidTrackingThreshold) movementVectorLeft = new Vector2(0f, 0f);
        }

        if (rightTrigger || rightTriggerBack || leftTrigger || leftTriggerBack) isFullBack = bothMoveBack;

        Vector2 totalMovement = movementVectorLeft + movementVectorRight;
        if (totalMovement.magnitude > Mathf.Epsilon && canMove) lastRawMovementVector = totalMovement;
        if (canMove) return movementVectorLeft + movementVectorRight;
        else return new Vector2(0f, 0f);

        /* old code */
        //if (rightTrigger && leftTrigger)
        //{
        //    if (Vector3.Angle(previousProjectedHandVectorLeft - projectedHandVectorLeft, previousProjectedHandVectorRight - projectedHandVectorRight) < swingAngle)
        //        return new Vector2(0, 0);
        //}

        //if (rightTrigger && leftTrigger) averagePitch /= 2;

        //if (!(leftTriggerBack && rightTriggerBack))
        //{
        //    //moving forwards
        //    Vector2 totalMovement = movementVectorLeft + movementVectorRight;
        //    if (totalMovement.magnitude > Mathf.Epsilon) lastRawMovementVector = totalMovement;
        //    return movementVectorLeft + movementVectorRight;
        //}
        //else
        //{
        //    //moving backwards
        //    return new Vector2(0, -(movementVectorLeft.magnitude + movementVectorRight.magnitude) / 2);
        //}

    }

    private bool IsJumping(Transform handTransformRight, Transform handTransformLeft)
    {
        bool leftJump = true, rightJump = true;
        if (rightTriggerJump)
        {
            Vector3 vectorRight = handTransformRight.position - handRightLastPosition;

            if (Vector3.Angle(vectorRight, Vector3.up) > jumpAngleThreshold) rightJump = false;
            if (Vector3.Project(vectorRight, Vector3.up).magnitude < jumpThreshold || Vector3.Project(vectorRight, Vector3.up).magnitude > invalidTrackingThreshold) rightJump = false;
        }
        else rightJump = false;

        if (leftTriggerJump)
        {
            Vector3 vectorLeft = handTransformLeft.position - handLeftLastPosition;

            if (Vector3.Angle(vectorLeft, Vector3.up) > jumpAngleThreshold) leftJump = false;
            if (Vector3.Project(vectorLeft, Vector3.up).magnitude < jumpThreshold || Vector3.Project(vectorLeft, Vector3.up).magnitude > invalidTrackingThreshold) leftJump = false;
        }
        else leftJump = false;
        
        return leftJump || rightJump;
    }

    private bool CheckIfGrounded()
    {
        Vector3 rayStart = transform.TransformPoint(character.center);
        float rayLength = character.center.y + 0.01f;
        bool hasHit = Physics.SphereCast(rayStart, character.radius, Vector3.down, out RaycastHit hitInfo, rayLength, groundLayer);
        return hasHit && !hitInfo.collider.isTrigger;
    }

    private void CapsuleFollowHeadset()
    {
        character.height = rig.centerEyeAnchor.position.y + additionalHeight - rig.transform.position.y;
        Vector3 capsuleCenter = transform.InverseTransformPoint(rig.centerEyeAnchor.position);
        character.center = new Vector3(capsuleCenter.x, character.height/2 + character.skinWidth, capsuleCenter.z);
    }

    private float GetSmoothedMagnitude(float magnitude)
    {
        if (magnitudeList.Count > speedSmoothing) magnitudeList.RemoveAt(magnitudeList.Count-1);

        float magnitudeClamp = smoothingRamp;
        if (magnitudeList.Count != 0)
        {
            magnitudeClamp += magnitudeList.Average();
        }

        magnitudeList.Insert(0, Mathf.Min(magnitude, smoothingInputClamp, Mathf.Max(magnitudeClamp, smoothRampBase)));
        return Mathf.Min(magnitudeList.Average(), magnitudeClamp);
    }

    private float GetSmoothedMagnitude()
    {
        return magnitudeList.Average();
    }

    private float ClearSmoothedMagnitude()
    {
        int vectorCount = magnitudeList.Count;
        magnitudeList.Clear();
        for (int i = 0; i < vectorCount; i++) magnitudeList.Add(0f);
        return magnitudeList.Average();
    }

    private float GetJumpAcceleration(float height)
    {
        return Mathf.Sqrt(-2 * gravity * height);
    }

    private Vector3 GetSmoothedDirection(Vector3 direction)
    {
        if (movementList.Count > directionSmoothing) movementList.RemoveAt(movementList.Count-1);
        movementList.Insert(0, direction.normalized);
        Vector3 averageDirection = new Vector3();
        foreach (Vector3 v in movementList)
        {
            averageDirection += v;
        }
        return averageDirection / movementList.Count;
    }

    private Vector3 GetSmoothedDirection()
    {
        Vector3 averageDirection = new Vector3();
        foreach (Vector3 v in movementList)
        {
            averageDirection += v;
        }
        return averageDirection / movementList.Count;
    }

    private void ShowHandLeftElements(bool input)
    {
        handLeftVisual.SetActive(input);
        handLeftHP.SetActive(input);
        handLeftCast.SetActive(input);
    }    

    private void ShowHandRightElements(bool input)
    {
        handRightVisual.SetActive(input);
        handRightHP.SetActive(input);
        handRightCast.SetActive(input);
    }    
}
