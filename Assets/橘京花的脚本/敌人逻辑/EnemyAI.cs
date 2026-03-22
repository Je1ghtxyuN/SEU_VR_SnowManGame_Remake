using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Ѳ������")]
    public Transform[] patrolPoints; // Ѳ�ߵ�����
    public float patrolSpeed = 3f; // Ѳ���ƶ��ٶ�
    public float waitTime = 1f; // ����Ѳ�ߵ��ĵȴ�ʱ��
    public float rotationSpeed = 5f; // ת���ٶ�

    [Header("��Ҽ��")]
    public float detectionRange = 10f; // �����ҵķ�Χ
    public float attackRange = 7f; // ������ҵķ�Χ
    public LayerMask playerLayer; // ������ڲ�
    public LayerMask obstacleLayer; // �ϰ����(�������߼��)

    [Header("��������")]
    public GameObject snowballPrefab; // ѩ��Ԥ����
    public Transform firePoint; // �����
    public float attackCooldown = 2f; // ������ȴʱ��
    public float projectileSpeed = 10f; // ѩ���ٶ�

    [Header("����")]
    public Animator animator; // ����������
    public string walkAnimParam = "isWalking"; // ���߶�������
    public string attackAnimParam = "Attack"; // ��������������
    public string dieAnimParam = "die";

    private int currentPatrolIndex = 0; // ��ǰѲ�ߵ�����
    private bool isWaiting = false; // �Ƿ��ڵȴ�
    private bool isChasing = false; // �Ƿ���׷�����
    private bool isAttacking = false; // �Ƿ��ڹ���
    private Transform player; // �������
    private float lastAttackTime; // �ϴι���ʱ��
    private bool isDead = false; // ����״̬��־

    void Start()
    {
        // ���û��ָ�����������������Ի�ȡ
        if (animator == null)
            animator = GetComponent<Animator>();

        // ���û��Ѳ�ߵ㣬ʹ������λ����ΪΨһѲ�ߵ�
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = new Transform[1];
            patrolPoints[0] = new GameObject("PatrolPoint").transform;
            patrolPoints[0].position = transform.position;
        }

        // ��ʼѲ��
        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        // ���������ֱ�ӷ��ز�ִ���κ�AI��Ϊ
        if (isDead) return;

        // ������
        DetectPlayer();

        // �������׷����ң�ת�����
        if (isChasing && player != null)
        {
            FaceTarget(player.position);
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // ���������ֹͣѲ��Э��
            if (isDead) yield break;

            // �������׷��򹥻�״̬��ִ��Ѳ��
            if (!isChasing && !isAttacking)
            {
                // �ƶ�����ǰѲ�ߵ�
                Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
                if (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    // �������߶���
                    if (animator != null)
                        animator.SetBool(walkAnimParam, true);

                    // �ƶ���ת��
                    FaceTarget(targetPos);
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);
                }
                else
                {
                    // ����Ѳ�ߵ㣬�ȴ�һ��ʱ��
                    if (animator != null)
                        animator.SetBool(walkAnimParam, false);

                    if (!isWaiting)
                    {
                        isWaiting = true;
                        yield return new WaitForSeconds(waitTime);
                        isWaiting = false;

                        // �л�����һ��Ѳ�ߵ�
                        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    }
                }
            }
            yield return null;
        }
    }

    void DetectPlayer()
    {
        // �����������������
        if (isDead) return;

        // ��ⷶΧ�ڵ����
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

        if (hitColliders.Length > 0)
        {
            // ���賡����ֻ��һ�����
            player = hitColliders[0].transform;

            // ����Ƿ�������(û���ϰ����赲)
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                // ����ڼ�ⷶΧ���ҿɼ�
                isChasing = true;

                // �������ڹ�����Χ�ڣ����Թ���
                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        StartCoroutine(AttackPlayer());
                    }
                }
                else
                {
                    // ׷�����
                    ChasePlayer();
                }
            }
            else
            {
                // ��ұ��ϰ����赲������Ѳ��
                isChasing = false;
            }
        }
        else
        {
            // û�м�⵽��ң�����Ѳ��
            isChasing = false;
            player = null;
        }
    }

    void ChasePlayer()
    {
        // �����������׷�����
        if (isDead) return;

        if (player != null)
        {
            // �������߶���
            if (animator != null)
                animator.SetBool(walkAnimParam, true);

            // ������ƶ�
            transform.position = Vector3.MoveTowards(transform.position, player.position, patrolSpeed * Time.deltaTime);
        }
    }

    IEnumerator AttackPlayer()
    {
        // �����������ִ�й���
        if (isDead) yield break;

        if (isAttacking || player == null)
        {
    #if UNITY_EDITOR
        Debug.Log($"��������ֹ - isAttacking:{isAttacking} player:{player != null}");
#endif
            yield break;
        }

        isAttacking = true;
        lastAttackTime = Time.time;

#if UNITY_EDITOR
        Debug.Log($"��ʼ���� - ʱ��:{Time.time}");
#endif

        // ������������
#if UNITY_EDITOR
        Debug.Log("���ò�����Attack������");
#endif
        animator.ResetTrigger(attackAnimParam); // ������
        animator.SetTrigger(attackAnimParam);  // �ٴ���
   

        // �ȴ�����ǰҡ(����ʵ�ʶ�������)
        float animationLeadTime = 0.3f;
        yield return new WaitForSeconds(animationLeadTime);
#if UNITY_EDITOR
        Debug.Log($"����ѩ�� - ʱ��:{Time.time}");
#endif

        // ����ѩ��
        if (snowballPrefab != null && firePoint != null)
        {
            try
            {
                GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.identity);
#if UNITY_EDITOR
                Debug.Log($"ѩ��ʵ�����ɹ� {snowball.name}");
#endif

                Rigidbody rb = snowball.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // �����������ˮƽ����
                    Vector3 horizontalDirection = (player.position - firePoint.position).normalized;
                    horizontalDirection.y = 0; // ����ˮƽ

                    // �������ϽǶȣ�����15-30�ȣ�
                    float launchAngle = 20f; // �Ƕȿɵ�
                    float radians = launchAngle * Mathf.Deg2Rad;

                    // ���շ��䷽�򣨴������ߣ�
                    Vector3 launchDirection = new Vector3(
                        horizontalDirection.x * Mathf.Cos(radians),
                        Mathf.Sin(radians),
                        horizontalDirection.z * Mathf.Cos(radians)
                    ).normalized;

                    // ���ӻ�����
                    Debug.DrawRay(firePoint.position, launchDirection * 5f, Color.cyan, 2f);

                    rb.velocity = launchDirection * projectileSpeed;

                    // ������תЧ��
                    rb.angularVelocity = new Vector3(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f)
                    );

                    Destroy(snowball, 5f);
                }
                else
                {
                    Debug.LogError("ѩ��ȱ��Rigidbody���");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ѩ��ʵ����ʧ��: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"ѩ��Ԥ����:{snowballPrefab != null} �����:{firePoint != null}");
        }

        // �ȴ�ʣ����ȴʱ��
        float remainingCooldown = Mathf.Max(0, attackCooldown - animationLeadTime);
#if UNITY_EDITOR
        Debug.Log($"�ȴ���ȴʱ��: {remainingCooldown}��");
#endif
        yield return new WaitForSeconds(remainingCooldown);

        isAttacking = false;
#if UNITY_EDITOR
        Debug.Log($"�������� - ʱ��:{Time.time} �´οɹ���ʱ��:{lastAttackTime + attackCooldown}");
#endif
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // ����ˮƽ��ת
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // ���Ƽ�ⷶΧ�͹�����Χ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // ��ǵ�������
    public void MarkAsDead()
    {
        isDead = true;
    }
}