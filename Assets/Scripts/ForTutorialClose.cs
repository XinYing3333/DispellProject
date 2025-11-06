using UnityEngine;

namespace DefaultNamespace
{
    public class ForTutorialClose : MonoBehaviour
    {
        void Start()
        {
            if (ForTutorialDemo.isTutorialFinished)
            {
                gameObject.SetActive(false);
            }
        }
    }
}