using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;
public class GestureRecognizer : MonoBehaviour
{
    [Header("Fixed variable")]
    public OVRSkeleton skeleton;
    public AudioSource audioSource;

    [Header("Assign hand poses")]
    public ActiveStateSelector pose;

    [Header("Settings")]
    public bool isLeftHand = false;
    public float newPositionThresholdDistance =  0.05f;
    public float minimumDistance = 0.5f;

    private List<Gesture> gestureList = new List<Gesture>();
    private bool isPressed = false;
    private bool isMoving = false;
    private Transform movementSource;
    private List<Vector3> positionsList = new List<Vector3>();
    private LineRenderer lineRenderer;
    private Material lineRendererMaterial;
    private Coroutine fadeEffect;
    private float totalDistance = 0;


    // Start is called before the first frame update
    void Start()
    {
        //event detecting for the drawing hand pose
        pose.WhenSelected += () => {isPressed = true;};
        pose.WhenUnselected += () => {isPressed = false;};

        //initilize line renderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRendererMaterial = GetComponent<Renderer>().material;

        string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (var item in gestureFiles)
        {
            gestureList.Add(GestureIO.ReadGestureFromFile(item));
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (movementSource)
        {
            if (!isMoving && isPressed)
            {
                StartMovement();
            }
            else if (isMoving && isPressed)
            {
                UpdateMovement();
            }
            else if (isMoving && !isPressed)
            {
                EndMovemnt();
            }
        }
        else
        {
            if (skeleton.Bones != null)
                movementSource = skeleton.Bones[20].Transform;
        }
    }

    void StartMovement()
    {
        audioSource.loop = true;
        audioSource.Play();

        //Debug.Log("Starting movement");
        Vector3 currentPosition = transform.InverseTransformPoint(movementSource.position);

        isMoving = true;
        positionsList.Clear();
        positionsList.Add(currentPosition);

        if (fadeEffect != null) StopCoroutine(fadeEffect);
        lineRenderer.startColor = new Color(0.9f, 0.9f, 0.9f, 0.75f);
        lineRenderer.endColor = lineRenderer.startColor;
        lineRenderer.positionCount = 0;
        lineRenderer.positionCount += 1;
        lineRenderer.SetPosition(lineRenderer.positionCount-1, currentPosition);

        lineRendererMaterial.color = lineRenderer.startColor;
        lineRendererMaterial.SetColor("_EmissionColor", new Color(0f, 0f, 0f, 0f));
    }

    void UpdateMovement()
    {
        //Debug.Log("Updating movmement");
        Vector3 currentPosition = transform.InverseTransformPoint(movementSource.position);
        Vector3 lastPositon = positionsList[positionsList.Count - 1];
        if (Vector3.Distance(currentPosition, lastPositon) > newPositionThresholdDistance)
        {
            positionsList.Add(currentPosition);
            totalDistance += newPositionThresholdDistance;

            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, currentPosition);
        }
            
    }

    void EndMovemnt()
    {
        audioSource.loop = false;
        audioSource.Stop();
        
        //Debug.Log("Ending movement");
        isMoving = false;

        //Creating gesture from position list
        Point[] pointArray = new Point[positionsList.Count]; 
        for (int i = 0; i < positionsList.Count; i++)
        {
            List<Vector3> positionListWorldSpace = new List<Vector3>();
            foreach (var item in positionsList)
            {
                positionListWorldSpace.Add(transform.TransformPoint(item));
                //Destroy(Instantiate(debugObject, transform.TransformPoint(item), Quaternion.identity), 3);
            }

            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionListWorldSpace[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
        }

        Gesture newGesture = new Gesture(pointArray);

        Rune result = RuneRecognizer.instance.ClassifyRuneFromPlayer(newGesture, totalDistance >= minimumDistance, isLeftHand );
        totalDistance = 0;

        if (result != Rune.Empty)
        {
            lineRenderer.startColor = RuneInfo.GetColor(result);
            lineRenderer.endColor = lineRenderer.startColor;
            lineRendererMaterial.color = lineRenderer.startColor;
            lineRendererMaterial.SetColor("_EmissionColor", lineRenderer.startColor * 2);
        }
        else
        {
            //onRecognized.Invoke("Not recognized");
            lineRenderer.startColor = new Color(0f, 0f, 0f, 0.9f);
            lineRenderer.endColor = lineRenderer.startColor;
            lineRendererMaterial.color = lineRenderer.startColor;
        }
        fadeEffect = StartCoroutine(LineFade());
    }

    /*deprecated function*/
    //public void ToggleCreation()
    //{
    //    //Debug.Log("Toggle Creation");
    //    if (creationMode)
    //    {
    //        creationMode = false;
    //        //onRecognized.Invoke("Recognize mode");
    //    }
    //    else
    //    {
    //        creationMode = true;
    //        //onRecognized.Invoke("Creation mode");
    //        gestureName = gestureNameBase + "_" + gestureNameIndex++;
    //    }
    //}

    IEnumerator LineFade()
    {
        Debug.Log("fading");
        Color lineColor = lineRenderer.startColor;
        Color glowColor = lineRendererMaterial.GetColor("_EmissionColor");
        Color glowColorBase = glowColor * 0.025f;
        for (float alpha = lineColor.a; alpha >= 0; alpha -= 0.025f)
        {
            lineColor.a = alpha;
            glowColor -= glowColorBase;
            lineRendererMaterial.SetColor("_EmissionColor", glowColor);
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRendererMaterial.color = lineColor;
            yield return new WaitForSeconds(.05f);
        }
        yield return null;
    }
}
