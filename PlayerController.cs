using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
      float pos, lastPos;
    void FixedUpdate()
    {
        pos = Input.GetAxis("Horizontal");
        if (pos != lastPos && (pos==-1 || pos == 1 || pos == 0))
        {
            MultiplayerController.Instance.Send(pos.ToString());
            lastPos = pos;
        }
    }
}
