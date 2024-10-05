using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace AvoiderDLL;

[RequireComponent(typeof(NavMeshAgent))]
public class Avoider : MonoBehaviour
{
    private NavMeshAgent _navMeshAgent;

    [SerializeField] private GameObject objectToAvoid;

    [SerializeField] private bool drawGizmos = true;

    [Header("Stats")] [SerializeField] [Min(.5f)]
    private float detectionRange = 7.5f;

    private bool _objectInRange;

    [SerializeField] [Min(.5f)] private float speed = 10;


    private PoissonDiscSampler _sampler;

    [Header("Poisson Disc Path Sampling Settings")] [SerializeField] [Min(2)]
    private float areaSize = 32;

    [SerializeField] [Min(.5f)] private float density = 2;

    private Vector3? _targetPosition;
    private HashSet<Vector3> _allSpots;
    private IReadOnlyCollection<Vector3> _validSpots;

    private int _actorLayerMask;

    private void Awake()
    {
        // Test for a nav mesh agent on the current game object
        if (!TryGetComponent(out _navMeshAgent))
            Debug.LogWarning($"{gameObject.name} NEEDS A NAV MESH AGENT TO WORK! Also, make sure to bake a nav mesh!");
    }

    private void Start()
    {
        // Create a layer mask that only ignores the actor layer
        _actorLayerMask = ~LayerMask.GetMask("Actor");
    }

    private void Update()
    {
        // Maintain eye contact with the player
        MaintainEyeContact();

        // Can the object see this game object?
        var canSeeMe = IsObjectVisible(gameObject);

        // Once the object is in range, create poisson disc points
        var currentDistance = Vector3.Distance(transform.position, objectToAvoid.transform.position);

        // If the target position is not null and
        // If the object to avoid can see the target position,
        if (_targetPosition != null && IsPositionVisible(_targetPosition.Value, _actorLayerMask))
        {
            // clear the target position
            _targetPosition = null;

            // Also reset the object in range flag
            _objectInRange = false;

            // Clear the valid spots
            _validSpots = new HashSet<Vector3>();

            // Clear the all spots
            _allSpots = new HashSet<Vector3>();

            // This forces the object to avoid to reevaluate its position
        }

        // JUST entered / exited the range
        if (canSeeMe && currentDistance < detectionRange && !_objectInRange)
        {
            _objectInRange = true;

            // If there is no target position, find one
            if (_targetPosition == null)
            {
                _validSpots = FindASpot();

                _targetPosition = null;

                if (_validSpots.Count > 0)
                {
                    // Determine the furthest point from the object to avoid
                    Vector3? furthestPoint = _validSpots
                        .OrderByDescending(x => Vector3.Distance(x, objectToAvoid.transform.position))
                        .FirstOrDefault();

                    _targetPosition = furthestPoint;
                }

                Debug.Log(string.Format("Found a spot: {0} vs {1}", _targetPosition, transform.position));
            }

            if (_targetPosition != null)
                _navMeshAgent.SetDestination(_targetPosition.Value);
        }
        else if (currentDistance > detectionRange && _objectInRange)
        {
            _objectInRange = false;
        }

        // If the target position is null, stop moving
        if (_targetPosition == null)
        {
            _navMeshAgent.SetDestination(transform.position);
            _navMeshAgent.speed = 0;
        }
        else
            _navMeshAgent.speed = speed;

        // If this game object is close to its target position, clear the target position
        if (_targetPosition != null && Vector3.Distance(transform.position, _targetPosition.Value) < 1f)
            _targetPosition = null;
    }

    private void MaintainEyeContact()
    {
        // Look at the player at all times
        transform.LookAt(objectToAvoid.transform);
    }

    private bool IsPositionVisible(Vector3 position, int layerMask = ~0)
    {
        // Return false if there is no object to avoid
        if (objectToAvoid == null)
            return false;

        // Do a ray cast from the object to avoid to this player
        if (Physics.Raycast(
                origin: objectToAvoid.transform.position,
                direction: position - objectToAvoid.transform.position,
                hitInfo: out var hitInfo,
                maxDistance: 9999,
                layerMask: layerMask
            )
           )
        {
            // If the cast hit anything, return false
            return false;
        }

        // If the cast didn't hit anything, return true
        return true;
    }

    private bool IsObjectVisible(GameObject obj, int layerMask = ~0)
    {
        // Return false if there is no object to avoid
        if (objectToAvoid == null)
            return false;

        // Do a ray cast from the object to avoid to this player
        if (Physics.Raycast(
                origin: objectToAvoid.transform.position,
                direction: obj.transform.position - objectToAvoid.transform.position,
                hitInfo: out var hitInfo,
                maxDistance: 9999,
                layerMask: layerMask
            )
           )
        {
            // If the cast hit the object we are looking for, return true
            if (hitInfo.collider.gameObject == obj)
                return true;
        }

        return false;
    }

    private HashSet<Vector3> FindASpot()
    {
        // Create a poisson disc sampler
        _sampler = new PoissonDiscSampler(areaSize, areaSize, density);

        // Get the samples
        var samples = _sampler.Samples();

        _allSpots = new HashSet<Vector3>();

        var validSpots = new HashSet<Vector3>();

        foreach (var sample in samples)
        {
            // convert the sampled position to a vector3
            // and add the current position of the game object to make it dynamic
            var sample3 = transform.position +
                          new Vector3(sample.x, 0, sample.y) -
                          new Vector3(areaSize / 2, 0, areaSize / 2);

            // Add the sample to the list of all spots
            _allSpots.Add(sample3);

            // Check if the sample is on the nav mesh
            if (!NavMesh.SamplePosition(sample3, out var hit, 3f, NavMesh.AllAreas))
                continue;

            // Set the sample3 to the hit position
            sample3 = hit.position + new Vector3(0, _navMeshAgent.height, 0);

            // If the enemy can see the spot, skip it
            if (IsPositionVisible(sample3, _actorLayerMask))
                continue;

            validSpots.Add(sample3);
        }

        return validSpots;
    }

    private void OnDrawGizmos()
    {
        // If the user doesn't want to see the gizmos, don't draw them
        if (!drawGizmos)
            return;

        // Draw the range of the avoider
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // // Draw each sample point
        // Gizmos.color = Color.green;
        // if (_allSpots != null)
        // {
        //     foreach (var spot in _allSpots)
        //         Gizmos.DrawSphere(spot, 0.1f);
        // }

        // Draw each valid spot
        Gizmos.color = Color.yellow;
        if (_validSpots != null)
        {
            foreach (var spot in _validSpots)
                Gizmos.DrawSphere(spot, 0.1f);
        }

        // Draw the target position
        Gizmos.color = Color.blue;
        if (_targetPosition != null)
            Gizmos.DrawSphere(_targetPosition.Value, 0.1f);
    }
}