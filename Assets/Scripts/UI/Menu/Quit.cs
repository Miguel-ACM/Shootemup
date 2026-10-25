using UnityEngine;

public class Quit : AMenuElement
{
    public override void OnAccept()
    {
        CloudTransition.Load("Title");
    }
}

