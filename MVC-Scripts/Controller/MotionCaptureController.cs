using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public enum RecordingStates
{
    idle,
    recording,
    playing
}

public class MotionCaptureController : UdonSharpBehaviour
{
    //UI
    [SerializeField] TMPro.TextMeshProUGUI currentFixedUpdateText;
    [SerializeField] Slider timelineSlider;

    //Unity assignments
    [SerializeField] SyncedLocationData[] linkedSyncedDataHolders;
    RecordingStates recordingState = RecordingStates.idle;

    //Recording data
    int fixedUpdateBetweenRecordStates;
    int previousRecordIndex;
    int currentFixedUpdateIndex;

    //Replay data
    public float replayStartTime { get; private set; }

    //Internal functions
    void Setup()
    {
        foreach (SyncedLocationData locaitonData in linkedSyncedDataHolders)
        {
            locaitonData.ClearAndSetup(this);
        }
    }

    void RecordUpdate()
    {
        int recordIndex = currentFixedUpdateIndex / fixedUpdateBetweenRecordStates;
        
        if(previousRecordIndex != recordIndex)
        {
            foreach(SyncedLocationData locationData in linkedSyncedDataHolders)
            {
                locationData.RecordLocation();
            }

            previousRecordIndex = recordIndex;
        }

        currentFixedUpdateIndex++;
    }

    void ReplayingUpdate()
    {
        float replayTime = Time.time - replayStartTime;

        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.SetReplayLocations(replayTime);
        }

        timelineSlider.SetValueWithoutNotify(replayTime);
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
    }

    private void Update()
    {
        switch (recordingState)
        {
            case RecordingStates.idle:
                break;
            case RecordingStates.recording:
                break;
            case RecordingStates.playing:
                ReplayingUpdate();
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (recordingState)
        {
            case RecordingStates.idle:
                break;
            case RecordingStates.recording:
                RecordUpdate();
                break;
            case RecordingStates.playing:
                break;
        }
    }

    //UI events
    public void IncreaseFixedUpdateCounter()
    {
        currentFixedUpdateIndex++;

        UpdateFixedUpdateText();
    }

    public void DecreaseFixedUpdateCounter()
    {
        currentFixedUpdateIndex--;

        if (currentFixedUpdateIndex < 1) currentFixedUpdateIndex = 1;

        UpdateFixedUpdateText();
    }

    public void StartRecording()
    {
        recordingState = RecordingStates.recording;
        currentFixedUpdateIndex = 0;
        previousRecordIndex = 0;
    }

    public void StopRecording()
    {
        recordingState = RecordingStates.idle;
    }

    public void StartPlaying()
    {
        recordingState = RecordingStates.playing;
        replayStartTime = (timelineSlider.maxValue * 0.99f > timelineSlider.value) ? Time.time : Time.time - timelineSlider.value;

        float maxReplayTime = 0;

        foreach(SyncedLocationData data in linkedSyncedDataHolders)
        {
            if (data.DoReplay && data.RecordTime > maxReplayTime) maxReplayTime = data.RecordTime; 
        }

        timelineSlider.maxValue = maxReplayTime;
    }

    public void StopPlaying()
    {
        recordingState = RecordingStates.idle;
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
        replayStartTime = Time.time - timelineSlider.value;

        ReplayingUpdate();
    }
}