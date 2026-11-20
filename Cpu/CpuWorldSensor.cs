using System;
using System.Collections.Generic;
using UnityEngine;

public class CpuWorldSensor
{
    private const float REFRESH_INTERVAL = 0.5f;

    private List<EatObject> _eatObjects = new List<EatObject>();
    private List<StatusUpItem> _items = new List<StatusUpItem>();
    private List<BreakObject> _breakObjects = new List<BreakObject>();
    private Dictionary<GameObject, float> _ignoreTimes = new Dictionary<GameObject, float>(); //たどり着けなかったものはしばらく無視する
    private float _nextRefreshTime;

    public void Refresh()
    {
        if (Time.time < _nextRefreshTime) return;
        _nextRefreshTime = Time.time + REFRESH_INTERVAL;

        FindObjects(_eatObjects);
        FindObjects(_items);
        FindObjects(_breakObjects);
    }

    private void FindObjects<T>(List<T> list) where T : MonoBehaviour
    {
        list.Clear();
        list.AddRange(UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    public void Ignore(GameObject target, float time)
    {
        if (target == null) return;
        _ignoreTimes[target] = Time.time + time;
    }

    private bool IsIgnored(GameObject target)
    {
        if (_ignoreTimes.TryGetValue(target, out float ignoreTime) == false) return false;
        return Time.time < ignoreTime;
    }

    public bool TryFindNearestEat(Vector3 from, float radius, out EatObject result, out float distance)
    {
        return TryFindNearest(_eatObjects, from, radius, null, out result, out distance);
    }

    public bool TryFindNearestItem(Vector3 from, float radius, out StatusUpItem result, out float distance)
    {
        return TryFindNearest(_items, from, radius, null, out result, out distance);
    }

    public bool TryFindNearestBreak(Vector3 from, float radius, out BreakObject result, out float distance)
    {
        return TryFindNearest(_breakObjects, from, radius, b => b.CompareTag("BreakObject") || b.CompareTag("NPC"), out result, out distance);
    }

    private bool TryFindNearest<T>(List<T> list, Vector3 from, float radius, Func<T, bool> filter, out T result, out float distance) where T : MonoBehaviour
    {
        result = null;
        distance = float.MaxValue;
        float sqrRadius = radius * radius;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            T obj = list[i];
            if (obj == null || obj.gameObject.activeInHierarchy == false) continue;
            if (IsIgnored(obj.gameObject)) continue;
            if (filter != null && filter(obj) == false) continue;

            Vector3 diff = obj.transform.position - from;
            diff.y = 0f;
            float sqr = diff.sqrMagnitude;
            if (sqr > sqrRadius || sqr >= nearestSqr) continue;

            nearestSqr = sqr;
            result = obj;
        }

        if (result == null) return false;
        distance = Mathf.Sqrt(nearestSqr);
        return true;
    }
}
