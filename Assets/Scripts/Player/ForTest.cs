using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class ForTest : MonoBehaviour
    {
        private GameObject player;
        private Health playerHP;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            playerHP = player.GetComponent<Health>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))//清除重生點重置
            {
                SceneController.Instance.LoadSceneWithLoading("MainMenu");
            }
            if (Input.GetKeyDown(KeyCode.F9))//清除重生點重置
            {
                //CheckpointManager.Instance.ClearCheckpoint();
                PlayerPrefs.DeleteAll();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            if (Input.GetKeyDown(KeyCode.F8))//清除重生點重置
            {
                PlayerInputHandler.Instance.SetLockMovement(!PlayerInputHandler.Instance.InputLock);
            }
        }
    }
}