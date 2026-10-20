using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public sealed class TF2MapRespawn : MonoBehaviour
{
    public Transform spawn;
    public float minimumHeight = -30;
    void FixedUpdate()
    {
        if (spawn == null || transform.position.y >= minimumHeight) return;
        var body = GetComponent<Rigidbody>();
        body.position = spawn.position;
        body.rotation = spawn.rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}
