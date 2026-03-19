using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class TestButton : MonoBehaviour
    {
        public Button Button;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                Button.Select();
            }
        }
    }
}