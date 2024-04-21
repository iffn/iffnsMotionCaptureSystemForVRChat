
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum GetOrSetDataOptions
{
    Get,
    Set
}

public class SaveSlotButton : UdonSharpBehaviour
{
    [SerializeField] SaveSlot linkedSaveSlot;
    [SerializeField] int slotIndexFrom0;
    [SerializeField] GetOrSetDataOptions getOrSetDataOption;

    public void ClickFromButton()
    {
        switch (getOrSetDataOption)
        {
            case GetOrSetDataOptions.Get:
                linkedSaveSlot.LoadFromSlot(slotIndexFrom0);
                break;
            case GetOrSetDataOptions.Set:
                linkedSaveSlot.SaveToSlot(slotIndexFrom0);
                break;
            default:
                break;
        }
    }
}
