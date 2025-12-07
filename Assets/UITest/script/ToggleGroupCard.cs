using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleGroupCard : MonoBehaviour
{
    public ToggleMoveCard[] items;

    void Start()
    {
        foreach (var item in items)
        {
            var localItem = item;

            localItem.toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(localItem, isOn);
            });
        }
    }

    void OnToggleChanged(ToggleMoveCard changedItem, bool isOn)
    {
        if (isOn)
        {
            changedItem.SetState(true);

            foreach (var item in items)
            {
                if (item != changedItem)
                    item.ResetPosition();
            }

        }
        else
        {
            changedItem.SetState(false);
        }

    }
}
