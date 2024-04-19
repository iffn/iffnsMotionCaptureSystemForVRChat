//Blocker

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using static VRC.Core.ApiAvatar;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SyncedLocationData : UdonSharpBehaviour
{
    //UI
    [SerializeField] Toggle doRecordIfOwnerToggle;
    [SerializeField] Toggle doReplayToggle;
    [SerializeField] TMPro.TextMeshProUGUI currentPlayerText;
    [SerializeField] TMPro.TextMeshProUGUI currentOwnerText;
    [SerializeField] TMPro.TextMeshProUGUI recordedTimeText;

    [SerializeField] AvatarModelMover linkedAvatarModelMover;

    //Synced variables
    [UdonSynced] Vector3[] syncedRecordedHipPositions = new Vector3[0];
    [UdonSynced] Quaternion[] syncedRecordedBoneRotations = new Quaternion[0];
    [UdonSynced] float recordedTime;
    [UdonSynced] float syncedPlayerHeight;

    public float RecordedTime
    {
        get
        {
            return recordedTime;
        }
        set
        {
            recordedTime = value;
            SetRecordingTimeText();
        }
    }

    //Fixed variables
    DataList recordedHipPositions;
    DataList recorderBoneRotations;
    MotionCaptureController linkedController;
    VRCPlayerApi localPlayer;

    //Runtime variables
    public bool DoReplay
    {
        get
        {
            return doReplayToggle.isOn;
        }
        set
        {
            doReplayToggle.SetIsOnWithoutNotify(value);
            linkedAvatarModelMover.gameObject.SetActive(value);
        }
    }
    VRCPlayerApi selectedPlayer;
    VRCPlayerApi[] players;
    int selectedPlayerInPlayerIndex;

    public int recordedSteps;
    public float timeStep;
    public int bones;

    //Internal functions
    void SetDummyData()
    {
        syncedRecordedHipPositions = new Vector3[] { Vector3.zero };
        syncedRecordedBoneRotations = GetBoneRotations();
        timeStep = 1;
        bones = syncedRecordedBoneRotations.Length;
    }

    Quaternion[] GetBoneRotations()
    {
        return new Quaternion[]
        {
            selectedPlayer.GetBoneRotation(HumanBodyBones.Hips),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftUpperLeg),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightUpperLeg),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftLowerLeg),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightLowerLeg),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftFoot),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightFoot),
            selectedPlayer.GetBoneRotation(HumanBodyBones.Spine),
            selectedPlayer.GetBoneRotation(HumanBodyBones.Chest),
            selectedPlayer.GetBoneRotation(HumanBodyBones.UpperChest),
            selectedPlayer.GetBoneRotation(HumanBodyBones.Neck),
            selectedPlayer.GetBoneRotation(HumanBodyBones.Head),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftShoulder),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightShoulder),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftUpperArm),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightUpperArm),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftLowerArm),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightLowerArm),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftHand),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightHand),
            selectedPlayer.GetBoneRotation(HumanBodyBones.LeftToes),
            selectedPlayer.GetBoneRotation(HumanBodyBones.RightToes),
            //linkedPlayer.GetBoneRotation(HumanBodyBones.LeftEye),
            //linkedPlayer.GetBoneRotation(HumanBodyBones.RightEye),
        };
    }

    void PrepareReplayData()
    {
        recordedSteps = syncedRecordedHipPositions.Length;

        if (recordedSteps == 0)
        {
            SetDummyData();
        }
        else
        {
            timeStep = recordedTime / recordedSteps;
            bones = syncedRecordedBoneRotations.Length / syncedRecordedHipPositions.Length;
        }
    }

    VRCPlayerApi SelectedPlayer
    {
        set
        {
            this.selectedPlayer = value;
            SetSelectedPlayerText();
        }
    }

    void SetOwnerText(VRCPlayerApi owner)
    {
        currentOwnerText.text = MotionCaptureController.PlayerText(owner);
    }

    void SetRecordingTimeText()
    {
        recordedTimeText.text = $"{recordedTime}s";
    }

    void SetSelectedPlayerText()
    {
        currentPlayerText.text = MotionCaptureController.PlayerText(selectedPlayer);
    }

    //Unity functions
    private void Start()
    {
        //Note Actual setup controlled by ClearAndSetup function from MotionCaptureController

        //Set states from UI buttons
        DoReplay = doReplayToggle.isOn;
        linkedAvatarModelMover.gameObject.SetActive(DoReplay);
    }

    bool setupRan = false;

    //Script events
    public void ClearAndSetup(MotionCaptureController linkedController)
    {
        setupRan = true;

        //External data
        if (localPlayer == null) localPlayer = Networking.LocalPlayer;

        //Data creation
        recordedHipPositions = new DataList();
        recorderBoneRotations = new DataList();

        //Variable data
        this.linkedController = linkedController;

        //Default values
        if (selectedPlayer == null) selectedPlayer = localPlayer;

        //UI
        SetOwnerText(Networking.GetOwner(gameObject));
        SetRecordingTimeText();
        SetSelectedPlayerText();

        //Setups
        linkedAvatarModelMover.Setup();
    }

    public void ClearBeforeRecording()
    {
        if (!Networking.IsOwner(gameObject) || !doRecordIfOwnerToggle.isOn) return;

        recordedHipPositions = new DataList();
        recorderBoneRotations = new DataList();

    }

    public void UpdatePlayerList(VRCPlayerApi[] players)
    {
        this.players = players;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == selectedPlayer) selectedPlayerInPlayerIndex = i;
        }
    }

    public void SetPlayerAsOwner()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);
    }

    public void FinishAndTransferData()
    {
        if (!Networking.IsOwner(gameObject) || !doRecordIfOwnerToggle.isOn) return;

        //Positions
        syncedRecordedHipPositions = new Vector3[recordedHipPositions.Count];

        for (int i = 0; i < syncedRecordedHipPositions.Length; i++)
        {
            syncedRecordedHipPositions[i] = (Vector3)recordedHipPositions[i].Reference;
        }

        //Rotations
        Quaternion[] firstRotationElement = (Quaternion[])recorderBoneRotations[0].Reference;
        syncedRecordedBoneRotations = new Quaternion[firstRotationElement.Length * recorderBoneRotations.Count];

        int count = 0;
        for (int i = 0; i < recorderBoneRotations.Count; i++)
        {
            Quaternion[] currentRotationElement = (Quaternion[])recorderBoneRotations[i].Reference;

            for (int j = 0; j < firstRotationElement.Length; j++)
            {
                syncedRecordedBoneRotations[count++] = currentRotationElement[j];
            }
        }

        //Height
        syncedPlayerHeight = selectedPlayer.GetAvatarEyeHeightAsMeters();

        //Record data
        PrepareReplayData();

        //Sync
        RequestSerialization();
    }

    public void RecordLocation(float timeForReference)
    {
        if (!Networking.IsOwner(gameObject) || !doRecordIfOwnerToggle.isOn)
        {
            SetReplayLocations(Time.time - linkedController.ReplayStartTime);

            return;
        }

        //Vector3 avatarPosition = (linkedPlayer.isLocal) ? linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position : linkedPlayer.GetPosition(); //Only needed for rotation I think
        Vector3 avatarPosition = selectedPlayer.GetBonePosition(HumanBodyBones.Hips);
        Quaternion[] boneRotations = GetBoneRotations();

        recordedHipPositions.Add(new DataToken((object)avatarPosition));
        recorderBoneRotations.Add(new DataToken((object)boneRotations));

        RecordedTime = timeForReference;
    }

    public void SetReplayLocations(float replayTime)
    {
        if (syncedRecordedHipPositions.Length == 0) return;

        if (!DoReplay) return;

        //Step
        int earlyStep = (replayTime > 0f) ? ((int)(replayTime / timeStep)) : 0;
        if (earlyStep > recordedSteps - 2)
        {
            if (recordedSteps == 1) return;
            else if (recordedSteps == 1)
            {
                linkedAvatarModelMover.SetAvatarData(syncedRecordedHipPositions[0], syncedRecordedBoneRotations);
            }
            earlyStep = recordedSteps - 2;
        }
        int lateStep = earlyStep + 1;

        float subStepLerpValue = Mathf.Clamp01((replayTime - earlyStep * timeStep) / timeStep);

        //Position
        Vector3 earlyAvatarPosition = syncedRecordedHipPositions[earlyStep];
        Vector3 lateAvatarPosition = syncedRecordedHipPositions[lateStep];
        Vector3 playerPosition = Vector3.Lerp(earlyAvatarPosition, lateAvatarPosition, subStepLerpValue);

        //Rotations
        Quaternion[] boneRotations = new Quaternion[bones];

        for (int i = 0; i < bones; i++)
        {
            Quaternion earlyBoneRotation = syncedRecordedBoneRotations[earlyStep * bones + i];
            Quaternion lateBoneRotation = syncedRecordedBoneRotations[lateStep * bones + i];

            boneRotations[i] = Quaternion.Lerp(earlyBoneRotation, lateBoneRotation, subStepLerpValue);
        }

        //Set avatar
        linkedAvatarModelMover.SetAvatarData(playerPosition, boneRotations);
    }

    //UI events
    public void IncreasePlayerIndex()
    {
        selectedPlayerInPlayerIndex++;

        if (selectedPlayerInPlayerIndex >= players.Length) selectedPlayerInPlayerIndex = 0;

        SelectedPlayer = players[selectedPlayerInPlayerIndex];
    }

    public void DecreasePlayerIndex()
    {
        selectedPlayerInPlayerIndex--;

        if (selectedPlayerInPlayerIndex < 0) selectedPlayerInPlayerIndex = players.Length - 1;

        SelectedPlayer = players[selectedPlayerInPlayerIndex];
    }

    public void SelectOwnPlayerIndex()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].isLocal) selectedPlayerInPlayerIndex = i;
        }

        SelectedPlayer = localPlayer;
    }

    public void UpdateDoReplayToggle()
    {
        if (linkedController.AllowReplayChange)
        {
            linkedAvatarModelMover.gameObject.SetActive(DoReplay);

            linkedController.UpdateReplaySyncFromTogglesIfOwner();
        }
        else
        {
            linkedController.SetReplayStatesFromSync();
        }
    }

    public void RequestOwnership()
    {
        SetPlayerAsOwner();

        RequestSerialization();
    }

    //VRChat functions
    public override void OnPreSerialization()
    {
        base.OnPreSerialization();
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        base.OnPostSerialization(result);
    }

    public override void OnDeserialization()
    {
        if (!setupRan)
        {
            Debug.LogWarning("Setup did not run");
            return;
        }

        base.OnDeserialization();
        
        PrepareReplayData();

        RecordedTime = recordedTime; //Update text

        linkedAvatarModelMover.transform.localScale = syncedPlayerHeight * Vector3.one;

        linkedController.UpdateMaxTime();

    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        SetOwnerText(player);
    }

    public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
    {
        if (player != selectedPlayer) return;

        linkedAvatarModelMover.transform.localScale = player.GetAvatarEyeHeightAsMeters() * Vector3.one;
    }
}