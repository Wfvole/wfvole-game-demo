using UnityEngine;
using System.Collections.Generic;

public class GuardPatrol : MonoBehaviour
{
    [Header("巡逻路径")]
    public List<Vector3> waypoints;      // 世界坐标路径点
    public float moveSpeed = 2f;
    public float stoppingDistance = 0.2f;
    public bool pingPong = true;         // true: 往返, false: 单向循环

    private int currentIndex = 0;
    private bool movingForward = true;

    void Start()
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            //Debug.LogWarning("守卫路径点不足，禁用巡逻");
            Destroy(gameObject);
            enabled = false;
            return;
        }
        transform.position = waypoints[0];
        currentIndex = 1;
    }

    void Update()
    {
        Vector3 target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= stoppingDistance)
        {
            if (pingPong)
            {
                if (movingForward)
                {
                    if (currentIndex + 1 < waypoints.Count)
                        currentIndex++;
                    else
                        movingForward = false;
                }
                else
                {
                    if (currentIndex - 1 >= 0)
                        currentIndex--;
                    else
                        movingForward = true;
                }
            }
            else
            {
                currentIndex = (currentIndex + 1) % waypoints.Count;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&&ThirdPersonShooterController.Instance!=null)
        {
            ThirdPersonShooterController.Instance.ApplyLookedByGuard(0.5f,10f);
            UImanager.Instance.DialogTextMgr("被守卫发现，速度降低一半，持续10s！");
        }
        if (other.CompareTag("playerAtk"))
        {
            Destroy(gameObject);
        }
    }
}
