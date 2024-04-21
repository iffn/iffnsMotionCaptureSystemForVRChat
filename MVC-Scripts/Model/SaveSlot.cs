using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;

public class SaveSlot : UdonSharpBehaviour
{
    //Unity assignments
    [SerializeField] SyncedLocationData[] linkedDataSyncers;
    [SerializeField] TMPro.TextMeshProUGUI timeIndicationText;
    [SerializeField] InputField transferField;

    //Data storage
    Vector3[] savedRecordedHipPositions = new Vector3[0];
    Quaternion[] savedRecordedBoneRotations = new Quaternion[0];
    float savedRecordedTime;
    float savedPlayerHeight;
    const string rotationPecision = "0.000";
    const string positonPecision = "0.000";


    public void SaveToSlot(int slotNumber)
    {
        if(slotNumber < 0)
        {
            Debug.LogWarning("Warning: Saving to negative number not possible");
            return;
        }

        if (slotNumber > linkedDataSyncers.Length - 1)
        {
            Debug.LogWarning("Warning: Saving to an index larger than the syncers not possible");
            return;
        }

        linkedDataSyncers[slotNumber].syncedRecordedHipPositions = savedRecordedHipPositions;
        linkedDataSyncers[slotNumber].syncedRecordedBoneRotations = savedRecordedBoneRotations;
        linkedDataSyncers[slotNumber].syncedRecordedTime = savedRecordedTime;
        linkedDataSyncers[slotNumber].syncedPlayerHeight = savedPlayerHeight;
    }

    public void LoadFromSlot(int slotNumber)
    {
        if (slotNumber < 0)
        {
            Debug.LogWarning("Warning: Loading from negative number not possible");
            return;
        }

        if (slotNumber > linkedDataSyncers.Length - 1)
        {
            Debug.LogWarning("Warning: Loading from an index larger than the syncers not possible");
            return;
        }

        savedRecordedHipPositions = linkedDataSyncers[slotNumber].syncedRecordedHipPositions;
        savedRecordedBoneRotations = linkedDataSyncers[slotNumber].syncedRecordedBoneRotations;
        savedRecordedTime = linkedDataSyncers[slotNumber].syncedRecordedTime;
        savedPlayerHeight = linkedDataSyncers[slotNumber].syncedPlayerHeight;

        timeIndicationText.text = $"t={savedRecordedTime}s\nat{Time.time}";
    }

    public void ExportData()
    {
        string outputText = "";

        outputText += $"Time:\t{savedRecordedTime}\n";
        outputText += $"Height:\t{savedPlayerHeight}\n";

        int timeSteps = savedRecordedHipPositions.Length;

        outputText += $"Positions xyz:\t{timeSteps}\n";
        foreach (Vector3 position in savedRecordedHipPositions)
        {
            outputText += $"{position.x.ToString(positonPecision)}\t{position.y.ToString(positonPecision)}\t{position.z.ToString(positonPecision)}\n";
        }

        int bones = savedRecordedBoneRotations.Length / timeSteps;
        int rotationIndex = 0;

        outputText += $"Rotations wxyz, bones:\t{bones}\n";
        for(int timeIndex = 0; timeIndex < timeSteps; timeIndex++)
        {
            Quaternion rotation;

            for (int boneIndex = 0; boneIndex < bones - 1; boneIndex++)
            {
                rotation = savedRecordedBoneRotations[rotationIndex];

                outputText += $"{rotation.w.ToString(rotationPecision)}\t{rotation.x.ToString(rotationPecision)}\t{rotation.y.ToString(rotationPecision)}\t{rotation.z.ToString(rotationPecision)}\t";

                rotationIndex++;
            }

            rotation = savedRecordedBoneRotations[rotationIndex];

            outputText += $"{rotation.w.ToString(rotationPecision)}\t{rotation.x.ToString(rotationPecision)}\t{rotation.y.ToString(rotationPecision)}\t{rotation.z.ToString(rotationPecision)}\n";

            rotationIndex++;
        }

        transferField.text = outputText;
    }
}
