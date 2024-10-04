using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AvoiderDLL;

public class Avoider : MonoBehaviour
{
    private void Update()
    {
        TestUpdate();
    }

    private void TestUpdate()
    {
        Debug.Log("The DLL Works!");
    }
}