using UnityEngine;

public class IK_Master3DOF : MonoBehaviour
{
    [Header("Chain Roots")]
    public Transform axis0;   // +X 회전
    public Transform axis1;   // +Z 회전
    public Transform axis2;   // +Y 프리즘
    public Transform handle;  // 말단(Handle) 기준점
    public Transform eeTarget;// 목표(이걸 움직여!)

    [Header("Local Offsets (from each joint)")]
    // axis1의 localPosition = dim0
    // axis2의 localPosition = dim1 + axis2Local * q2
    // handle의 localPosition = dim2
    public Vector3 dim0 = new Vector3(-0.566f, 0f, 0f);
    public Vector3 dim1 = new Vector3(0f, -1.263f, 0f);
    public Vector3 dim2 = new Vector3(-0.05f, 0f, 0f); // 손잡이 끝 오프셋(필요시 조정)

    [Header("Local Joint Axes")]
    public Vector3 axis0Local = Vector3.right;   // +X
    public Vector3 axis1Local = Vector3.forward; // +Z
    public Vector3 axis2Local = Vector3.up;      // +Y (프리즘 진행방향)

    [Header("Joint States")]
    public float q0Deg = 0f; // Axis_0 각도(도)
    public float q1Deg = 0f; // Axis_1 각도(도)
    public float q2 = 0f;    // Axis_2 신장(미터)

    [Header("Limits")]
    public float q0Min = -30f, q0Max = 30f;
    public float q1Min = -20f, q1Max = 90f;
    public float q2Min = 0.00f, q2Max = 0.30f;   // 프리즘 스트로크 길이(프로젝트에 맞게 조정)

    [Header("IK Params")]
    public int iterations = 64;
    public float step = 0.25f;     // 업데이트 게인(0.1~0.5 사이에서 조정)
    public float stopEps = 1e-3f;  // 위치 오차 정지 임계값(미터)

    void Start()
    {
        // 초기 배치(로컬)
        axis1.localPosition = dim0;
        axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;
        ApplyPose();
    }

    void Update()
    {
        if (!axis0 || !axis1 || !axis2 || !handle || !eeTarget) return;

        for (int k = 0; k < iterations; k++)
        {
            // 현재 포즈 적용(Transform 갱신)
            ApplyPose();

            // 세계 좌표들
            Vector3 p0 = axis0.position;
            Vector3 p1 = axis1.position;
            Vector3 p2w = axis2.position;
            Vector3 pe = handle.position;           // end-effector (handle)
            Vector3 pt = eeTarget.position;         // target

            Vector3 err = pt - pe;                  // 3x1
            if (err.sqrMagnitude < stopEps * stopEps) break;

            // 세계 좌표 회전축(단위벡터)
            Vector3 w0 = axis0.TransformDirection(axis0Local).normalized;
            Vector3 w1 = axis1.TransformDirection(axis1Local).normalized;
            Vector3 w2 = axis2.TransformDirection(axis2Local).normalized; // prismatic

            // 3x3 야코비안의 각 컬럼(위치 영향)
            Vector3 j0 = Vector3.Cross(w0, (pe - p0)); // revolute
            Vector3 j1 = Vector3.Cross(w1, (pe - p1)); // revolute
            Vector3 j2 = w2;                           // prismatic

            // Jacobian Transpose 업데이트
            // dq0, dq1: 라디안, dq2: 미터
            float dq0 = step * Vector3.Dot(j0, err);   // rad
            float dq1 = step * Vector3.Dot(j1, err);   // rad
            float dq2 = step * Vector3.Dot(j2, err);   // m

            // 적용(+클램프)
            q0Deg = Mathf.Clamp(q0Deg + dq0 * Mathf.Rad2Deg, q0Min, q0Max);
            q1Deg = Mathf.Clamp(q1Deg + dq1 * Mathf.Rad2Deg, q1Min, q1Max);
            q2 = Mathf.Clamp(q2 + dq2, q2Min, q2Max);

            // 프리즘 로컬 위치 업데이트
            axis2.localPosition = dim1 + axis2Local.normalized * q2;
        }
        // 최종 포즈 반영 1회
        ApplyPose();
    }

    void ApplyPose()
    {
        // 부모-자식 로컬 배치 유지
        axis1.localPosition = dim0;
        axis2.localPosition = dim1 + axis2Local.normalized * q2;
        if (handle) handle.localPosition = dim2;

        // 각도 적용(로컬 축 기준)
        axis0.localRotation = Quaternion.AngleAxis(q0Deg, axis0Local);
        axis1.localRotation = Quaternion.AngleAxis(q1Deg, axis1Local);
        // 프리즘은 회전 없음(필요하면 고정 오프셋 회전 추가 가능)
    }
}
