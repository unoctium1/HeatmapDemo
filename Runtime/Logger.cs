﻿using HeatmapParticles.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HeatmapParticles
{
    public class Logger : PersistableObject
    {
        //[SerializeField] float waitTime = 0.03f;

        [SerializeField] public PointsList points;

        [SerializeField] Text debugText = null;
        [SerializeField, Tooltip("Set true to log on start")] bool log = false;
        [SerializeField] Camera cam;
        [SerializeField, Tooltip("Controls speed of polling")] float waitTime;
        float timer = 0.0f;
        [SerializeField] InputSource input;
        [SerializeField] bool realtime = false; //temporarily disabled realtime behavior
        [SerializeField] HeatmapParticleSystem system; //tried to avoid coupling these but oh well

        private int layerMask;

        public bool Realtime { get => realtime; set => realtime = value; }
        public bool Log { get => log; set => log = value; }

        public VectorEvent onLogEvent;
        public IntEvent onPrepPlayback;

        delegate bool GetInputPoint(Camera cam, out Vector3 point, int layerMask);
        GetInputPoint getter;

        private void Awake()
        {
            if (onLogEvent == null) onLogEvent = new VectorEvent();
            if (onPrepPlayback == null) onPrepPlayback = new IntEvent();
            points = PointsList.Instance;

            switch (input)
            {
                case InputSource.mousePos:
                    getter = GetMousePos;
                    break;
                case InputSource.vrCameraGaze:
                    getter = GetGazePos;
                    break;
            }


            layerMask = 1 << Physics.IgnoreRaycastLayer;
            layerMask = ~layerMask;

        }

        public void Playback()
        {
            onPrepPlayback.Invoke(points.CountCurrent);
            bool resetLog = false;
            if (log)
            {
                log = false;
                resetLog = true;
            }
            system.CreateFromDictionary(points.CurrDict);
            //points.ClearCurrent();
            if (resetLog) log = true;
        }

        private void FixedUpdate()
        {
            if (log)
            {
                timer += Time.deltaTime;
                if (timer > waitTime)
                {
                    if (getter(cam, out Vector3 point, layerMask))
                    {
                        SmallVector3 toSave = new SmallVector3(point);
                        points.Add(toSave);

                        if (realtime)
                        {
                            onLogEvent.Invoke(toSave);
                        }
                        //debugText.text = toSave.ToString();
                    }
                    timer -= waitTime;
                }
            }
        }

        private IEnumerator PlaybackPoints()
        {
            onPrepPlayback.Invoke(points.Count);
            bool resetLog = false;
            if (log)
            {
                log = false;
                resetLog = true;
            }
            for (int i = 0; i < points.Count; i++)
            {
                yield return new WaitForFixedUpdate();
                onLogEvent.Invoke(points[i].GetVector3());

            }
            points.Clear();
            if (resetLog) log = true;
        }

        private static bool GetMousePos(Camera cam, out Vector3 point, int layerMask)
        {
            point = Input.mousePosition;
            point.z = cam.nearClipPlane;
            Ray r = cam.ScreenPointToRay(point);

            if (Physics.Raycast(r, out RaycastHit hit, 50f, layerMask))
            {
                point = hit.point;
                return true;
            }
            else { return false; }
        }


        // Replace this with eye tracking
        private static bool GetGazePos(Camera cam, out Vector3 point, int layerMask)
        {
            point = new Vector3(0.5f, 0.5f, 0f);

            // Haven't really tested Mono vs left here
            Ray r = cam.ViewportPointToRay(point, Camera.MonoOrStereoscopicEye.Mono);

            if (Physics.Raycast(r, out RaycastHit hit, 50f, layerMask))
            {
                point = hit.point;
                return true;
            }
            else { return false; }
        }

        public override void Load(DataReader reader)
        {
            points.ClearCurrent();
            PointsList.Instance.Load(reader);
            points = PointsList.Instance;
        }

        public override void Save(DataWriter writer)
        {
            points.Save(writer);


        }

        public void Clear()
        {
            points.ClearCurrent();
        }

    }

    public enum InputSource
    {
        mousePos, vrCameraGaze
    }

    [System.Serializable]
    public class VectorEvent : UnityEvent<SmallVector3>
    {

    }

    [System.Serializable]
    public class IntEvent : UnityEvent<int>
    {

    }
}
