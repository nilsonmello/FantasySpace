using UnityEngine;

public class FakeSoundEmitter : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO channel;

    [SerializeField] private float searchRadius = 4f;
    [SerializeField] private float priority = 1f;

    [SerializeField] private Transform emitPoint;

    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0f, 0.5f);

    private float emitFlashUntil = -1f;
    private const float EmitFlashDuration = 0.5f;

    public void EmitSound()
    {
        if (channel == null)
        {
            Debug.LogWarning($"{name}: missing soundeventso", this);
            return;
        }

        Vector2 pos = emitPoint != null ? (Vector2)emitPoint.position : (Vector2)transform.position;
        channel.RaiseSound(pos, searchRadius, priority);

        emitFlashUntil = Time.time + EmitFlashDuration;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;

        Vector2 pos = emitPoint != null ? (Vector2)emitPoint.position : (Vector2)transform.position;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(pos, searchRadius);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && Time.time < emitFlashUntil)
        {
            Vector2 pos = emitPoint != null ? (Vector2)emitPoint.position : (Vector2)transform.position;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(pos, searchRadius * 1.05f);
        }
    }
}