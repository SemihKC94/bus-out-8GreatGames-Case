using UnityEngine;

namespace SKC.GameLogic
{
    public class CamSetter : MonoBehaviour
    {
        public Camera cam;
        
        public void Initialize(float y)
        {
            cam.transform.position = new Vector3(0.0f, y, 0.0f);
        }
    }
}
