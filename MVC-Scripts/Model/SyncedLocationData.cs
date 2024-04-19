//Blocker

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

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
    [UdonSynced] Vector3[] syncedRecordedAvatarPositions;
    [UdonSynced] Quaternion[] syncedRecorderBoneRotations;
    [UdonSynced] float recordingTime;
    [UdonSynced] float syncedPlayerHeight;

    public float RecordTime
    {
        get
        {
            return recordingTime;
        }
        set
        {
            recordingTime = value;
            recordedTimeText.text = $"{recordedTimeText}s";
        }
    }

    //Fixed variables
    DataList recordedAvatarPositions;
    DataList recorderBoneRotations;
    MotionCaptureController linkedController;
    VRCPlayerApi localPlayer;

    //Runtime variables
    bool doRecordIfOwner = false;
    public bool DoReplay { get; private set; } = false;
    VRCPlayerApi linkedPlayer;
    VRCPlayerApi[] players;
    int currentPlayerIndex;

    //Internal functions
    VRCPlayerApi LinkedPlayer
    {
        set
        {
            this.linkedPlayer = value;
            currentPlayerText.text = linkedPlayer.displayName;

        }
    }

    //Unity functions
    private void Start()
    {
        //Note Actual setup controlled by ClearAndSetup function from MotionCaptureController

        //Set states from UI buttons
        DoReplay = doReplayToggle.isOn;
        doRecordIfOwner = doRecordIfOwnerToggle.isOn;
        linkedAvatarModelMover.gameObject.SetActive(DoReplay);
    }

    //Script events
    public void ClearAndSetup(MotionCaptureController linkedController)
    {
        //External data
        if (localPlayer == null) localPlayer = Networking.LocalPlayer;

        //Data creation
        recordedAvatarPositions = new DataList();
        recorderBoneRotations = new DataList();

        //Variable data
        this.linkedController = linkedController;

        //Default values
        linkedPlayer = localPlayer;

        //Setups
        linkedAvatarModelMover.Setup();
    }

    public void UpdatePlayerList(VRCPlayerApi[] players)
    {
        this.players = players;
    }

    public void SetPlayerAsOwner()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);
    }

    public void TransferData(bool sync)
    {
        if (!Networking.IsOwner(gameObject) || !doRecordIfOwner) return;

        //Positions
        syncedRecordedAvatarPositions = new Vector3[recordedAvatarPositions.Count];

        for (int i = 0; i < syncedRecordedAvatarPositions.Length; i++)
        {
            syncedRecordedAvatarPositions[i] = (Vector3)recordedAvatarPositions[i].Reference;
        }

        //Rotations
        Quaternion[] firstRotationElement = (Quaternion[])recorderBoneRotations[0].Reference;
        syncedRecorderBoneRotations = new Quaternion[firstRotationElement.Length * recorderBoneRotations.Count];

        int count = 0;
        for (int i = 0; i < recorderBoneRotations.Count; i++)
        {
            Quaternion[] currentRotationElement = (Quaternion[])recorderBoneRotations[i].Reference;

            for (int j = 0; j < firstRotationElement.Length; j++)
            {
                syncedRecorderBoneRotations[count++] = currentRotationElement[j];
            }
        }

        //Height
        syncedPlayerHeight = linkedPlayer.GetAvatarEyeHeightAsMeters();

        //Sync
        if (sync) RequestSerialization();
    }

    public void RecordLocation(float timeForReference)
    {
        if (!Networking.IsOwner(gameObject) || !doRecordIfOwner)
        {
            SetReplayLocations(Time.time - linkedController.ReplayStartTime);

            return;
        }

        //Vector3 avatarPosition = (linkedPlayer.isLocal) ? linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position : linkedPlayer.GetPosition(); //Only needed for rotation I think
        Vector3 avatarPosition = linkedPlayer.GetPosition();
        Quaternion[] boneRotations = new Quaternion[]
        {
            linkedPlayer.GetBoneRotation(HumanBodyBones.Hips),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftUpperLeg),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightUpperLeg),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftLowerLeg),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightLowerLeg),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftFoot),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightFoot),
            linkedPlayer.GetBoneRotation(HumanBodyBones.Spine),
            linkedPlayer.GetBoneRotation(HumanBodyBones.Chest),
            linkedPlayer.GetBoneRotation(HumanBodyBones.UpperChest),
            linkedPlayer.GetBoneRotation(HumanBodyBones.Neck),
            linkedPlayer.GetBoneRotation(HumanBodyBones.Head),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftShoulder),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightShoulder),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftUpperArm),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightUpperArm),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftLowerArm),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightLowerArm),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftHand),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightHand),
            linkedPlayer.GetBoneRotation(HumanBodyBones.LeftToes),
            linkedPlayer.GetBoneRotation(HumanBodyBones.RightToes),
            //linkedPlayer.GetBoneRotation(HumanBodyBones.LeftEye),
            //linkedPlayer.GetBoneRotation(HumanBodyBones.RightEye),
        };

        recordedAvatarPositions.Add(new DataToken((object)avatarPosition));
        recorderBoneRotations.Add(new DataToken((object)boneRotations));

        recordingTime = timeForReference;
    }

    public void SetReplayLocations(float replayTime)
    {
        if (!DoReplay) return;

        //To move out
        int recordedSteps = syncedRecordedAvatarPositions.Length;
        float timeStep = recordingTime / recordedSteps;
        int bones = syncedRecorderBoneRotations.Length / syncedRecordedAvatarPositions.Length;

        //Step
        int earlyStep = (int)(recordingTime / replayTime);
        if (earlyStep < 0) earlyStep = 0;
        int lateStep = earlyStep + 1;
        if (lateStep > recordedSteps - 1) lateStep = recordedSteps - 1;

        float subStepLerpValue = Mathf.Clamp01((replayTime - earlyStep * timeStep) / timeStep);

        //Position
        Vector3 earlyAvatarPosition = syncedRecordedAvatarPositions[earlyStep];
        Vector3 lateAvatarPosition = syncedRecordedAvatarPositions[lateStep];
        Vector3 playerPosition = Vector3.Lerp(earlyAvatarPosition, lateAvatarPosition, subStepLerpValue);

        //Rotations
        Quaternion[] boneRotations = new Quaternion[bones];

        for (int i = 0; i < bones; i++)
        {
            Quaternion earlyBoneRotation = syncedRecorderBoneRotations[earlyStep * bones + i];
            Quaternion lateBoneRotation = syncedRecorderBoneRotations[lateStep * bones + i];

            boneRotations[i] = Quaternion.Lerp(earlyBoneRotation, lateBoneRotation, subStepLerpValue);
        }

        //Set avatar
        linkedAvatarModelMover.SetAvatarData(playerPosition, boneRotations);
    }

    //UI events
    public void IncreasePlayerIndex()
    {
        currentPlayerIndex++;

        if(currentPlayerIndex >= players.Length) currentPlayerIndex = 0;

        LinkedPlayer = players[currentPlayerIndex];
    }

    public void DecreasePlayerIndex()
    {
        currentPlayerIndex--;

        if (currentPlayerIndex < 0) currentPlayerIndex = players.Length - 1;

        LinkedPlayer = players[currentPlayerIndex];
    }

    public void SelectOwnPlayerIndex()
    {
        currentPlayerIndex = localPlayer.playerId;

        LinkedPlayer = players[currentPlayerIndex];
    }

    public void UpdateDoRecordIfOwnerToggle()
    {
        doRecordIfOwner = doRecordIfOwnerToggle.isOn;
    }

    public void UpdateDoReplayToggleToggle()
    {
        DoReplay = doReplayToggle.isOn;

        linkedAvatarModelMover.gameObject.SetActive(DoReplay);
    }

    public void RequestOwnership()
    {
        SetPlayerAsOwner();
    }

    //VRChat functions
    public override void OnDeserialization()
    {
        base.OnDeserialization();

        RecordTime = recordingTime; //Update text

        linkedAvatarModelMover.transform.localScale = syncedPlayerHeight * Vector3.one;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        currentOwnerText.text = $"[{player.playerId}] {player.displayName}{(player.isLocal ? " (you)" : "")}";
    }
}