using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverShow;
    private Animator animator;
    private Toggle toggle;

    private bool alreadyDo;
    private bool isHovered = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        toggle = GetComponent<Toggle>();
    }

    private void Update()
    {
        bool isSelected = EventSystem.current.currentSelectedGameObject == gameObject;
        hoverShow.SetActive(isHovered || isSelected);
        if (toggle.isOn && !alreadyDo)
        {
            animator.SetTrigger("Normal");
            alreadyDo = true;
        }

        if (!isSelected)
        {
            alreadyDo = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}