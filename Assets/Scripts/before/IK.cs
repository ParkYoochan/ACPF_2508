/*
�� �ڵ�� �κ��� ���� ���ϴ� ��ġ�� �����̱� ���� ����� �����մϴ�. 
CalcIK �Լ��� �� ��� ����� �����Ͽ� �����ϴ� �ֿ� �Լ��Դϴ�.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra.Single;


public class IK6_test : MonoBehaviour
{
    //��ǥ ��ġ
    public GameObject ee_Target;
    public GameObject ee_Target_R;
    public GameObject ee_Target_L;
    // �κ��� ���õ� ������
    private GameObject[] joint = new GameObject[5];
    private float[] angle = new float[5];
    private float[] prevAngle = new float[5];
    private Vector3[] dim = new Vector3[5];    
    private Vector3[] point = new Vector3[6];           // world position of joint end
    private Vector3[] axis = new Vector3[5];            // local direction of each axis

    private Quaternion[] rotation = new Quaternion[5];  // local rotation(quaternion) of joint relative to its parent
    private Quaternion[] wRotation = new Quaternion[6]; // world rotation(quaternion) of joint
    private Vector3 pos, pos_R, pos_L;                                // reference(target) position
    private Vector3 rot, rot_R, rot_L;                                // reference(target) pose
    private float lambda = 0.1f;
    private float[] minAngle = new float[5];            // limits of joint rotatation
    private float[] maxAngle = new float[5];
    private float forpris = new float();


    private GameObject[] twee = new GameObject[4];
    private float[] angle_twee = new float[4];
    private float[] prevAngle_twee = new float[4];
    private Vector3[] dim_twee = new Vector3[4];// local dimensions of each Joint
    private Vector3[] point_twee = new Vector3[4];
    private Vector3 axis_twee = new Vector3(0f, 1f, 0f);
    private Quaternion[] rotation_twee = new Quaternion[4];
    private Quaternion[] wRotation_twee = new Quaternion[2];


    /*
�����̳� �ùķ��̼��� ������ �� �κ� ���� �ʱ� ������ �մϴ�. 
�κ� ���� �� ������ ����ڰ� ������ �� �ִ� �������� ã�Ƽ� ����Ʈ�� �߰��ϰ�, 
�� ������ ũ��, ȸ�� ��, �ʱ� ȸ�� ����, ȸ���� �� �ִ� �ִ�/�ּ� ������ �����մϴ�.
*/
    void Start()
        {
            // �κ��� ������ ã�Ƽ� ����Ʈ�� �߰��մϴ�.
        for (int i = 0; i < joint.Length; i++)
            {
                joint[i] = GameObject.Find("Axis_" + i.ToString());
            }
        twee[0] = GameObject.Find("Axis_5_R");
        twee[1] = GameObject.Find("Axis_5_L");
        twee[2] = GameObject.Find("Axis_6_R");
        twee[3] = GameObject.Find("Axis_6_L");

            // �� ������ ũ��� ȸ�� ���� �����մϴ�.
        dim[0] = new Vector3(-0.566f, 0f, 0f);
        dim[1] = new Vector3(0f, -1.263f, 0f);
        dim[2] = new Vector3(0f, -0.1066998f, 0f);
        dim[3] = new Vector3(-0.03949997f, 0f, 0f);
        dim[4] = new Vector3(-0.04258f, 0f, 0f);

        dim_twee[0] = new Vector3(-0.004520106f, 0f, 0.02950001f);  //R
        dim_twee[1] = new Vector3(-0.004520106f, 0f, -0.02950001f);  //L
        dim_twee[2] = new Vector3(-0.0732f, 0f, 0.0479f);
        dim_twee[3] = new Vector3(-0.07319998f, 0f, -0.0479f);
        
            // IK5�� ���� ȸ������: [x z y z]
        axis[0] = new Vector3(1f, 0f, 0f);
        axis[1] = new Vector3(0f, 0f, 1f);
        axis[2] = new Vector3(0f, 1f, 0f); // -y�������� �����̵���
        axis[3] = new Vector3(0f, 0f, 1f);
        axis[4] = new Vector3(1f, 0f, 0f);



        // �� ������ �ʱ� ȸ�� ������ �����մϴ�. �ʱ� �����ڼ�
        angle[0] = 0f;
        angle[1] = 0f;
        angle[2] = 0f;
        angle[3] = 0f;
        angle[4] = 0f;

        angle_twee[0] = 0f;
        angle_twee[1] = 0f;
        angle_twee[2] = 0f;
        angle_twee[3] = 0f;

        // �� ������ ȸ���� �� �ִ� �ִ�, �ּ� ������ �����մϴ�.
        minAngle[0] = -30f;
        maxAngle[0] = 30f;
        minAngle[1] = -20f;
        maxAngle[1] = 90f;
        minAngle[2] = -176;
        maxAngle[2] = 176;
        minAngle[3] = -110f;
        maxAngle[3] = 30f;
        minAngle[4] = -180f;
        maxAngle[4] = 180f;

        forpris = 0;


        }

        /*
        �����̳� �ùķ��̼� �߿� ����ڰ� ������ �����̴��� ���� �о�ͼ� 
        �κ� ���� ��ġ�� ȸ���� �����մϴ�. �׸��� IK ����� ����ؼ� �κ� ���� �����Դϴ�.
        */
        void Update()
        {
            
            // ��ǥ ��ġ�� EE_Target���� �о�ɴϴ�.
        pos.x = ee_Target.transform.position.x;
        pos.y = ee_Target.transform.position.y;
        pos.z = ee_Target.transform.position.z;
        rot.x = ee_Target.transform.eulerAngles.x;
        rot.y = ee_Target.transform.eulerAngles.y;
        rot.z = ee_Target.transform.eulerAngles.z;

        pos_R.x = ee_Target_R.transform.position.x;
        pos_R.y = ee_Target_R.transform.position.y;
        pos_R.z = ee_Target_R.transform.position.z;
        rot_R.x = ee_Target_R.transform.eulerAngles.x;
        rot_R.y = ee_Target_R.transform.eulerAngles.y;
        rot_R.z = ee_Target_R.transform.eulerAngles.z;

        pos_L.x = ee_Target_L.transform.position.x;
        pos_L.y = ee_Target_L.transform.position.y;
        pos_L.z = ee_Target_L.transform.position.z;
        rot_L.x = ee_Target_L.transform.eulerAngles.x;
        rot_L.y = ee_Target_L.transform.eulerAngles.y;
        rot_L.z = ee_Target_L.transform.eulerAngles.z;

        // IK ����� ����ؼ� �κ� ���� �����Դϴ�.
        CalcIK();
        }

        /*
        �κ��� ���� ���ϴ� ��ġ�� �������� ��� �������� ����մϴ�.
        100�� ���� �ݺ��Ͽ� ���� ������ �����ϸ� �ּ��� ��ġ�� ã�Ƴ����ϴ�.
        ���� ������ ��� ������ ����ų� 100�� �ȿ� ���ϴ� ��ġ�� ã�� ���ϸ�, �ʱ� �������� �ǵ����ϴ�.
        */
        void CalcIK()
        {
            int count = 0;
            bool outOfLimit = false;
            // 100�� ���� ��� ���� �������� ���� ��ġ�� ã���ϴ�.
            for (int i = 0; i < 100; i++)   // iteration
            {
                count = i;
                // ���� ��ġ�� ã�ƺ��ϴ�.
                ForwardKinematics();

                // ���� �󸶳� �߸��� ��ġ�� �ִ��� Ȯ���մϴ�.
                var err = CalcErr();    // 6x1 matrix(vector)
            var err_twee_R = CalcErr_twee_R();
            var err_twee_L = CalcErr_twee_L();

            Debug.Log(err_twee_R);

            float err_norm = (float)err.L2Norm();
            float err_norm_R = (float)err_twee_R.L2Norm();
            float err_norm_L = (float)err_twee_L.L2Norm();
            // ���� ���� ���� ��Ȯ�� ��ġ�� �ִٸ�, ���� ���� �������� �ʾƵ� �˴ϴ�.
            if (err_norm < 1E-3 && err_norm_R < 1E-2 && err_norm_L < 1E-2)
                {
                    for (int ii = 0; ii < joint.Length; ii++)
                    {
                        // ������ ���� ������ �ʹ� ũ�ٸ� ������ ����ϴ�!
                        if (angle[ii] < minAngle[ii] || angle[ii] > maxAngle[ii])
                        {
                            outOfLimit = true;
                            break;
                        }
                    }
                    break;
                }

                // ���� ������ �󸶳� �����ؾ� �ϴ��� ����մϴ�.
            var J = CalcJacobian(); // 6x6 matrix
            var J_R = CalcJacobian_R();
            var J_L = CalcJacobian_L();
            Debug.Log("Jacobian J: " + J_R);
            Debug.Log("PseudoInverse J: " + J_R.PseudoInverse());


            // ���� ���� ����
            var dAngle = lambda * J.PseudoInverse() * err; // 6x1 matrix
            angle[0] += dAngle[0, 0] * Mathf.Rad2Deg;
            angle[1] += dAngle[1, 0] * Mathf.Rad2Deg;

            forpris = dAngle[2, 0];
            dim[1] += forpris * axis[2];

            angle[2] += dAngle[3, 0] * Mathf.Rad2Deg;
            angle[3] += dAngle[4, 0] * Mathf.Rad2Deg;
            angle[4] += dAngle[5, 0] * Mathf.Rad2Deg;

            var dAngle_R = lambda * J_R.PseudoInverse() * err_twee_R;
            angle_twee[0] += dAngle_R[0, 0];
            var dAngle_L = lambda * J_L.PseudoInverse() * err_twee_L;
            angle_twee[1] += dAngle_L[0, 0];

            angle_twee[2] = -angle_twee[0];
            angle_twee[3] = -angle_twee[1];

            /*for (int ii = 0; ii < 4; ii++)
            {
                angle[ii] += dAngle[ii, 0] * Mathf.Rad2Deg;
            }  */

        }

            // ���� 100�� �ȿ� ���� ��ġ�� ����� �� ã�Ұų�, ���� ������ �ʹ� ũ�ٸ�,
            // ó������ �ٽ� �õ��մϴ�.
            if (count == 99 || outOfLimit == true)  // did not converge or angle out of limit
            {
                for (int i = 0; i < joint.Length; i++) // reset slider
                {
                    Debug.Log("NG");
                    
                    angle[i] = prevAngle[i];
                }
            }
            // �ƴϸ� ���� ����� ���� ������ �κ��� �����Դϴ�. 
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                    joint[i].transform.localRotation = rotation[i];              
                    prevAngle[i] = angle[i];
                }
            
            Debug.Log(dim[1]);
            joint[2].transform.position=(joint[1].transform.position + rotation[0] * rotation[1] *  dim[1]);
            joint[2].transform.localRotation = rotation[2];

            for (int i = 3; i < 5; i++)
            {
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                joint[i].transform.localRotation = rotation[i];
                prevAngle[i] = angle[i];
            }
            //�ո�

            for (int i = 0; i <4 ; i++)
            {
                rotation_twee[i] = Quaternion.AngleAxis(angle_twee[i], axis_twee);
                twee[i].transform.localRotation = rotation_twee[i];
                prevAngle_twee[i] = angle_twee[i];
            }


            Debug.Log(joint[2].transform.position + "*");
            /*Debug.Log(dim[2] + "*");
            Debug.Log(joint[1].transform.position);
                Debug.Log(joint[2].transform.position);
            Debug.Log(rotation[0]);
            Debug.Log(rotation[1]);
            Debug.Log(rotation[2]);*/

            }
        }

        /*
        ������ ���� ������ ������� �κ��� ���� ��ġ�� ����մϴ�.
        ���� �� �κ��� �����ϸ� ���� ���� ��ġ�� ã�Ƴ����ϴ�.
        */
        void ForwardKinematics()
        {
            point[0] = joint[0].transform.position;
            wRotation[0] = Quaternion.AngleAxis(angle[0], axis[0]);

            for (int i = 1; i < 5; i++) //angle[2]���� 0���� �����Ǿ�����, dim[1]���� ������Ʈ��
            {
                point[i] = wRotation[i - 1] * dim[i - 1] + point[i - 1];
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                wRotation[i] = wRotation[i - 1] * rotation[i];
            }

        point[5] = wRotation[4] * dim[4] + point[4];
        wRotation[5] = wRotation[4];
        
        //�ճ��κ�
        for (int  i = 0;  i < 2;  i++)
        {
            point_twee[i] = wRotation[5]*dim_twee[i] + point[5];
            rotation_twee[i] = Quaternion.AngleAxis(angle_twee[i], axis_twee);
            wRotation_twee[i] = wRotation[5] * rotation_twee[i];
        }
        point_twee[2] = wRotation_twee[0] * dim_twee[2] + point_twee[0];
        point_twee[3] = wRotation_twee[1] * dim_twee[3] + point_twee[1];

        /*for (int i = 1; i < joint.Length; i++)
        {
            point[i] = wRotation[i - 1] * dim[i - 1] + point[i - 1];
            rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
            wRotation[i] = wRotation[i - 1] * rotation[i];
        }
        point[joint.Length] = wRotation[joint.Length - 1] * dim[joint.Length - 1] + point[joint.Length - 1];*/
    }

        /*
        �κ��� ���� ���ϴ� ��ġ�� ���� ��ġ ������ ������ ����մϴ�.
        ��ġ ������ ���� ������ ��� �����Ͽ� ������ ��ȯ�մϴ�.
        */
        DenseMatrix CalcErr()
        {
            // position error
            Vector3 perr = pos - point[5];
            // pose error
            Quaternion rerr = Quaternion.Euler(rot) * Quaternion.Inverse(wRotation[4]);
            // make error vector
            Vector3 rerrVal = new Vector3(rerr.eulerAngles.x, rerr.eulerAngles.y, rerr.eulerAngles.z);
            if (rerrVal.x > 180f) rerrVal.x -= 360f;
            if (rerrVal.y > 180f) rerrVal.y -= 360f;
            if (rerrVal.z > 180f) rerrVal.z -= 360f;
            var err = DenseMatrix.OfArray(new float[,]
            {
                { perr.x },
                { perr.y },
                { perr.z },
                { rerrVal.x * Mathf.Deg2Rad},
                { rerrVal.y * Mathf.Deg2Rad},
                { rerrVal.z * Mathf.Deg2Rad}
            });
            return err;
        }
    DenseMatrix CalcErr_twee_R()
    {
        // position error
        Vector3 perr = pos_R - point[2];
        // pose error
        Quaternion rerr = Quaternion.Euler(rot_R) * Quaternion.Inverse(wRotation_twee[0]);
        // make error vector
        Vector3 rerrVal = new Vector3(rerr.eulerAngles.x, rerr.eulerAngles.y, rerr.eulerAngles.z);
        if (rerrVal.x > 180f) rerrVal.x -= 360f;
        if (rerrVal.y > 180f) rerrVal.y -= 360f;
        if (rerrVal.z > 180f) rerrVal.z -= 360f;
        var err = DenseMatrix.OfArray(new float[,]
        {
                { perr.x },
                { perr.y },
                { perr.z },
                { rerrVal.x * Mathf.Deg2Rad},
                { rerrVal.y * Mathf.Deg2Rad},
                { rerrVal.z * Mathf.Deg2Rad}
        });
        return err;
    }

    DenseMatrix CalcErr_twee_L()
    {
        // position error
        Vector3 perr = pos_L - point[3];
        // pose error
        Quaternion rerr = Quaternion.Euler(rot_L) * Quaternion.Inverse(wRotation_twee[1]);
        // make error vector
        Vector3 rerrVal = new Vector3(rerr.eulerAngles.x, rerr.eulerAngles.y, rerr.eulerAngles.z);
        if (rerrVal.x > 180f) rerrVal.x -= 360f;
        if (rerrVal.y > 180f) rerrVal.y -= 360f;
        if (rerrVal.z > 180f) rerrVal.z -= 360f;
        var err = DenseMatrix.OfArray(new float[,]
        {
                { perr.x },
                { perr.y },
                { perr.z },
                { rerrVal.x * Mathf.Deg2Rad},
                { rerrVal.y * Mathf.Deg2Rad},
                { rerrVal.z * Mathf.Deg2Rad}
        });
        return err;
    }

    /*
    ���� �����Ӱ� ���� ��ġ ������ ���踦 ��Ÿ���� ����� ����մϴ�.
    �� ����� ����Ͽ� ���� ��ġ ������ �ּ�ȭ�ϴ� ���� �������� ����մϴ�.
    */
    DenseMatrix CalcJacobian()
        {
            // ���⼭�� �� ���� �κп� ���� ����� �մϴ�.
        Vector3 w0 = wRotation[0] * axis[0];
        Vector3 w1 = wRotation[1] * axis[1];
        Vector3 w2 = new Vector3(0f,0f,0f);
        Vector3 w3 = wRotation[2] * axis[2];
        Vector3 w4 = wRotation[3] * axis[3];
        Vector3 w5 = wRotation[4] * axis[4];


        Vector3 p0 = Vector3.Cross(w0, point[5] - point[0]);
        Vector3 p1 = Vector3.Cross(w1, point[5]- point[1]);
        Vector3 p2 = wRotation[2]*axis[2];
        Vector3 p3 = Vector3.Cross(w3, point[5] - point[2]);
        Vector3 p4 = Vector3.Cross(w4, point[5] - point[3]);
        Vector3 p5 = Vector3.Cross(w5, point[5] - point[4]);



            var J = DenseMatrix.OfArray(new float[,]
            {
                
                /* �� ���� �κп� ���� ��� ����� ��ķ� ����ϴ�.*/
                { p0.x, p1.x, p2.x, p3.x, p4.x, p5.x },
                { p0.y, p1.y, p2.y, p3.y, p4.y, p5.y },
                { p0.z, p1.z, p2.z, p3.z, p4.z, p5.z },
                { w0.x, w1.x, w2.x, w3.x, w4.x, w5.x },
                { w0.y, w1.y, w2.y, w3.y, w4.y, w5.y },
                { w0.z, w1.z, w2.z, w3.z, w4.z, w5.z }
            });
            return J;
            
        }

    DenseMatrix CalcJacobian_R()
    {
        // ���⼭�� �� ���� �κп� ���� ����� �մϴ�.
        Vector3 w0 = wRotation_twee[0] * axis_twee;
        Vector3 p0 = Vector3.Cross(w0, point_twee[2] - point[0]);
        var J = DenseMatrix.OfArray(new float[,]
        {
                
                /* �� ���� �κп� ���� ��� ����� ��ķ� ����ϴ�.*/
                { p0.x },
                { p0.y },
                { p0.z },
                { w0.x },
                { w0.y },
                { w0.z }
        });
        return J;

    }

    DenseMatrix CalcJacobian_L()
    {
        // ���⼭�� �� ���� �κп� ���� ����� �մϴ�.
        Vector3 w0 = wRotation_twee[1] * axis_twee;
        Vector3 p0 = Vector3.Cross(w0, point_twee[3] - point[1]);
        var J = DenseMatrix.OfArray(new float[,]
        {
                
                /* �� ���� �κп� ���� ��� ����� ��ķ� ����ϴ�.*/
                { p0.x },
                { p0.y },
                { p0.z },
                { w0.x },
                { w0.y },
                { w0.z }
        });
        return J;

    }
}