using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarCollision : MonoBehaviour
{
    public float stopDuration = 3f;
    public bool destroyOnCollision = false;

    private bool alreadyHit = false;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (alreadyHit) return;
        if (!collision.gameObject.CompareTag("Car")) return;

        var otherCollision = collision.gameObject.GetComponent<CarCollision>();
        if (otherCollision != null && otherCollision.alreadyHit) return;

        alreadyHit = true;
        if (otherCollision != null) otherCollision.alreadyHit = true;

        var myMover = GetComponent<WaypointMover>();
        var otherMover = collision.gameObject.GetComponent<WaypointMover>();

        if (myMover != null) myMover.SetStopped(true);
        if (otherMover != null) otherMover.SetStopped(true);

        if (destroyOnCollision)
        {
            Destroy(gameObject);
            Destroy(collision.gameObject);
            return;
        }

        StartCoroutine(ResumeAfterDelay(myMover, otherMover, otherCollision));
    }

    private IEnumerator ResumeAfterDelay(WaypointMover myMover, WaypointMover otherMover, CarCollision otherCollision)
    {
        yield return new WaitForSeconds(stopDuration);
        if (myMover != null) myMover.SetStopped(false);
        if (otherMover != null) otherMover.SetStopped(false);
        alreadyHit = false;
        if (otherCollision != null) otherCollision.alreadyHit = false;
    }
}
