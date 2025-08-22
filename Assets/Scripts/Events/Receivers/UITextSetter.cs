using System.Collections;
using TMPro;
using UnityEngine;

namespace Events
{
    public class UITextSetter : MonoBehaviour
    {
        private TMP_Text myTxt;
        private Coroutine _currentRoutine;
        [SerializeField]private GameObject myObj;

        private void Awake()
        {
            myTxt = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            EventBus<TriggerUITextEvent>.Subscribe(OnChangeUI);
            EventBus<OpenObjectEvent>.Subscribe(OnSetObject);
        }

        private void OnDisable()
        {
            EventBus<TriggerUITextEvent>.Unsubscribe(OnChangeUI);
            EventBus<OpenObjectEvent>.Unsubscribe(OnSetObject);

        }

        private void OnChangeUI(TriggerUITextEvent e)
        {
            if (_currentRoutine != null)
                StopCoroutine(_currentRoutine);

            _currentRoutine = StartCoroutine(ShowTextTemporarily(e.textToShow, e.displayTime));
        }

        private IEnumerator ShowTextTemporarily(string text, float duration)
        {
            myTxt.text = text;
            yield return new WaitForSeconds(duration);
            myTxt.text = string.Empty;
        }
        private void OnSetObject(OpenObjectEvent e)
        {
            e.objectToOpen.SetActive(e.objectToOpen);
        }
    }
}