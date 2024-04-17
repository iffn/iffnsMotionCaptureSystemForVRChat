
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AvatarModelMover : UdonSharpBehaviour
{
    //Unity assignments
    [SerializeField] Transform avatarTransform;
    [SerializeField] Transform[] sceneAvatarBones;

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
