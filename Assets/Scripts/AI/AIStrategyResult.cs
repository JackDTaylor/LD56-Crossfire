using UnityEngine;

public class AIStrategyResult
{
    public enum Action { EAT, FEAR, EXPLORE, ATTACK }

    public AntController targetAnt = null;

    public Action action;
    public Vector2 target;
}