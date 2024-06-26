﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class PlayerEmulator : UdonSharpBehaviour
{
    [SerializeField] AvatarModelMover linkedAvatarModelMover;
    [SerializeField, Multiline(10)] string saveData;

    public Vector3[] hipPositions = new Vector3[0];
    public Quaternion[] boneRotations = new Quaternion[0];
    public float recordedTime;
    public float playerHeight;
    public int recordedSteps;
    public float timeStep;
    public int bones;

    float startTime = 0;
    float endTime;
    bool running = false;

    public void Start()
    {
        Import();
        linkedAvatarModelMover.Setup();
    }

    public void Import()
    {
        saveData = saveData.Replace("\r", "");

        string[] lines = saveData.Split('\n');

        //Debug.Log($"{nameof(lines)} = {lines.Length}");

        string[] currentSplit;
        string currentLine;
        bool worked;

        currentLine = lines[0];
        currentSplit = currentLine.Split('\t');

        if (currentSplit[1].Equals("1"))
        {
            //RecordingTime
            int currentLineIndex = 1;

            currentLine = lines[currentLineIndex++];
            currentSplit = currentLine.Split('\t');
            worked = float.TryParse(currentSplit[1], out float recordedTime);

            //Height
            currentLine = lines[currentLineIndex++];
            currentSplit = currentLine.Split('\t');
            worked = float.TryParse(currentSplit[1], out float playerHeight);

            //TimeSteps
            currentLine = lines[currentLineIndex++];
            currentSplit = currentLine.Split('\t');
            worked = int.TryParse(currentSplit[1], out int timeSteps);

            //Positions
            Vector3[] hipPositions = new Vector3[timeSteps];

            for (int i = 0; i < timeSteps; i++)
            {
                currentLine = lines[currentLineIndex++];
                currentSplit = currentLine.Split('\t');

                worked = float.TryParse(currentSplit[0], out hipPositions[i].x);
                worked = float.TryParse(currentSplit[1], out hipPositions[i].y);
                worked = float.TryParse(currentSplit[2], out hipPositions[i].z);
            }

            //Bones
            currentLine = lines[currentLineIndex++];
            currentSplit = currentLine.Split('\t');
            worked = int.TryParse(currentSplit[1], out int bones);

            //Rotations
            Quaternion[] boneRotations = new Quaternion[bones * timeSteps];

            /*
            for (int i = 0; i < timeSteps; ++i)
            {
                currentLine = lines[currentLineIndex++];
                currentSplit = currentLine.Split('\t');

                for (int j = 0; j < currentSplit.Length; j++)
                {
                    worked = float.TryParse(currentSplit[j * 4], out boneRotations[i * bones + j].x);
                    worked = float.TryParse(currentSplit[j * 4 + 1], out boneRotations[i * bones + j].y);
                    worked = float.TryParse(currentSplit[j * 4 + 2], out boneRotations[i * bones + j].z);
                    worked = float.TryParse(currentSplit[j * 4 + 3], out boneRotations[i * bones + j].w);
                }
            }
            */

            Debug.Log($"{nameof(recordedTime)} = {recordedTime}");
            Debug.Log($"{nameof(playerHeight)} = {playerHeight}");
            Debug.Log($"{nameof(playerHeight)} = {playerHeight}");
            Debug.Log($"{nameof(hipPositions)} = {hipPositions.Length}");
            Debug.Log($"{nameof(boneRotations)} = {boneRotations.Length}");

            SetData(recordedTime, playerHeight, hipPositions, boneRotations);
        }
    }

    public void SetData(float recordedTime, float playerHeight, Vector3[] hipPositions, Quaternion[] boneRotations)
    {
        this.recordedTime = recordedTime;
        this.playerHeight = playerHeight;
        this.hipPositions = hipPositions;
        this.boneRotations = boneRotations;

        timeStep = recordedTime / recordedSteps;
        recordedSteps = hipPositions.Length;
        bones = boneRotations.Length / hipPositions.Length;

    }

    public void StartPlaying()
    {
        running = true;
        startTime = Time.time;
        endTime = Time.time + recordedTime;

        Debug.Log($"{nameof(recordedTime)} = {recordedTime}");
        Debug.Log($"{nameof(recordedSteps)} = {recordedSteps}");
    }

    private void Update()
    {
        if (running)
        {
            if(Time.time > endTime)
            {
                running = false;
            }
            else
            {
                SetReplayLocations(Time.time - startTime);
            }
        }
    }

    void SetReplayLocations(float replayTime)
    {
        //Step
        int earlyStep = (replayTime > 0f) ? ((int)(replayTime / timeStep)) : 0;
        if (earlyStep > recordedSteps - 2)
        {
            if (recordedSteps == 1) return;
            else if (recordedSteps == 1)
            {
                linkedAvatarModelMover.SetAvatarData(hipPositions[0], this.boneRotations);
            }
            earlyStep = recordedSteps - 2;
        }
        int lateStep = earlyStep + 1;

        float subStepLerpValue = Mathf.Clamp01((replayTime - earlyStep * timeStep) / timeStep);

        //Position
        Vector3 earlyAvatarPosition = hipPositions[earlyStep];
        Vector3 lateAvatarPosition = hipPositions[lateStep];
        Vector3 playerPosition = Vector3.Lerp(earlyAvatarPosition, lateAvatarPosition, subStepLerpValue);

        //Rotations
        Quaternion[] boneRotations = new Quaternion[bones];

        for (int i = 0; i < bones; i++)
        {
            Quaternion earlyBoneRotation = this.boneRotations[earlyStep * bones + i];
            Quaternion lateBoneRotation = this.boneRotations[lateStep * bones + i];

            boneRotations[i] = Quaternion.Lerp(earlyBoneRotation, lateBoneRotation, subStepLerpValue);
        }

        //Set avatar
        linkedAvatarModelMover.SetAvatarData(playerPosition, boneRotations);
    }

    public override void Interact()
    {
        base.Interact();

        StartPlaying();
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        base.InputJump(value, args);

        if(value)
        {
            StartPlaying();
        }
    }
}
