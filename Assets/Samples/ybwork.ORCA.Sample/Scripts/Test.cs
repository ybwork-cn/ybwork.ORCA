using Nebukam.ORCA;
using UnityEngine;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    private ORCA<AgentBase> _orca;
    private Unity.Mathematics.Random _random;
    private readonly List<AgentBase> _agents = new();
    private Transform _parent;

    [SerializeField] GameObject _prefabe;
    [SerializeField] float _radius;
    [SerializeField] int _count;

    private void Start()
    {
        _parent = new GameObject("Units").transform;
        _orca = new();
        _random = new Unity.Mathematics.Random(12345);
    }

    private void FixedUpdate()
    {
        _orca.Complete();

        for (int i = 0; i < _count; i++)
            Create();

        foreach (var item in _agents)
        {
            float x = item.pos.x;
            float y = item.pos.y;
            item.Transform.position = new Vector3(x, 0, y);
        }

        _orca.Schedule(Time.fixedDeltaTime);
    }

    private void Create()
    {
        var agent = _orca.agents.Add(_random.NextFix64Vec2() * _radius);
        GameObject go = Instantiate(_prefabe, _parent);
        agent.Transform = go.transform;
        _agents.Add(agent);
    }

    private void OnGUI()
    {
        GUI.TextArea(new Rect(100, 100, 200, 100), _agents.Count.ToString());
    }
}

public class AgentBase : Agent
{
    public Transform Transform { get; set; }
}
