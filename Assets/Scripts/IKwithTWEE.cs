/*
�� �ڵ�� �κ��� ���� ���ϴ� ��ġ�� �����̱� ���� ����� �����մϴ�. 
CalcIK �Լ��� �� ��� ����� �����Ͽ� �����ϴ� �ֿ� �Լ��Դϴ�.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra.Single;


public class IKwithTWEE : MonoBehaviour
{
    //��ǥ ��ġ
    public GameObject ee_Target;
    // �κ��� ���õ� ������
    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[5];
    private float[] prevAngle = new float[5];
    private Vector3[] dim = new Vector3[5];             // local dimensions of each Joint
    private Vector3[] point = new Vector3[6];           // world position of joint end
    private Vector3[] axis = new Vector3[5];            // local direction of each axis
    private Quaternion[] rotation = new Quaternion[5];  // local rotation(quaternion) of joint relative to its parent
    private Quaternion[] wRotation = new Quaternion[5]; // world rotation(quaternion) of joint
    private Vector3 pos;                                // reference(target) position
    private Vector3 rot;                                // reference(target) pose
    private float lambda = 0.1f;
    private float[] minAngle = new float[5];            // limits of joint rotatation
    private float[] maxAngle = new float[5];
    private float forpris = new float();


    public GameObject ee_Target_R;
    public GameObject ee_Target_L;
    private GameObject[] twee = new GameObject[5];
    private float[] angle_twee = new float[5];
    private float[] prevAngle_twee = new float[5];
    private Vector3[] dim_twee = new Vector3[4];// local dimensions of each Joint
    private Vector3[] point_twee = new Vector3[5];
    private Vector3 axis_twee = new Vector3(0f, 1f, 0f);
    private Quaternion[] rotation_twee = new Quaternion[5];
    private Quaternion[] wRotation_twee = new Quaternion[3];
    private Vector3 pos_R, pos_L;                                // reference(target) position
    private Vector3 rot_R, rot_L;
    private float[] length = new float[2];
    private float length_goal, length_target, length_now = new float();
    private float angle_goal = new float();
    private float angle_now = new float();

    Renderer sr_R;
    Renderer sr_L;
    public GameObject Link_6_R;
    public GameObject Link_6_L;


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

        // �� ������ ũ��� ȸ�� ���� �����մϴ�.
        dim[0] = new Vector3(-0.566f, 0f, 0f);
        dim[1] = new Vector3(0f, -1.263f, 0f);
        dim[2] = new Vector3(0f, -0.1066998f, 0f);
        dim[3] = new Vector3(-0.03949997f, 0f, 0f);
        dim[4] = new Vector3(-0.04258f, 0f, 0f);



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

        // �� ������ ȸ���� �� �ִ� �ִ�, �ּ� ������ �����մϴ�.
        minAngle[0] = -30f;
        maxAngle[0] = 30f;
        minAngle[1] = -20f;
        maxAngle[1] = 90f;
        minAngle[2] = -176;
        maxAngle[2] = 176;
        minAngle[3] = -110f;
        maxAngle[3] = 180f;
        minAngle[4] = -180f;
        maxAngle[4] = 180f;

        forpris = 0;

        twee[0] = GameObject.Find("Axis_5");
        twee[1] = GameObject.Find("Axis_5_R");
        twee[2] = GameObject.Find("Axis_5_L");
        twee[3] = GameObject.Find("Axis_6_R");
        twee[4] = GameObject.Find("Axis_6_L");

        for (int i = 0; i < twee.Length; i++)
        {
            if (twee[i] == null)
            {
                Debug.LogError($"twee[{i}]�� null�Դϴ�. GameObject �̸��� Ȯ���ϼ���.");
            }
            else
            {
                Debug.Log($"twee[{i}]�� ���������� �Ҵ�Ǿ����ϴ�: {twee[i].name}");
            }
        }

        dim_twee[0] = new Vector3(-0.004520106f, 0f, 0.02950001f);  //R
        dim_twee[1] = new Vector3(-0.00297f, 0f, -0.0332f);  //L
        dim_twee[2] = new Vector3(-0.0732f, 0f, 0.0479f);
        dim_twee[3] = new Vector3(-0.0696f, 0f, -0.0533f);

        angle_twee[0] = 0f;
        angle_twee[1] = 0f;
        angle_twee[2] = 0f;
        angle_twee[3] = 0f;
        angle_twee[4] = 0f;

        length[0] = Vector3.Distance(twee[1].transform.position, twee[2].transform.position);
        length[1] = Vector3.Distance(twee[1].transform.position, twee[3].transform.position);
        //Debug.Log(twee[1].transform.position);
        //Debug.Log(twee[2].transform.position);


        Link_6_R = GameObject.Find("Link_6_R");
        Link_6_L = GameObject.Find("Link_6_L");
        sr_R = Link_6_R.GetComponent<Renderer>();
        sr_L = Link_6_L.GetComponent<Renderer>();




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

        pos_R = ee_Target_R.transform.position;
        pos_L = ee_Target_L.transform.position;


        // IK ����� ����ؼ� �κ� ���� �����Դϴ�.
        CalcIK();
        //Debug.Log(wRotation[4]);
        CalcIK_twee();
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
            float err_norm = (float)err.L2Norm();
            // ���� ���� ���� ��Ȯ�� ��ġ�� �ִٸ�, ���� ���� �������� �ʾƵ� �˴ϴ�.
            if (err_norm < 1E-3)
            {
                for (int ii = 0; ii < joint.Length - 1; ii++)
                {
                    // ������ ���� ������ �ʹ� ũ�ٸ� ������ ����ϴ�!
                    if (angle[ii] < minAngle[ii] || angle[ii] > maxAngle[ii])
                    {
                        outOfLimit = true;
                        sr_R.material.color = Color.red;
                        sr_L.material.color = Color.red;
                        break;
                    }
                    else 
                    {   sr_R.material.color = new Color(249/255f,160/255f, 237/255f, 255/255f);
                        sr_L.material.color = new Color(186 / 255f, 252 / 255f, 192 / 255f, 255 / 255f);
                    }
                }
                break;
            }

            // ���� ������ �󸶳� �����ؾ� �ϴ��� ����մϴ�.
            var J = CalcJacobian(); // 6x6 matrix



            // ���� ���� ����
            var dAngle = lambda * J.PseudoInverse() * err; // 6x1 matrix
            angle[0] += dAngle[0, 0] * Mathf.Rad2Deg;
            angle[1] += dAngle[1, 0] * Mathf.Rad2Deg;

            forpris = dAngle[2, 0];
            dim[1] += forpris * axis[2];

            angle[2] += dAngle[3, 0] * Mathf.Rad2Deg;
            angle[3] += dAngle[4, 0] * Mathf.Rad2Deg;
            angle[4] += dAngle[5, 0] * Mathf.Rad2Deg;


            /*for (int ii = 0; ii < 4; ii++)
            {
                angle[ii] += dAngle[ii, 0] * Mathf.Rad2Deg;
            }  */

        }

        // ���� 100�� �ȿ� ���� ��ġ�� ����� �� ã�Ұų�, ���� ������ �ʹ� ũ�ٸ�,
        // ó������ �ٽ� �õ��մϴ�.
        if (count == 99 || outOfLimit == true)  // did not converge or angle out of limit
        {
            for (int i = 0; i < joint.Length-1; i++) // reset slider
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

            joint[2].transform.position = (joint[1].transform.position + rotation[0] * rotation[1] * dim[1]);
            joint[2].transform.localRotation = rotation[2];

            for (int i = 3; i < 5; i++)
            {
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                joint[i].transform.localRotation = rotation[i];
                prevAngle[i] = angle[i];
            }


            /*Debug.Log(dim[2] + "*");
            Debug.Log(joint[1].transform.position);
                Debug.Log(joint[2].transform.position);
            Debug.Log(rotation[0]);
            Debug.Log(rotation[1]);
            Debug.Log(rotation[2]);*/

        }
    }

    void CalcIK_twee()
    {

        length_now = Vector3.Distance(twee[3].transform.position, twee[4].transform.position);
        length_now = Mathf.Clamp(length_now, 0.01f, 0.13f);
        length_target = Mathf.Clamp(Vector3.Distance(pos_R, pos_L), 0.01f, 0.13f);
        length_goal = Mathf.Clamp(length_target * (1), 0.01f, 0.13f);

        angle_now = Mathf.Acos((length_now - length[0]) / (2 * length[1])) * Mathf.Rad2Deg;
        angle_now = Mathf.Clamp(angle_now, 0f, 120f);
        angle_goal = Mathf.Acos((length_goal - length[0]) / (2 * length[1])) * Mathf.Rad2Deg;
        angle_goal = Mathf.Clamp(angle_goal, 0f, 120f);

        //Debug.Log(angle_now + " " + angle_goal);
        
        angle_twee[1] += -(angle_goal - angle_now);
        //angle_twee[1] = Mathf.Clamp(angle_twee[1], -30f, 30f);
        angle_twee[2] = -angle_twee[1];
        angle_twee[3] = -angle_twee[1];
        angle_twee[4] = -angle_twee[2];
        //Debug.Log(angle_twee[1]);

        // �ƴϸ� ���� ����� ���� ������ �κ��� �����Դϴ�. 

        for (int i = 1; i < 5; i++)
        {
            rotation_twee[i] = Quaternion.AngleAxis(angle_twee[i], axis_twee);
            twee[i].transform.localRotation = rotation_twee[i];
            prevAngle_twee[i] = angle_twee[i];
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


    /*
    ���� �����Ӱ� ���� ��ġ ������ ���踦 ��Ÿ���� ����� ����մϴ�.
    �� ����� ����Ͽ� ���� ��ġ ������ �ּ�ȭ�ϴ� ���� �������� ����մϴ�.
    */
    DenseMatrix CalcJacobian()
    {
        // ���⼭�� �� ���� �κп� ���� ����� �մϴ�.
        Vector3 w0 = wRotation[0] * axis[0];
        Vector3 w1 = wRotation[1] * axis[1];
        Vector3 w2 = new Vector3(0f, 0f, 0f);
        Vector3 w3 = wRotation[2] * axis[2];
        Vector3 w4 = wRotation[3] * axis[3];
        Vector3 w5 = wRotation[4] * axis[4];


        Vector3 p0 = Vector3.Cross(w0, point[5] - point[0]);
        Vector3 p1 = Vector3.Cross(w1, point[5] - point[1]);
        Vector3 p2 = wRotation[2] * axis[2];
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

}
