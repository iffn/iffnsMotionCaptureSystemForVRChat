
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AvatarModelMover : UdonSharpBehaviour
{
    //Unity assignments
    [SerializeField] Transform avatarTransform;

    [SerializeField] Transform Hips;
    [SerializeField] Transform LeftUpperLeg;
    [SerializeField] Transform RightUpperLeg;
    [SerializeField] Transform LeftLowerLeg;
    [SerializeField] Transform RightLowerLeg;
    [SerializeField] Transform LeftFoot;
    [SerializeField] Transform RightFoot;
    [SerializeField] Transform Spine;
    [SerializeField] Transform Chest;
    [SerializeField] Transform UpperChest;
    [SerializeField] Transform Neck;
    [SerializeField] Transform Head;
    [SerializeField] Transform LeftShoulder;
    [SerializeField] Transform RightShoulder;
    [SerializeField] Transform LeftUpperArm;
    [SerializeField] Transform RightUpperArm;
    [SerializeField] Transform LeftLowerArm;
    [SerializeField] Transform RightLowerArm;
    [SerializeField] Transform LeftHand;
    [SerializeField] Transform RightHand;
    [SerializeField] Transform LeftToes;
    [SerializeField] Transform RightToes;
    /*
    [SerializeField] Transform LeftEye;
    [SerializeField] Transform RightEye;
    [SerializeField] Transform Jaw;
    */

    Transform[] sceneAvatarBones;

    public void Setup()
    {
        sceneAvatarBones = new Transform[]
        {
            Hips,
            LeftUpperLeg,
            RightUpperLeg,
            LeftLowerLeg,
            RightLowerLeg,
            LeftFoot,
            RightFoot,
            Spine,
            Chest,
            UpperChest,
            Neck,
            Head,
            LeftShoulder,
            RightShoulder,
            LeftUpperArm,
            RightUpperArm,
            LeftLowerArm,
            RightLowerArm,
            LeftHand,
            RightHand,
            LeftToes,
            RightToes
            /* ,
            LeftEye,
            RightEye,
            Jaw*/
        };
    }

    //Public functions
    public void SetAvatarData(Vector3 position, Quaternion[] rotations)
    {
        avatarTransform.position = position;

        int minBoneCount = Mathf.Min(sceneAvatarBones.Length, rotations.Length);

        for(int i = 0; i < minBoneCount; i++)
        {
            sceneAvatarBones[i].rotation = rotations[i];
        }
    }
}
