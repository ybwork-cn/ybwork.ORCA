using Nebukam.ORCA;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Nebukam.Common;

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
        _orca.staticObstacles.Add(new List<float2> {
            new(20,40),
            new(20,20),
            new(40,20),
            new(40,40),
        }, isReverse: false, maxSegmentLength: 6);
        _random = new Unity.Mathematics.Random(12345);
    }

    int center;
    private void Update()
    {
        center = Input.GetKey(KeyCode.Space) ? 0 : 30;
        _orca.Complete();

        if (_agents.Count < 6000)
        {
            for (int i = 0; i < _count; i++)
                Create();
        }
        foreach (var item in _agents)
        {
            float x = item.pos.x;
            float y = item.pos.y;
            item.Transform.position = new Vector3(x, 0, y);
            item.prefVelocity = math.normalize(new float2(center) - item.pos) * item.maxSpeed;
        }

        _orca.Schedule(Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Draw.Square(new float3(30, 0, 30), 20, Color.green);
    }

    private void Create()
    {
        var agent = _orca.agents.Add(30 + _random.NextFloat2() * _radius);
        agent.maxSpeed = 10;
        agent.prefVelocity = math.normalize(new float2(1)) * agent.maxSpeed;
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
