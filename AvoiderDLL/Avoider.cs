using UnityEngine;
using UnityEngine.AI;

namespace AvoiderDLL;

public class Avoider : MonoBehaviour
{
    private NavMeshAgent _navMeshAgent;

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

    private Vector3? _targetPosition;
    private HashSet<Vector3> _allSpots;
    private IReadOnlyCollection<Vector3> _validSpots;

    private void Awake()
    {
        // Test for a nav mesh agent on the current game object
        if (!TryGetComponent(out _navMeshAgent))
            Debug.LogWarning($"{gameObject.name} NEEDS A NAV MESH AGENT TO WORK! Also, make sure to bake a nav mesh!");
    }

    private void Update()
    {
        // Maintain eye contact with the player
        MaintainEyeContact();

        // Can the object see this game object?
        var canSeeMe = IsPositionVisible(transform.position);

        // Once the object is in range, create poisson disc points
        var currentDistance = Vector3.Distance(transform.position, objectToAvoid.transform.position);

        // JUST entered / exited the range
        if (currentDistance < range && !_objectInRange)
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
        else if (currentDistance > range && _objectInRange)
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
        if (_targetPosition != null && Vector3.Distance(transform.position, _targetPosition.Value) < 0.5f)
            _targetPosition = null;
    }

    private void MaintainEyeContact()
    {
        // Look at the player at all times
        transform.LookAt(objectToAvoid.transform);
    }

    private bool IsPositionVisible(Vector3 position)
    {
        // Return false if there is no object to avoid
        if (objectToAvoid == null)
            return false;

        // Do a ray cast from the object to avoid to this player
        if (Physics.Raycast(
                objectToAvoid.transform.position,
                position - objectToAvoid.transform.position,
                out var hitInfo
            )
           )
        {
            // If the object to avoid can see this player, return true
            if (hitInfo.collider.gameObject == gameObject)
                return true;
        }

        // If the object to avoid can't see this player, return false
        return false;
    }

    private HashSet<Vector3> FindASpot()
    {
        // Create a poisson disc sampler
        _sampler = new PoissonDiscSampler(sizeX, sizeY, cellSize);

        // Get the samples
        var samples = _sampler.Samples();

        _allSpots = new HashSet<Vector3>();

        var validSpots = new HashSet<Vector3>();

        foreach (var sample in samples)
        {
            // convert the sampled position to a vector3
            var sample3 = new Vector3(sample.x, transform.position.y, sample.y);

            // Add the sample to the list of all spots
            _allSpots.Add(sample3);

            // Check if the sample is on the nav mesh
            if (!NavMesh.SamplePosition(sample3, out var hit, 3f, NavMesh.AllAreas))
                continue;

            // Set the sample3 to the hit position
            sample3 = hit.position;

            // If the enemy can see the spot, skip it
            if (IsPositionVisible(sample3))
                continue;

            validSpots.Add(sample3);
        }

        Debug.Log(string.Format("All spots: {0}, Valid spots: {1}", _allSpots.Count, validSpots.Count));

        return validSpots;
    }

    private void OnDrawGizmos()
    {
        // If the user doesn't want to see the gizmos, don't draw them
        if (!drawGizmos)
            return;

        // Draw the range of the avoider
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        // Draw each sample point
        Gizmos.color = Color.green;
        if (_allSpots != null)
        {
            foreach (var spot in _allSpots)
                Gizmos.DrawSphere(spot, 0.1f);
        }

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