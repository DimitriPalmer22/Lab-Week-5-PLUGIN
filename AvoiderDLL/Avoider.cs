using UnityEngine;
using UnityEngine.AI;

namespace AvoiderDLL;

public class Avoider : MonoBehaviour
{
    [Tooltip("Put the player here!")] [SerializeField]
    private GameObject objectToAvoid;

    [SerializeField] private bool drawGizmos;

    [Header("Stats")] [SerializeField] [Min(0)]
    private float range;

    [SerializeField] [Min(0)] private float speed;

    private bool _objectInRange;

    private PoissonDiscSampler _sampler;

    [SerializeField] private float sizeX;
    [SerializeField] private float sizeY;
    [SerializeField] private float cellSize;

    private void Awake()
    {
        // Test for a nav mesh agent on the current game object
        if (GetComponent<NavMeshAgent>() == null)
            Debug.LogError($"{gameObject.name} NEEDS A NAV MESH AGENT TO WORK! Also, make sure to bake a nav mesh!");
    }

    private void Update()
    {
        // Maintain eye contact with the player
        MaintainEyeContact();

        // Once the object is in range, create poisson disc points
        var currentDistance = Vector3.Distance(transform.position, objectToAvoid.transform.position);

        // Create the poisson disc points once the object is in range
        if (currentDistance <= range && !_objectInRange)
        {
            _objectInRange = true;
            CreatePoissonDiscPoints();
        }
        else if (currentDistance > range && _objectInRange)
        {
            _objectInRange = false;

            // Clear the poisson disc
            _sampler = null;
        }
    }

    private void MaintainEyeContact()
    {
        // Look at the player at all times
        transform.LookAt(objectToAvoid.transform);
    }

    private void CreatePoissonDiscPoints()
    {
        // Create a poisson disc sampler
        _sampler = new PoissonDiscSampler(sizeX, sizeY, cellSize);

        foreach (var point in _sampler.Samples())
        {
            //do something
        }
    }

    private void OnDrawGizmos()
    {
        // If the user doesn't want to see the gizmos, don't draw them
        if (!drawGizmos)
            return;

        // Draw the range of the avoider
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        // Draw each poisson disc point
        if (_objectInRange && _sampler != null)
        {
            var samples = _sampler.Samples();

            if (samples == null)
                return;

            foreach (var point in samples)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point, 0.1f);
            }
        }
    }
}