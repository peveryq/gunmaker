using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class LockerCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform lockerViewPoint;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.65f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Transform cameraTransform;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;

    private Coroutine transitionRoutine;
    private bool isInLockerView;
    private bool transitionInProgress;
    private readonly List<CameraChildState> hiddenCameraChildren = new List<CameraChildState>();
    private Action enterCompletedCallback;
    private Action exitCompletedCallback;

    private struct CameraChildState
    {
        public GameObject GameObject;
        public bool WasActive;
    }

    private void Awake()
    {
        ResolveCameraReference();
    }

    public void EnterLockerView(Action onCompleted = null)
    {
        if (lockerViewPoint == null || cameraTransform == null || transitionInProgress || isInLockerView)
        {
            return;
        }

        StopCurrentTransition();
        transitionInProgress = true;
        enterCompletedCallback = onCompleted;

        originalParent = cameraTransform.parent;
        originalLocalPosition = cameraTransform.localPosition;
        originalLocalRotation = cameraTransform.localRotation;
        originalWorldPosition = cameraTransform.position;
        originalWorldRotation = cameraTransform.rotation;

        cameraTransform.SetParent(null, true);
        HideCameraChildren();

        transitionRoutine = StartCoroutine(AnimateCamera(originalWorldPosition,
                                                         originalWorldRotation,
                                                         lockerViewPoint.position,
                                                         lockerViewPoint.rotation,
                                                         OnEnterCompleted));
    }

    public void ExitLockerView(Action onCompleted = null)
    {
        if (cameraTransform == null)
        {
            onCompleted?.Invoke();
            return;
        }

        if (!isInLockerView && !transitionInProgress)
        {
            onCompleted?.Invoke();
            return;
        }

        StopCurrentTransition();
        transitionInProgress = true;
        exitCompletedCallback = onCompleted;

        Vector3 targetPosition = originalParent != null
            ? originalParent.TransformPoint(originalLocalPosition)
            : originalWorldPosition;

        Quaternion targetRotation = originalParent != null
            ? originalParent.rotation * originalLocalRotation
            : originalWorldRotation;

        transitionRoutine = StartCoroutine(AnimateCamera(cameraTransform.position,
                                                         cameraTransform.rotation,
                                                         targetPosition,
                                                         targetRotation,
                                                         OnExitCompleted));
    }

    private void RestoreOriginalParent()
    {
        if (cameraTransform == null) return;

        cameraTransform.SetParent(originalParent, true);
        if (originalParent != null)
        {
            cameraTransform.localPosition = originalLocalPosition;
            cameraTransform.localRotation = originalLocalRotation;
        }
        else
        {
            cameraTransform.position = originalWorldPosition;
            cameraTransform.rotation = originalWorldRotation;
        }

        isInLockerView = false;
        transitionInProgress = false;
        RestoreCameraChildren();
    }

    private IEnumerator AnimateCamera(Vector3 startPos,
                                      Quaternion startRot,
                                      Vector3 endPos,
                                      Quaternion endRot,
                                      Action onComplete)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curvedT = transitionCurve != null ? transitionCurve.Evaluate(t) : t;

            cameraTransform.position = Vector3.Lerp(startPos, endPos, curvedT);
            cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, curvedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = endPos;
        cameraTransform.rotation = endRot;

        transitionInProgress = false;
        onComplete?.Invoke();
    }

    private void StopCurrentTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        transitionInProgress = false;
        enterCompletedCallback = null;
        exitCompletedCallback = null;
    }

    private void ResolveCameraReference()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        cameraTransform = targetCamera != null ? targetCamera.transform : null;
    }

    private void OnDisable()
    {
        StopCurrentTransition();

        if (isInLockerView)
        {
            RestoreOriginalParent();
        }
        else
        {
            RestoreCameraChildren();
        }
    }

    private void HideCameraChildren()
    {
        hiddenCameraChildren.Clear();
        if (cameraTransform == null) return;

        for (int i = 0; i < cameraTransform.childCount; i++)
        {
            Transform child = cameraTransform.GetChild(i);
            GameObject childObject = child.gameObject;
            hiddenCameraChildren.Add(new CameraChildState
            {
                GameObject = childObject,
                WasActive = childObject.activeSelf
            });
            childObject.SetActive(false);
        }
    }

    private void RestoreCameraChildren()
    {
        if (hiddenCameraChildren.Count == 0) return;

        foreach (var childState in hiddenCameraChildren)
        {
            if (childState.GameObject != null)
            {
                childState.GameObject.SetActive(childState.WasActive);
            }
        }

        hiddenCameraChildren.Clear();
    }

    private void OnEnterCompleted()
    {
        isInLockerView = true;
        transitionInProgress = false;
        enterCompletedCallback?.Invoke();
        enterCompletedCallback = null;
    }

    private void OnExitCompleted()
    {
        RestoreOriginalParent();
        exitCompletedCallback?.Invoke();
        exitCompletedCallback = null;
    }

    public bool IsTransitionInProgress => transitionInProgress;
    public bool IsInLockerView => isInLockerView;
}

