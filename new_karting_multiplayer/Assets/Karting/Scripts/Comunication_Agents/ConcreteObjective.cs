using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class used to represent a concrete implementation of the Objective class (in a GameObject Component)
public class ConcreteObjective : Objective
{
    public override void ReachCheckpoint(int remaining)
    {
        // Implementazione concreta del metodo ReachCheckpoint
        Debug.Log("Checkpoint raggiunto. Checkpoint rimanenti: " + remaining);
    }

}
