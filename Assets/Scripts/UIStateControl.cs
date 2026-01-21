using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public enum UIState
    {
        Main,
        Menu,
        Gallery,
        Settings,
    }
    
    public class UIStateControl : MonoBehaviour
    {
        private UIState _currentUIState;
        
        [SerializeField]private Animator anim;
        
        [SerializeField]private GameObject menuObj, galleryObj, settingsObj,quitObj,menuConfirmObj;
        [SerializeField]private Button menuBtn, galleryBtn, settingsBtn;

       

        private void Start()
        {
            _currentUIState = UIState.Main;
            anim = GetComponent<Animator>();
            menuBtn = menuObj.GetComponent<Button>();
            galleryBtn = galleryObj.GetComponent<Button>();
            settingsBtn = settingsObj.GetComponent<Button>();
        }

        private void Update()
        {
            //EnsureSelection(); // ★ 關鍵
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("current select : "+ EventSystem.current.currentSelectedGameObject);
            }
        }

        public void SwitchCurrentUIState(int state)
        {
            UIState newState = (UIState)state;
            if (_currentUIState == newState) return;

            _currentUIState = newState;
            SetState();
            EnsureSelection();
            Debug.Log("current state ： "+_currentUIState +"current select : "+ EventSystem.current.currentSelectedGameObject);
        }


        private void SetState()
        {
            switch (_currentUIState)
            {
                case UIState.Main:
                    anim.SetInteger("state",0);
                    menuObj.SetActive(true);
                    galleryObj.SetActive(true);
                    settingsObj.SetActive(true);
                    quitObj.SetActive(true);

                    menuBtn.interactable = true;
                    galleryBtn.interactable = true;
                    settingsBtn.interactable = true;
                    break;

                case UIState.Menu:
                    anim.SetInteger("state",1);
                    menuObj.SetActive(false);
                    galleryObj.SetActive(false);
                    settingsObj.SetActive(false);
                    quitObj.SetActive(true);

                    menuBtn.interactable = false;
                    galleryBtn.interactable = false;
                    settingsBtn.interactable = false;
                    break;

                case UIState.Gallery:
                    anim.SetInteger("state",2);
                    menuObj.SetActive(false);
                    galleryObj.SetActive(false);
                    settingsObj.SetActive(false);
                    quitObj.SetActive(true);

                    menuBtn.interactable = false;
                    galleryBtn.interactable = false;
                    settingsBtn.interactable = false;
                    break;

                case UIState.Settings:
                    anim.SetInteger("state",3);
                    menuObj.SetActive(false);
                    galleryObj.SetActive(false);
                    settingsObj.SetActive(false);
                    quitObj.SetActive(true);

                    menuBtn.interactable = false;
                    galleryBtn.interactable = false;
                    settingsBtn.interactable = false;
                    break;
            }

            EnsureSelection(); // ★ 關鍵
        }


        // public void Quit()
        // {
        //     if (_currentUIState == UIState.Main)
        //     {
        //         anim.SetTrigger("quit");
        //         Debug.Log("關閉選單，回到游戲");
        //     }
        // }
        
        private void EnsureSelection()
        {
            if (EventSystem.current == null) return;
            //if (EventSystem.current.currentSelectedGameObject != null) return;

            GameObject target = null;

            switch (_currentUIState)
            {
                case UIState.Main:
                    target = menuObj;      // Main 頁預設選中 Menu
                    break;
                case UIState.Menu:
                    target = menuConfirmObj;      // Menu confirm 或返回頁
                    break;
                case UIState.Gallery:
                    target = quitObj;      // Gallery 預設回上一層
                    break;
                case UIState.Settings:
                    //target = quitObj;      // Settings 預設回上一層
                    break;
            }

            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

       

    }
}