using UnityEngine;

public class ThrowingSystem
{
    private GameObject throwablePrefab;
    private GameObject spellPrefab;
    private Transform throwOrigin;
    private float throwForce;

    public ThrowingSystem(GameObject throwablePrefab, GameObject spellPrefab, Transform throwOrigin, float throwForce)
    {
        this.throwablePrefab = throwablePrefab;
        this.spellPrefab = spellPrefab;
        this.throwOrigin = throwOrigin;
        this.throwForce = throwForce;
    }

    public void ThrowObject(Transform player)
    {
        GameObject selectedPrefab = spellPrefab;

        GameObject thrownObject = GameObject.Instantiate(selectedPrefab, throwOrigin.position, throwOrigin.rotation);
        Rigidbody rb = thrownObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 使用螢幕中心點做射線
            //Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Ray ray = new Ray(player.position, player.forward);
            Vector3 throwDirection = ray.direction;

            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }

        Debug.Log($"投擲");
    }

}