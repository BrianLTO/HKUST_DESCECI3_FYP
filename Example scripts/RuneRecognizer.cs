using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;
using System.Linq;

public class RuneRecognizer : MonoBehaviour
{
    public static RuneRecognizer instance;
    public bool showDebug = false;
    void Awake()
    {
        //singleton script
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    [System.Serializable]
    public class UnityNullEvent : UnityEvent { }
    public UnityNullEvent onRecognizedVoice;

    [Header("Settings")]
    public float recognizeThreshold = 0.9f;
    public int runeMemoryCount = 10;
    public int KNN_N = 3;
    public float gestureRenderDistance = 1f;
    public float recognizedSoundVolume = 1f;
    public float notRecognizedSoundVolume = 1f;


    [Header("Actions")]
    public string saveName;
    public bool saveLastGesture;
    public bool showLastGesture;

    [Header("")]
    public string renderTarget;
    public bool renderGesture;
    public bool removeRender;
    public bool testKNN;

    [Header("Tutorial")]
    public Rune lastRune;
    public bool tutorialCheck = false;

    List<Gesture> allGestures { get { return baseGestures.Concat(userVoice).Concat(userFire).Concat(userThunder).Concat(userWater).ToList(); } }
    List<Gesture> baseGestures = new List<Gesture>();
    List<Gesture> userVoice = new List<Gesture>();
    List<Gesture> userFire = new List<Gesture>();
    List<Gesture> userThunder = new List<Gesture>();
    List<Gesture> userWater = new List<Gesture>();

    Gesture lastGestureFromPlayer;

    LineRenderer lineRenderer;
    Material lineRendererMaterial;


    int memoryCounter = 0; //for player gesture naming purposes

    // Start is called before the first frame update
    void Start()
    {
        //initilize line renderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRendererMaterial = GetComponent<Renderer>().material;

        //load gestures from persistent data path
        string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        if (showDebug) Debug.Log("Loading from" + Application.persistentDataPath);
        foreach (var item in gestureFiles)
        {
            if (showDebug) Debug.Log(item);
            baseGestures.Add(GestureIO.ReadGestureFromFile(item));
        }
    }

    //returns the classified rune from input gesture
    //also stores the gesture and updates the dynamic gesture list
    public Rune ClassifyRuneFromPlayer(Gesture input, bool isValid, bool isLeftHand)
    {
        //obtain classified rune (isValid is false when drawing distance is too low)
        Rune classifyResult;
        if (isValid) classifyResult = ClassifyKNN(input, allGestures);
        else classifyResult = Rune.Empty;
        lastRune = classifyResult;

        //store the gesture
        lastGestureFromPlayer = input;

        //no dynamic rune list update needed if rune is unclassified
        if (classifyResult != Rune.Empty) UpdateDynamicList(input, classifyResult);

        //perform actions
        if (classifyResult == Rune.Voice) onRecognizedVoice.Invoke();
        else
        {
            if (isLeftHand) SpellCastingController.instance.SetRuneLeft(classifyResult);
            else SpellCastingController.instance.SetRuneRight(classifyResult);
        }
        tutorialCheck = true;
        return classifyResult;      
    }

    //returns the classified rune from the input gesture
    public Rune ClassifyRune(Gesture input)
    {
        Rune result = ClassifyKNN(input, allGestures);
        if(showDebug) Debug.Log("RUNE RECOG: input is " + result);
        return result;
    }

    //returns the classified rune without score threshold and using only baseGestures
    public Rune ClassifyRuneBase(Gesture input)
    {
        Rune result = ClassifyKNN(input, baseGestures);
        if (showDebug) Debug.Log("RUNE RECOG: input is " + result + " with baseGestures set");
        return result;
    }

    //Classifies a gesture using KNN
    //increases N by one if tied until broken
    Rune ClassifyKNN(Gesture input, List<Gesture> gesturelist)
    {
        //this list stores the gesture name is distances
        List<(string, float)> results = new List<(string, float)>();

        //find cloud distance between input and each gesture
        foreach(Gesture g in gesturelist)
        {
            results.Add((g.Name, PointCloudRecognizer.GreedyCloudMatch(input.Points, g.Points)));
        }

        //convert distances into similarity scores
        var scores = results.Select(e => { return (e.Item1, Mathf.Max((e.Item2 - 2.0f) / -2.0f, 0.0f)); }).ToList();


        if (scores.Where(e => e.Item2 > recognizeThreshold).Count() < KNN_N) return Rune.Empty;
        scores = scores.OrderByDescending(e => e.Item2).ToList();

        int counter = 0;
        Dictionary<string, int> KNN_result = new Dictionary<string, int>();
        foreach(var e in scores) if(showDebug)Debug.Log(e.Item1 + "\t\t" + e.Item2);
        foreach(var e in scores)
        {
            if (!KNN_result.ContainsKey(e.Item1.Split("-")[0])) KNN_result[e.Item1.Split("-")[0]] = 1;
            else KNN_result[e.Item1.Split("-")[0]] += 1;
            counter++;

            if (counter >= KNN_N && counter > KNN_result.Count)
            {
                return RuneInfo.GetRune(KNN_result.Aggregate((x, y) => x.Value > y.Value ? x : y).Key);
            }
        }

        return Rune.Empty; //never reached
    }

    //HELPER FUNCTION: updates the dynamic gesture list with given gesture and classification
    //gesture is not added if it classifies as another rune with baseGestures
    void UpdateDynamicList(Gesture playerGesture, Rune classification)
    {
        //gesture is not added if it classifies as another rune with baseGestures
        if (classification != ClassifyKNN(playerGesture, baseGestures)) return;

        List<Gesture> targetList = new List<Gesture>(); //dummy
        switch(classification)
        {
            case Rune.Voice:
                targetList = userVoice;
                break;
            case Rune.Fire:
                targetList = userFire;
                break;
            case Rune.Thunder:
                targetList = userThunder;
                break;
            case Rune.Water:
                targetList = userWater;
                break;
        }

        //new object for changing name
        Gesture gestureCopy = new Gesture(playerGesture.Points);
        gestureCopy.Name = RuneInfo.GetString(classification) + "-Player-" + memoryCounter++;

        //gesture is added to the end
        targetList.Add(gestureCopy);

        //remove oldest memory if count exceeds limit
        if (targetList.Count > runeMemoryCount) targetList.RemoveAt(0);
    }

    void SaveLastGesture()
    {
        if (lastGestureFromPlayer == null) return;
        string fileName = Application.persistentDataPath + "/" + saveName + ".xml";
        GestureIO.WriteGesture(lastGestureFromPlayer.Points, saveName, fileName);
    }

    void RenderLastGesture()
    {
        if (lastGestureFromPlayer == null) return;

        RenderGesture(lastGestureFromPlayer);
    }

    void RenderGesture(Gesture input)
    {
        if (input == null) return;

        Transform anchor = ContinousMovement.rig.centerEyeAnchor;
        Point[] points = input.Points;
        Vector3[] vectors = new Vector3[points.Length];
        lineRenderer.positionCount = 0;

        for (int i = 0; i < points.Length; i++)
        {
            //Debug.Log("RUNE RECOG: " + p.X + " " + p.Y);
            vectors[i] = new Vector3(points[i].X, points[i].Y, 0);
            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, vectors[i]);
        }

        lineRenderer.transform.position = anchor.position + new Vector3(anchor.forward.x, 0, anchor.forward.z).normalized * gestureRenderDistance;
        lineRenderer.transform.LookAt(new Vector3(anchor.position.x, lineRenderer.transform.position.y, anchor.position.z));
        lineRenderer.transform.forward *= -1;

        if (input.Name != "")
        {
            lineRenderer.material.color = RuneInfo.GetColor(RuneInfo.GetRune(input.Name.Split("-")[0]));
            lineRenderer.material.SetColor("_EmissionColor", RuneInfo.GetColor(RuneInfo.GetRune(input.Name.Split("-")[0])) * 2);
        }
        else
        {
            lineRenderer.material.color = new Color(1, 1, 1);
            lineRenderer.material.SetColor("_EmissionColor", new Color(1, 1, 1) * 2);
        }
    }

    public void RenderGesture(string gestureName)
    {
        if (gestureName.Equals("Fire") || gestureName.Equals("Thunder") || gestureName.Equals("Water") || gestureName.Equals("Voice")) gestureName += "-1";
        Gesture target = baseGestures.Find(g => g.Name.ToLower().Equals(gestureName.ToLower()));
        if (target != null) RenderGesture(target);
    }

    public void RemoveRender()
    {
        lineRenderer.positionCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (saveLastGesture)
        {
            saveLastGesture = false;
            SaveLastGesture();
        }

        if (showLastGesture)
        {
            showLastGesture = false;
            RenderLastGesture();
        }
        
        if (renderGesture)
        {
            Debug.Log("rendering " + baseGestures[0].Name);
            renderGesture = false;
            RenderGesture(renderTarget);
        }

        if (removeRender)
        {
            removeRender = false;
            lineRenderer.positionCount = 0;
        }

        if (testKNN)
        {
            testKNN = false;
            if (showDebug) Debug.Log("Classified as " + ClassifyKNN(baseGestures.Find(e => e.Name.Equals(renderTarget)), baseGestures));
            
        }
    }
}
