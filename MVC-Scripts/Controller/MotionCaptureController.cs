using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.Core.ApiAvatar;

public enum TimelineStates
{
    idle,
    recording,
    replaying
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MotionCaptureController : UdonSharpBehaviour
{
    //UI
    [SerializeField] TMPro.TextMeshProUGUI currentFixedUpdateText;
    [SerializeField] TMPro.TextMeshProUGUI currentTimeText;
    [SerializeField] Slider timelineSlider;
    [SerializeField] Button startRecordingButton;
    [SerializeField] Button stopRecordingButton;
    [SerializeField] Button startReplayingButton;
    [SerializeField] Button stopReplayingButton;
    [SerializeField] Toggle useSyncedReplayDataToggle;
    [SerializeField] TMPro.TextMeshProUGUI currentOwnerText;

    [UdonSynced] float syncedTime;
    [UdonSynced] bool[] replayElements = new bool[0];

    //Unity assignments
    [SerializeField] SyncedLocationData[] linkedSyncedDataHolders;
    TimelineStates timelineState = TimelineStates.idle;
    
    const float smoothTime = 0.068f;

    //Recording data
    float maxReplayTime = 0;
    int fixedUpdateBetweenRecordStates = 4;
    int previousRecordIndex;
    int currentFixedUpdateIndex;
    VRCPlayerApi localPlayer;

    //Replay data
    public float ReplayStartTime { get; private set; }
    float sliderVelocity;

    //Internal functions
    void Setup()
    {
        localPlayer = Networking.LocalPlayer;

        foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
        {
            locationData.ClearAndSetup(this);
        }

        replayElements = new bool[linkedSyncedDataHolders.Length];

        UpdateFixedUpdateText();
        UpdateCurrentTimeText(0);
        SetOwnerText(Networking.GetOwner(gameObject));
        //UpdatePlayerNames(); //Probably not needed since OnPlayerJoin runs automatically
    }

    void RecordUpdate()
    {
        int recordIndex = currentFixedUpdateIndex / fixedUpdateBetweenRecordStates;

        float recordingTime = Time.time - ReplayStartTime;

        if (previousRecordIndex != recordIndex)
        {
            foreach (SyncedLocationData locationData in linkedSyncedDataHolders)
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

        if (replayTime >= maxReplayTime)
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
        currentFixedUpdateText.text = $"{fixedUpdateBetweenRecordStates}/{Mathf.RoundToInt(1f / Time.fixedDeltaTime)}, every {Time.fixedDeltaTime * fixedUpdateBetweenRecordStates * 1000} ms, {1 / (Time.fixedDeltaTime * fixedUpdateBetweenRecordStates)} times/s";
    }

    void SetOwnerText(VRCPlayerApi owner)
    {
        currentOwnerText.text = PlayerText(owner);
    }

    public static string PlayerText(VRCPlayerApi player)
    {
#if UNITY_EDITOR
        return $"{player.displayName}{(player.isLocal ? " (you)" : "")}"; //Local player in editor already contains player ID in []
#else
        return $"[{player.playerId}] {player.displayName}{(player.isLocal ? " (you)" : "")}";
#endif
    }

    //Unity functions
    void Start()
    {
        Setup();

        Debug.Log("New version");
    }

    private void Update()
    {
        if(AllowReplayChange)
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
        else
        {
            float newSliderValue = Mathf.SmoothDamp(timelineSlider.value, syncedTime, ref sliderVelocity, smoothTime);

            timelineSlider.SetValueWithoutNotify(newSliderValue);
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

    public bool AllowReplayChange
    {
        get
        {
            return Networking.IsOwner(gameObject) || !useSyncedReplayDataToggle.isOn;
        }
    }

    public void UpdateReplayStatesFromSync()
    {
        if (AllowReplayChange)
        {
            for (int i = 0; i < linkedSyncedDataHolders.Length; i++)
            {
                replayElements[i] = linkedSyncedDataHolders[i].DoReplay;
            }

            RequestSerialization();
        }
        else
        {
            RestoreReplayStates();
        }
    }

    public void RestoreReplayStates()
    {
        for(int i = 0; i < linkedSyncedDataHolders.Length; i++)
        {
            linkedSyncedDataHolders[i].DoReplay = replayElements[i];
        }
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

        if (!AllowReplayChange) return;

        timelineSlider.SetValueWithoutNotify(timelineSlider.maxValue); //ToDo: Set slider to max recordable value

        timelineState = TimelineStates.recording;
        currentFixedUpdateIndex = 0;
        previousRecordIndex = 0;
        ReplayStartTime = Time.time;

        startRecordingButton.gameObject.SetActive(false);
        stopRecordingButton.gameObject.SetActive(true);

        foreach (SyncedLocationData syncedLocation in linkedSyncedDataHolders)
        {
            syncedLocation.ClearBeforeRecording();
        }
    }

    public void StopRecording()
    {
        timelineState = TimelineStates.idle;
        stopRecordingButton.gameObject.SetActive(false);
        startRecordingButton.gameObject.SetActive(true);

        UpdateMaxTime();
        timelineSlider.SetValueWithoutNotify(0);

        foreach (SyncedLocationData syncedLocation in linkedSyncedDataHolders)
        {
            syncedLocation.FinishAndTransferData();
        }
    }

    public void StartReplaying()
    {
        if (timelineState == TimelineStates.recording) StopRecording();

        if (!AllowReplayChange) return;

        timelineState = TimelineStates.replaying;
        ReplayStartTime = (timelineSlider.maxValue * 0.99f > timelineSlider.value) ? Time.time : Time.time - timelineSlider.value;

        stopReplayingButton.gameObject.SetActive(true);
        startReplayingButton.gameObject.SetActive(false);
    }

    public void StopReplaying()
    {
        timelineState = TimelineStates.idle;

        stopReplayingButton.gameObject.SetActive(false);
        startReplayingButton.gameObject.SetActive(true);
    }

    public void UpdateTimeFromSlider()
    {
        if (!Networking.IsOwner(gameObject) && useSyncedReplayDataToggle.isOn)
        {
            timelineSlider.SetValueWithoutNotify(syncedTime);
            return;
        }

        SetReplayTime(timelineSlider.value);

        if (Networking.IsOwner(gameObject))
        {
            syncedTime = timelineSlider.value;
            RequestSerialization();
        }
    }

    public void RequestOwnership()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);
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

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        SetOwnerText(player);

        if (AllowReplayChange)
        {
            UpdateReplayStatesFromSync();

            RequestSerialization();
        }
        else
        {
            switch (timelineState)
            {
                case TimelineStates.idle:
                    break;
                case TimelineStates.recording:
                    StopRecording();
                    break;
                case TimelineStates.replaying:
                    StopReplaying();
                    break;
                default:
                    break;
            }
        }
    }

    public override void OnDeserialization()
    {
        RestoreReplayStates();
    }
}