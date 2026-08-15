using UnityEngine;

public class SpeedBoostItem : MonoBehaviour
{
    [Header("速度增益参数")]
    public float speedMultiplier = 1.5f;   // 速度倍率
    public float duration = 10f;            // 持续时间（秒）

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ThirdPersonShooterController.Instance!=null)
            {
                ThirdPersonShooterController.Instance.ApplySpeedBoost(speedMultiplier, duration);
                // 显示提示（假设已有 ShowPickupTip 方法）
                UImanager.Instance.DialogTextMgr("速度提升！持续时间："+ duration.ToString()+"秒");
                // 播放音效（可选）
                // AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                // 销毁道具
                Destroy(gameObject);
            }
        }
    }
}
