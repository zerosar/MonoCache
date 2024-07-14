// -------------------------------------------------------------------------------------------
// The MIT License
// MonoCache is a fast optimization framework for Unity https://github.com/MeeXaSiK/MonoCache
// Copyright (c) 2021-2024 Night Train Code
// -------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GlobalUpdate : MonoBehaviour
{
    private static GlobalUpdate instance;
    private static bool applicationIsQuitting = false;
    private static bool isCreatingInstance = false;
    private readonly List<MonoCache> _monoCaches = new(32);
    private int _count;

    internal static GlobalUpdate Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
#if DEBUG
                Debug.LogWarning("[GlobalUpdate] Instance already destroyed on application quit. Won't create again - returning null.");
#endif
                return null;
            }

            if (instance == null)
            {
                isCreatingInstance = true;
                instance = (GlobalUpdate)FindObjectOfType(typeof(GlobalUpdate));

                if (FindObjectsOfType(typeof(GlobalUpdate)).Length > 1)
                {
#if DEBUG
                    Debug.LogError("[GlobalUpdate] Something went really wrong " +
                        " - there should never be more than 1 GlobalUpdate!" +
                        " Reopening the scene might fix it.");
#endif
                    return instance;
                }

                if (instance == null)
                {
                    GameObject singleton = new GameObject();
                    instance = singleton.AddComponent<GlobalUpdate>();
                    singleton.name = "(singleton) GlobalUpdate";

                    DontDestroyOnLoad(singleton);
#if DEBUG
                    Debug.Log("[GlobalUpdate] An instance of GlobalUpdate is needed in the scene, so '" + singleton +
                        "' was created with DontDestroyOnLoad.");
#endif
                }
#if DEBUG
                else
                {
                    Debug.Log("[GlobalUpdate] Using instance already created: " + instance.gameObject.name);
                }
#endif
                isCreatingInstance = false;
            }

            return instance;
        }
    }

    private GlobalUpdate() { }

    private void OnDestroy()
    {
        applicationIsQuitting = true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
#if DEBUG
            Debug.LogError("GlobalUpdate should not be added manually. Use GlobalUpdate.Instance to access the singleton instance.");
#endif
            DestroyImmediate(this);
            return;
        }

        instance = this;

#if DEBUG
        MonoCacheExceptionChecker.CheckForExceptions();
#endif
    }

    private void Update()
    {
        for (int i = 0; i < _count; i++)
        {
            _monoCaches[i].RaiseRun();
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < _count; i++)
        {
            _monoCaches[i].RaiseFixedRun();
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _count; i++)
        {
            _monoCaches[i].RaiseLateRun();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Add(MonoCache monoCache)
    {
        _monoCaches.Add(monoCache);
        monoCache._index = _count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Remove(MonoCache monoCache)
    {
        var lastComponent = _monoCaches[_count - 1];
        _monoCaches[monoCache._index] = lastComponent;
        lastComponent._index = monoCache._index;
        _monoCaches.RemoveAt(--_count);
    }
}