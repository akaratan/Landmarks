using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
    Speed discrimination pilot: moves the viewer straight down a hallway
    at a set speed for a set time, then asks for a response.

    Attach to the object that holds the camera (a plain Camera for
    desktop testing, or the VR camera rig's parent for a headset).

    On-screen prompts say what to press at each step:
        Space        run the next trial
        R            replay the current trial (before answering)
        0 / 1 / 2    record the participant's answer (0 = slow, 1 = same, 2 = fast)

    The next trial can't start until an answer is recorded.

    Results are saved to data/tmp/speed_pilot_<participantID>.csv in the project folder.

    Written for Azra Karatan, Nov 2026.
*/
public class HallwaySpeedPilot : MonoBehaviour //monobheavior makes it something you can attach to an object
{
    //experiment settings: (change)
    [Header("Participant")]
    [Tooltip("Used in the output file name, e.g. P01")]
    public string participantID = "test";

    [Header("Speeds to test (meters per second)")]
    public List<float> speeds = new List<float> { 1.0f, 3.0f, 6.0f, 9.0f, 12.0f };
    public bool shuffleSpeeds = true;

    [Header("Trial settings")]
    [Tooltip("How long each movement lasts, in seconds")]
    public float trialDuration = 9.0f;
    [Tooltip("Pause after movement before the answer prompt, in seconds")]
    public float pauseAfter = 1.0f;

    //on-screen prompt settings: what the question says and what each key means
    [Header("Answer prompt")]
    public string question = "How fast was that?";
    public string label0 = "Slow";
    public string label1 = "Same";
    public string label2 = "Fast";
    public int fontSize = 20;

    //what the script is waiting for right now
    private enum State { WaitingToStart, Moving, WaitingForAnswer, Finished }
    private State state = State.WaitingToStart;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private int trialIndex = -1;
    private int timesShown = 0;          //how many times this trial was played (1 + replays)
    private float promptShownAt = 0f;    //when the answer prompt appeared, for response time
    private string csvPath;
    private string lastMessage = "";     //short confirmation shown after an answer

    void Start()
    {   //records start location so every run starts from the same place
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (shuffleSpeeds)
        {
            for (int i = speeds.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                float tmp = speeds[i]; speeds[i] = speeds[j]; speeds[j] = tmp;
            }
        }

        //creates the data temp file to save the trial values (one file per participant)
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "data", "tmp");
        Directory.CreateDirectory(folder);
        csvPath = Path.Combine(folder, "speed_pilot_" + participantID + ".csv");
        if (!File.Exists(csvPath))
            File.WriteAllText(csvPath,
                "timestamp,participant,trial,speed_mps,duration_s,distance_m,times_shown,key,answer,rt_s\n");

        Debug.Log("Speed pilot ready: " + speeds.Count + " speeds loaded. Saving to " + csvPath);
    }

    void Update()
    {
        switch (state)
        {
            case State.WaitingToStart: //hitting space moves to the next trial
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    trialIndex++;
                    timesShown = 0;
                    lastMessage = "";
                    StartCoroutine(RunTrial(speeds[trialIndex]));
                }
                break;

            case State.WaitingForAnswer: // records results: 0 is slow, 1 is same, 2 is fast (top row or number pad)
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) RecordAnswer("0", label0);
                else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) RecordAnswer("1", label1);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) RecordAnswer("2", label2);
                else if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(RunTrial(speeds[trialIndex])); //hitting R reruns this trial
                break;

                //Moving and Finished: keys are ignored, so the trial cant be interrupted while it is running
        }
    }

    IEnumerator RunTrial(float speed) //to ensure the trials run smoothly
    {
        state = State.Moving;
        timesShown++;
        transform.SetPositionAndRotation(startPosition, startRotation);
        Debug.Log("Trial " + (trialIndex + 1) + ": " + speed + " m/s");

        float elapsed = 0f;
        while (elapsed < trialDuration)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(pauseAfter);
        transform.SetPositionAndRotation(startPosition, startRotation);

        state = State.WaitingForAnswer; //now the answer prompt shows
        promptShownAt = Time.time;
    }

    void RecordAnswer(string key, string answer) //saves trial number, time, distance moved, participant answers
    {
        float speed = speeds[trialIndex];
        float rt = Time.time - promptShownAt;
        string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:F3}\n",
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            participantID, trialIndex + 1, speed, trialDuration, speed * trialDuration,
            timesShown, key, answer, rt);
        File.AppendAllText(csvPath, line);
        Debug.Log("Logged trial " + (trialIndex + 1) + " answer " + key + " (" + answer + ")");

        lastMessage = "Recorded: " + answer;
        state = (trialIndex + 1 >= speeds.Count) ? State.Finished : State.WaitingToStart;
    }

    //draws the prompts on the screen (Game view / monitor only, not inside a VR headset)
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        style.wordWrap = true;

        string text = "";
        switch (state)
        {
            case State.WaitingToStart:
                text = (lastMessage != "" ? lastMessage + "\n\n" : "")
                     + "Trial " + (trialIndex + 2) + " of " + speeds.Count
                     + "\n\nPress SPACE to start";
                break;
            case State.Moving:
                text = "";   //screen stays clear while moving
                break;
            case State.WaitingForAnswer:
                text = question + "\n\n"
                     + "0 = " + label0 + "     1 = " + label1 + "     2 = " + label2
                     + "\n\n(R = replay)";
                break;
            case State.Finished:
                text = lastMessage + "\n\nAll trials done. Stop Play mode.";
                break;
        }

        if (text == "") return;

        //dark box behind the text so it is readable against the walls
        Rect box = new Rect(Screen.width * 0.15f, Screen.height * 0.3f,
                            Screen.width * 0.7f, Screen.height * 0.4f);
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(box, text, style);
    }
}