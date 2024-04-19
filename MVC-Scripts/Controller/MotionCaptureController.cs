using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public enum TimelineStates
{
    idle,
    recording,
    replaying
}

public class MotionCaptureController : UdonSharpBehaviour
{
    //UI
    [SerializeField] TMPro.TextMeshProUGUI currentFixedUpdateText;
    [SerializeField] TMPro.TextMeshProUGUI currentTimeText;
    [SerializeField] Slider timelineSlider;
    [SerializeField] Button StartRecordingButton;
    [SerializeField] Button StopRecordingButton;
    [SerializeField] Button StartReplayingButton;
    [SerializeField] Button StopReplayingButton;

    //Unity assignments
    [SerializeField] SyncedLocationData[] linkedSyncedDataHolders;
    TimelineStates timelineState = TimelineStates.idle;

    //Recording data
    float maxReplayTime = 0;
    int fixedUpdateBetweenRecordStates = 4;
    int previousRecordIndex;
    int currentFixedUpdateIndex;

    //Replay data
    public float ReplayStartTime { get; private set; }

    //Internal functions
    void Setup()
    {
        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.ClearAndSetup(this);
        }

        UpdateFixedUpdateText();
        UpdateCurrentTimeText(0);
        //UpdatePlayerNames(); //Probably not needed since OnPlayerJoin runs automatically
    }

    void RecordUpdate()
    {
        int recordIndex = currentFixedUpdateIndex / fixedUpdateBetweenRecordStates;
        
        float recordingTime = Time.time - ReplayStartTime;

        if(previousRecordIndex != recordIndex)
        {
            foreach(SyncedLocationData locationData in linkedSyncedDataHolders)
            {
                locationData.RecordLocation(recordingTime);
            }

            previousRecordIndex = recordIndex;
        }

        currentFixedUpdateIndex++;

        currentTimeText.text = $"{recordingTime}s";
    }

    void ReplayingUpdate()
    {
        float replayTime = Time.time - ReplayStartTime;

        bool stop = false;

        if(replayTime >= maxReplayTime)
        {
            replayTime = maxReplayTime;
            stop = true;
        }

        SetReplayTime(replayTime);

        timelineSlider.SetValueWithoutNotify(replayTime);

        if (stop)
        {
            StopReplaying();
        }
    }

    void SetReplayTime(float replayTime)
    {
        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.SetReplayLocations(replayTime);
        }

        UpdateCurrentTimeText(replayTime);
    }

    void UpdateCurrentTimeText(float time)
    {
        currentTimeText.text = $"{time}s";
    }

    void UpdatePlayerNames()
    {
        int numberOfPlayers = VRCPlayerApi.GetPlayerCount();

        VRCPlayerApi[] players = new VRCPlayerApi[numberOfPlayers];

        VRCPlayerApi.GetPlayers(players);

        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.UpdatePlayerList(players);
        }
    }

    void UpdateFixedUpdateText()
    {
        currentFixedUpdateText.text = $"{fixedUpdateBetweenRecordStates}/{Mathf.RoundToInt(1f/ Time.fixedDeltaTime)}, {Time.fixedDeltaTime * fixedUpdateBetweenRecordStates * 1000} ms, {1 / (Time.fixedDeltaTime * fixedUpdateBetweenRecordStates)} times/s";
    }

    //Unity functions
    void Start()
    {
        Setup();

        Debug.Log("New version");
    }

    private void Update()
    {
        switch (timelineState)
        {
            case TimelineStates.idle:
                break;
            case TimelineStates.recording:
                break;
            case TimelineStates.replaying:
                ReplayingUpdate();
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (timelineState)
        {
            case TimelineStates.idle:
                break;
            case TimelineStates.recording:
                RecordUpdate();
                break;
            case TimelineStates.replaying:
                break;
        }
    }

    //External functions
    public void UpdateMaxTime()
    {
        maxReplayTime = 0;

        foreach (SyncedLocationData data in linkedSyncedDataHolders)
        {
            if (data.DoReplay && data.RecordedTime > maxReplayTime) maxReplayTime = data.RecordedTime;
        }

        timelineSlider.maxValue = maxReplayTime;
    }

    //UI events
    public void IncreaseFixedUpdateCounter()
    {
        fixedUpdateBetweenRecordStates++;

        UpdateFixedUpdateText();
    }

    public void DecreaseFixedUpdateCounter()
    {
        fixedUpdateBetweenRecordStates--;

        if (fixedUpdateBetweenRecordStates < 1) fixedUpdateBetweenRecordStates = 1;

        UpdateFixedUpdateText();
    }

    public void StartRecording()
    {
        if (timelineState == TimelineStates.replaying) StopReplaying();

        timelineSlider.SetValueWithoutNotify(timelineSlider.maxValue); //ToDo: Set slider to max recordable value

        timelineState = TimelineStates.recording;
        currentFixedUpdateIndex = 0;
        previousRecordIndex = 0;
        ReplayStartTime = Time.time;

        StartRecordingButton.gameObject.SetActive(false);
        StopRecordingButton.gameObject.SetActive(true);

        foreach (SyncedLocationData syncedLocation in linkedSyncedDataHolders)
        {
            syncedLocation.ClearBeforeRecording();
        }
    }

    public void StopRecording()
    {
        timelineState = TimelineStates.idle;
        StopRecordingButton.gameObject.SetActive(false);
        StartRecordingButton.gameObject.SetActive(true);

        UpdateMaxTime();

        foreach (SyncedLocationData syncedLocation in linkedSyncedDataHolders)
        {
            syncedLocation.TransferData();
        }
    }

    public void StartReplaying()
    {
        if(timelineState == TimelineStates.recording) StopRecording();

        timelineState = TimelineStates.replaying;
        ReplayStartTime = (timelineSlider.maxValue * 0.99f > timelineSlider.value) ? Time.time : Time.time - timelineSlider.value;

        StopReplayingButton.gameObject.SetActive(true);
        StartReplayingButton.gameObject.SetActive(false);
    }

    public void StopReplaying()
    {
        timelineState = TimelineStates.idle;

        StopReplayingButton.gameObject.SetActive(false);
        StartReplayingButton.gameObject.SetActive(true);
    }

    public void UpdateTimeFromSlider()
    {
        SetReplayTime(timelineSlider.value);
    }

    //VRChat functions
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        base.OnPlayerJoined(player);

        UpdatePlayerNames();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        base.OnPlayerLeft(player);

        UpdatePlayerNames();
    }

    public void UpdateSlider()
    {
        ReplayStartTime = Time.time - timelineSlider.value;

        ReplayingUpdate();
    }
}