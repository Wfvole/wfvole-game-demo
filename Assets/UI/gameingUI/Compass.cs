using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compass : MonoBehaviour
{
    private Vector3 playerZ;
    public Transform compassDial;
    private float tSAngle;
    private void OnEnable()
    {
        Eventmanager.Instance.AddListener("playerZ", GetplayerZ);
    }
    private void OnDisable()
    {
        //Eventmanager.Instance.RemoveListener("playerZ", GetplayerZ);
    }

    // Update is called once per frame
    void Update()
    {
        AngleOfxz(playerZ);
        compassDial.localEulerAngles= new Vector3(0,0,tSAngle);
        
    }
    void GetplayerZ(string eventName, object udata)
    {
        if (udata is Vector3 Zdir)
        {
            playerZ=Zdir;
        }
    }
    void AngleOfxz(Vector3 Z)
    {
        tSAngle = -Vector3.SignedAngle(Z, Vector3.forward, Vector3.up);
    }

}
