using GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMoveCard : MonoBehaviour
{
    public RectTransform rect;
    public Vector2 onOffset = new Vector2(0, 0);
    public PieceType piece;

    private Vector2 originalPos;

    [HideInInspector] public Toggle toggle;


    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (rect == null)
            rect = GetComponent<RectTransform>();

        originalPos = rect.anchoredPosition;
    }

    public void SetState(bool isOn)
    {
        rect.anchoredPosition = isOn
            ? originalPos + onOffset
            : originalPos;
        toggle.isOn = isOn;

        SkillTreeUIManager.Instance.UpdateSimpleSkillPanel(piece);

    }

    public void ResetPosition()
    {
        SetState(false);
    }

}
