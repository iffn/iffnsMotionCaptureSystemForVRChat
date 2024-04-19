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
    [SerializeField] TMPro.TextMeshProUGUI currentTimeText;
    [SerializeField] Slider timelineSlider;
    [SerializeField] Button StartRecordingButton;
    [SerializeField] Button StopRecordingButton;
    [SerializeField] Button StartPlayingButton;
    [SerializeField] Button StopPlayingButton;

    //Unity assignments
    [SerializeField] SyncedLocationData[] linkedSyncedDataHolders;
    RecordingStates recordingState = RecordingStates.idle;

    //Recording data
    int fixedUpdateBetweenRecordStates;
    int previousRecordIndex;
    int currentFixedUpdateIndex;

    //Replay data
    public float ReplayStartTime { get; private set; }

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
    }

    void ReplayingUpdate()
    {
        float replayTime = Time.time - ReplayStartTime;

        SetReplayTime(replayTime);

        timelineSlider.SetValueWithoutNotify(replayTime);
    }

    void SetReplayTime(float replayTime)
    {
        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.SetReplayLocations(replayTime);
        }
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
        if (recordingState == RecordingStates.playing) StopPlaying();

        recordingState = RecordingStates.recording;
        currentFixedUpdateIndex = 0;
        previousRecordIndex = 0;
        ReplayStartTime = Time.time;

        StartRecordingButton.gameObject.SetActive(false);
        StopRecordingButton.gameObject.SetActive(true);
    }

    public void StopRecording()
    {
        recordingState = RecordingStates.idle;
        StopRecordingButton.gameObject.SetActive(false);
        StartRecordingButton.gameObject.SetActive(true);
    }

    public void StartPlaying()
    {
        if(recordingState == RecordingStates.recording) StopRecording();

        recordingState = RecordingStates.playing;
        ReplayStartTime = (timelineSlider.maxValue * 0.99f > timelineSlider.value) ? Time.time : Time.time - timelineSlider.value;

        float maxReplayTime = 0;

        foreach(SyncedLocationData data in linkedSyncedDataHolders)
        {
            if (data.DoReplay && data.RecordTime > maxReplayTime) maxReplayTime = data.RecordTime; 
        }

        timelineSlider.maxValue = maxReplayTime;

        StopPlayingButton.gameObject.SetActive(true);
        StartPlayingButton.gameObject.SetActive(false);
    }

    public void StopPlaying()
    {
        recordingState = RecordingStates.idle;

        StopPlayingButton.gameObject.SetActive(false);
        StartPlayingButton.gameObject.SetActive(true);
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