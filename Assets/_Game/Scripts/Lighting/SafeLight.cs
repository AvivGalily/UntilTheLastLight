using UnityEngine;

namespace Game
{
    public sealed class SafeLight : MonoBehaviour
    {
        public string persistentId;
        public Vector3 boxSize = new Vector3(8, 4, 8);
        public Light lamp;
        public bool powered = true;
        public Renderer[] fixtures;
        public bool Contains(Vector3 p) => powered && new Bounds(Vector3.zero, boxSize).Contains(transform.InverseTransformPoint(p));
        public void SetPowered(bool value)
        {
            powered = value;
            if (lamp) { lamp.enabled = value; var glass=lamp.GetComponent<Renderer>();if(glass)glass.enabled=value; }
            if(fixtures==null)return;
            var properties=new MaterialPropertyBlock();
            foreach(var fixture in fixtures)
            {
                if(!fixture)continue;
                var materials=fixture.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    if(!materials[i]||materials[i].name!="Shelter glass")continue;
                    properties.Clear();properties.SetColor("_EmissionColor",value?new Color(1,.43f,.09f)*2:Color.black);
                    properties.SetColor("_BaseColor",value?new Color(.85f,.48f,.1f):new Color(.08f,.09f,.09f));
                    fixture.SetPropertyBlock(properties,i);
                }
            }
        }
        void OnDrawGizmosSelected()
        { Gizmos.color = Color.yellow; Gizmos.matrix = transform.localToWorldMatrix; Gizmos.DrawWireCube(Vector3.zero, boxSize); }
    }
}
