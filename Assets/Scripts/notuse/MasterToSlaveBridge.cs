using UnityEngine;

public class MasterToSlaveBridge : MonoBehaviour
{
    [Header("Master source")]
    public Transform masterHandle;            // ������ �ڵ�(����)
    public Transform masterAnchor;            // (����) ������ ��ǥ�� ������

    [Header("Slave targets (IKwithTWEE)")]
    public Transform slaveEETarget;           // �����̺� ee_Target
    public Transform slaveEETargetR;          // (����) �����̺� ee_Target_R
    public Transform slaveEETargetL;          // (����) �����̺� ee_Target_L
    public Transform slaveAnchor;             // (����) �����̺� ��ǥ�� ������

    [Header("Pose mapping")]
    public Vector3 positionOffsetLocal = Vector3.zero; // ������ �ڵ� ���ÿ����� �߰� ��ġ ������
    public Vector3 rotationOffsetEuler = Vector3.zero; // �߰� ȸ�� ������(��)
    public bool useAnchors = false;                     // ��ǥ�踦 ��Ŀ �������� ��ȯ����
    public bool mirrorX = false, mirrorY = false, mirrorZ = false; // �� ���� �ɼ�

    [Header("Smoothing")]
    [Range(0f, 1f)] public float posLerp = 1f;          // 1=���, 0.2~0.5=�ε巴��
    [Range(0f, 1f)] public float rotSlerp = 1f;         // 1=���

    [Header("Gripper driving")]
    public Transform masterGripR;   // (����) �����Ϳ��� ������ ���Ͻ� ����Ʈ(������ �켱 ���)
    public Transform masterGripL;   // (����) �����Ϳ��� ���� ���Ͻ� ����Ʈ
    public float gripHalfWidth = 0.05f; // ���Ͻ� ���� �� �ڵ�.right ���� ����(����)

    void Reset()
    {
        posLerp = rotSlerp = 1f;
    }

    void Update()
    {
        if (!masterHandle || !slaveEETarget) return;

        // 1) ���� ���� �б�
        Vector3 srcPos = masterHandle.position;
        Quaternion srcRot = masterHandle.rotation;

        // ���� �������� �ڵ� ������ �������� ����
        srcPos += masterHandle.TransformVector(positionOffsetLocal);
        srcRot = srcRot * Quaternion.Euler(rotationOffsetEuler);

        // 2) (����) ��Ŀ ���� ��ȯ
        if (useAnchors && masterAnchor && slaveAnchor)
        {
            // master local
            Vector3 localP = masterAnchor.InverseTransformPoint(srcPos);
            Quaternion localR = Quaternion.Inverse(masterAnchor.rotation) * srcRot;

            // (����) �� ����
            if (mirrorX || mirrorY || mirrorZ)
            {
                localP = new Vector3(
                    mirrorX ? -localP.x : localP.x,
                    mirrorY ? -localP.y : localP.y,
                    mirrorZ ? -localP.z : localP.z
                );

                Vector3 e = localR.eulerAngles;
                e = new Vector3(
                    mirrorX ? -e.x : e.x,
                    mirrorY ? -e.y : e.y,
                    mirrorZ ? -e.z : e.z
                );
                localR = Quaternion.Euler(e);
            }

            // slave world
            srcPos = slaveAnchor.TransformPoint(localP);
            srcRot = slaveAnchor.rotation * localR;
        }

        // 3) �����̺� ee_Target�� ���� (�ε巴��)
        if (posLerp >= 1f) slaveEETarget.position = srcPos;
        else slaveEETarget.position = Vector3.Lerp(slaveEETarget.position, srcPos, posLerp);

        if (rotSlerp >= 1f) slaveEETarget.rotation = srcRot;
        else slaveEETarget.rotation = Quaternion.Slerp(slaveEETarget.rotation, srcRot, rotSlerp);

        // 4) �׸�(R/L) Ÿ�� ����
        if (slaveEETargetR && slaveEETargetL)
        {
            Vector3 rPos, lPos;

            if (masterGripR && masterGripL)
            {
                // �����Ϳ� ���Ͻ� ����Ʈ�� �̹� �ִ� ��� �� �״�� ����
                rPos = masterGripR.position;
                lPos = masterGripL.position;

                if (useAnchors && masterAnchor && slaveAnchor)
                {
                    Vector3 rLoc = masterAnchor.InverseTransformPoint(rPos);
                    Vector3 lLoc = masterAnchor.InverseTransformPoint(lPos);

                    if (mirrorX || mirrorY || mirrorZ)
                    {
                        rLoc = new Vector3(mirrorX ? -rLoc.x : rLoc.x, mirrorY ? -rLoc.y : rLoc.y, mirrorZ ? -rLoc.z : rLoc.z);
                        lLoc = new Vector3(mirrorX ? -lLoc.x : lLoc.x, mirrorY ? -lLoc.y : lLoc.y, mirrorZ ? -lLoc.z : lLoc.z);
                    }

                    rPos = slaveAnchor.TransformPoint(rLoc);
                    lPos = slaveAnchor.TransformPoint(lLoc);
                }
            }
            else
            {
                // ���Ͻð� ������: �ڵ� ���� right������ �� �ռ�
                Vector3 right = srcRot * Vector3.right;
                rPos = srcPos + right * gripHalfWidth;
                lPos = srcPos - right * gripHalfWidth;
            }

            if (posLerp >= 1f)
            {
                slaveEETargetR.position = rPos;
                slaveEETargetL.position = lPos;
            }
            else
            {
                slaveEETargetR.position = Vector3.Lerp(slaveEETargetR.position, rPos, posLerp);
                slaveEETargetL.position = Vector3.Lerp(slaveEETargetL.position, lPos, posLerp);
            }
        }
    }
}
