using UnityEngine;
namespace Game
{
    // The wrist follows the equipment; the forearm continues below the camera
    // into the body, including during a swing or recoil. Keep the skin UVs.
    public sealed class FirstPersonArm:MonoBehaviour
    {
        public Camera eyes;
        public Transform bodyFrame;
        public Vector3 bodyAnchor=new Vector3(.26f,-.50f,.025f);
        Mesh mesh;
        MeshFilter surface;
        Vector3[] rest,posed,restNormals,posedNormals;
        Vector4[] restTangents,posedTangents;
        float[] weights,gradient;
        Vector3 end,lastDelta,axis;
        bool ready,applied;
        public Vector3 ForearmEnd=>bodyFrame?bodyFrame.TransformPoint(bodyAnchor):eyes?eyes.transform.TransformPoint(bodyAnchor):Vector3.zero;
        public void Pose()
        {
            if(!eyes)return;
            if(!ready)
            {
                surface=GetComponentInChildren<MeshFilter>();if(!surface)return;
                mesh=Instantiate(surface.sharedMesh);mesh.name="Body-connected first person forearm";mesh.MarkDynamic();surface.sharedMesh=mesh;
                rest=mesh.vertices;posed=new Vector3[rest.Length];weights=new float[rest.Length];
                restNormals=mesh.normals;posedNormals=new Vector3[rest.Length];restTangents=mesh.tangents;posedTangents=new Vector4[rest.Length];gradient=new float[rest.Length];
                axis=surface.transform.InverseTransformVector(transform.TransformVector(Vector3.forward));float units=axis.magnitude;axis.Normalize();
                float far=0;var local=new Vector3[rest.Length];
                for(int i=0;i<rest.Length;i++){local[i]=transform.InverseTransformPoint(surface.transform.TransformPoint(rest[i]));far=Mathf.Max(far,local[i].z);}
                int count=0;end=Vector3.zero;
                for(int i=0;i<rest.Length;i++)
                {
                    weights[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.095f,far-.025f,local[i].z));
                    float t=Mathf.InverseLerp(.095f,far-.025f,local[i].z);
                    gradient[i]=6*t*(1-t)/Mathf.Max(.001f,(far-.025f-.095f)*units);
                    if(local[i].z>far-.025f){end+=rest[i];count++;}
                }
                if(count>0)end/=count;ready=true;
            }
            Vector3 delta=surface.transform.InverseTransformVector(ForearmEnd-surface.transform.TransformPoint(end));
            if(applied&&(lastDelta-delta).sqrMagnitude<.000001f)return;
            for(int i=0;i<rest.Length;i++)
            {
                posed[i]=rest[i]+delta*weights[i];
                var rotation=Quaternion.FromToRotation(axis,axis+delta*gradient[i]);
                posedNormals[i]=rotation*restNormals[i];
                if(restTangents.Length==rest.Length)
                {Vector3 tangent=rotation*(Vector3)restTangents[i];posedTangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,restTangents[i].w);}
            }
            // Imported smooth normals span the anatomical UV seams. Recalculating
            // them on the split FBX vertices makes fingers look like separate facets.
            mesh.vertices=posed;mesh.normals=posedNormals;if(restTangents.Length==rest.Length)mesh.tangents=posedTangents;
            mesh.RecalculateBounds();lastDelta=delta;applied=true;
        }
        void LateUpdate(){Pose();}
        void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
