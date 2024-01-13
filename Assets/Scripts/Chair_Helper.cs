using cakeslice;
using Lean.Touch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair_Helper : MonoBehaviour
{
    [SerializeField] LeanTwistRotateAxis leanTwistRotateAxis;
    [SerializeField] LeanDragTranslate leanDragTranslate;
    [SerializeField] Outline outline;

    //Function for Enabling and Disabling Lean Touch interaction and outline component
    public void EnableDisableInteraction(bool enable)
    {
        leanTwistRotateAxis.enabled = enable;
        leanDragTranslate.enabled = enable;
        outline.enabled = enable;
    }

}
