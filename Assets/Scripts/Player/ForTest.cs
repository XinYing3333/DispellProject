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
            if (Input.GetKeyDown(KeyCode.F1))//清除重生點重置
            {
                CheckpointManager.Instance.ClearCheckpoint();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                playerHP.enabled = !playerHP.enabled;
            }
        }
    }
}