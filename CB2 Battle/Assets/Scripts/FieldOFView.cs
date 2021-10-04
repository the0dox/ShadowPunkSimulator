using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOFView : MonoBehaviour
{
    // Start is called before the first frame update
    private Mesh mesh;
    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        float fov = 90;
        Vector3 origin = Vector3.zero;
        int rayCount = 50;
        float angle = 0f;
        float angleIncrease = fov / rayCount;
        float viewDistance = 50f;

        Vector3[] vertices = new Vector3[rayCount + 1 + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];
       
        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit Hit;
            Physics.Raycast(origin, GetVectorFromAngle(angle), out Hit, viewDistance);
            if(Hit.collider != null) 
            {
                Hit.collider.gameObject.GetComponent<Tile>().selectable = true;
                Hit.collider.gameObject.GetComponent<Tile>().UpdateIndictator();
                vertex = Hit.point;
            }
            else
            {
                vertex = origin + GetVectorFromAngle(angle) * viewDistance;
            }
            
            vertices[vertexIndex] = vertex;
            
            if(i > 0){
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex -1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            vertexIndex++;
            angle -= angleIncrease;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }


    public static Vector3 GetVectorFromAngle(float angle)
    {
    float angleRad = angle * (Mathf.PI/180f);
    return new Vector3(Mathf.Cos(angleRad),Mathf.Sin(angleRad));
    }
}

