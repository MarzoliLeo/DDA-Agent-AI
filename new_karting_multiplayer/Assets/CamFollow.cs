using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CamFollow : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = transform;
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>().LookAt = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
